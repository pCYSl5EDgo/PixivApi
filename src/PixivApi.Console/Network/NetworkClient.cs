namespace PixivApi.Console;

[Command("net")]
public sealed partial class NetworkClient : ConsoleAppBase, IDisposable
{
    private const string ApiHost = "app-api.pixiv.net";
    private readonly ConfigSettings configSettings;
    private readonly HttpClient client;
    private readonly AuthenticationHeaderValueHolder holder;

    public NetworkClient(ConfigSettings config, HttpClient client, AuthenticationHeaderValueHolder holder)
    {
        configSettings = config;
        this.client = client;
        this.holder = holder;
    }

    public void Dispose() => holder.Dispose();

    private void AddToHeader(HttpRequestMessage request, AuthenticationHeaderValue authentication)
    {
        request.Headers.Authorization = authentication;
        if (!request.TryAddToHeader(configSettings.HashSecret, ApiHost))
        {
            throw new InvalidOperationException();
        }
    }

    private async ValueTask<HttpResponseMessage> RetryAndReconnectGetAsync(string url, CancellationToken token)
    {
        HttpResponseMessage responseMessage;
        var logger = Context.Logger;
        do
        {
            token.ThrowIfCancellationRequested();
            using (HttpRequestMessage request = new(HttpMethod.Get, url))
            {
                var authentication = await holder.GetAsync(token).ConfigureAwait(false);
                AddToHeader(request, authentication);
                responseMessage = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, token).ConfigureAwait(false);
            }

            var statusCode = responseMessage.StatusCode;
            var isBadRequest = statusCode == HttpStatusCode.BadRequest;
            if (responseMessage.IsSuccessStatusCode || (statusCode != HttpStatusCode.Forbidden && !isBadRequest))
            {
                return responseMessage;
            }

            try
            {
                if (!System.Console.IsOutputRedirected)
                {
                    var text = isBadRequest ? "not found" : "a bad request";
                    logger.LogWarning($"{VirtualCodes.BrightYellowColor}Downloading {url} is {text}. Retry {configSettings.RetrySeconds} seconds later. Time: {DateTime.Now} Restart: {DateTime.Now.Add(configSettings.RetryTimeSpan)}{VirtualCodes.NormalizeColor}");
                }

                await Task.Delay(configSettings.RetryTimeSpan, token).ConfigureAwait(false);
                if (isBadRequest)
                {
                    await holder.InvalidateAsync(token).ConfigureAwait(false);
                }

                if (!System.Console.IsOutputRedirected)
                {
                    logger.LogWarning($"{VirtualCodes.BrightYellowColor}Restart. Time: {DateTime.Now}{VirtualCodes.NormalizeColor}");
                }
            }
            finally
            {
                responseMessage.Dispose();
            }
        } while (true);
    }

    private async ValueTask DownloadArtworkResponses(string output, bool addBehaviour, string url, CancellationToken token)
    {
        var logger = Context.Logger;
        var databaseTask = IOUtility.MessagePackDeserializeAsync<DatabaseFile>(output, token);
        // When addBehaviour is false, we should wait for the database initialization in order not to query unneccessarily.
        var database = addBehaviour ? null : await databaseTask.ConfigureAwait(false);
        var responseList = default(List<ArtworkResponseContent>);
        ulong add = 0UL, update = 0UL;
        bool RegisterNotShow(DatabaseFile database, ArtworkResponseContent item)
        {
            var isAdd = true;
            _ = database.ArtworkDictionary.AddOrUpdate(
                item.Id,
                _ => LocalNetworkConverter.Convert(item, database.TagSet, database.ToolSet, database.UserDictionary),
                (_, artwork) =>
                {
                    isAdd = false;
                    LocalNetworkConverter.Overwrite(artwork, item, database.TagSet, database.ToolSet, database.UserDictionary);
                    return artwork;
                }
            );

            return isAdd;
        }

        void RegisterShow(DatabaseFile database, ArtworkResponseContent item)
        {
            _ = database.ArtworkDictionary.AddOrUpdate(
                item.Id,
                _ =>
                {
                    ++add;
                    if (System.Console.IsOutputRedirected)
                    {
                        logger.LogInformation($"{item.Id}");
                    }
                    else
                    {
                        logger.LogInformation($"{add,4}: {item.Id,20}");
                    }

                    return LocalNetworkConverter.Convert(item, database.TagSet, database.ToolSet, database.UserDictionary);
                },
                (_, artwork) =>
                {
                    ++update;
                    LocalNetworkConverter.Overwrite(artwork, item, database.TagSet, database.ToolSet, database.UserDictionary);
                    return artwork;
                }
            );
        }

        try
        {
            await foreach (var artworkCollection in new DownloadArtworkAsyncEnumerable(url, RetryAndReconnectGetAsync).WithCancellation(token))
            {
                if (database is null)
                {
                    if (!databaseTask.IsCompleted)
                    {
                        responseList ??= new List<ArtworkResponseContent>(5010);
                        foreach (var item in artworkCollection)
                        {
                            responseList.Add(item);
                            if (System.Console.IsOutputRedirected)
                            {
                                logger.LogInformation($"{item.Id}");
                            }
                            else
                            {
                                logger.LogInformation($"{responseList.Count,4}: {item.Id}");
                            }
                        }

                        token.ThrowIfCancellationRequested();
                        continue;
                    }

                    database = await databaseTask.ConfigureAwait(false) ?? new();
                    if (responseList is { Count: > 0 })
                    {
                        foreach (var item in responseList)
                        {
                            (RegisterNotShow(database, item) ? ref add : ref update)++;
                        }

                        responseList = null;
                    }
                }

                var oldAdd = add;
                foreach (var item in artworkCollection)
                {
                    RegisterShow(database, item);
                }

                token.ThrowIfCancellationRequested();
                if (!addBehaviour && add == oldAdd)
                {
                    break;
                }
            }
        }
        catch (TaskCanceledException)
        {
            logger.LogError("Accept cancel. Please wait for writing to the database file.");
        }
        finally
        {
            if (database is null)
            {
                database = await databaseTask.ConfigureAwait(false) ?? new();
                if (responseList is { Count: > 0 })
                {
                    foreach (var item in responseList)
                    {
                        (RegisterNotShow(database, item) ? ref add : ref update)++;
                    }

                    responseList = null;
                }
            }

            if (add != 0 || update != 0)
            {
                await IOUtility.MessagePackSerializeAsync(output, database, FileMode.Create).ConfigureAwait(false);
            }

            if (!System.Console.IsOutputRedirected)
            {
                logger.LogInformation($"Total: {database.ArtworkDictionary.Count} Add: {add} Update: {update}");
            }
        }
    }
}
