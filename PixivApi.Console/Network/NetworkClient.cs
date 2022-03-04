using System.Runtime.ExceptionServices;

namespace PixivApi.Console;

[Command("net")]
public sealed partial class NetworkClient : ConsoleAppBase, IDisposable
{
    private const string ApiHost = "app-api.pixiv.net";
    private readonly ConfigSettings configSettings;
    private readonly ILogger<NetworkClient> logger;
    private readonly HttpClient client;
    private readonly FinderFacade finder;
    private readonly ConverterFacade converter;
    private readonly AuthenticationHeaderValueHolder holder;

    public NetworkClient(ConfigSettings config, ILogger<NetworkClient> logger, HttpClient client, FinderFacade finderFacade, ConverterFacade converterFacade)
    {
        configSettings = config;
        this.logger = logger;
        this.client = client;
        finder = finderFacade;
        converter = converterFacade;
        holder = new(config, client, configSettings.ReconnectLoopIntervalTimeSpan);
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

    /// <summary>
    /// This must be called from single thread.
    /// </summary>
    private ValueTask<AuthenticationHeaderValue> ConnectAsync(CancellationToken token)
    {
        client.AddToDefaultHeader(configSettings);
        return holder.ConnectAsync(token);
    }

    private async ValueTask<AuthenticationHeaderValue> ReconnectAsync(bool pipe, CancellationToken token)
    {
        if (!pipe)
        {
            logger.LogInformation($"{VirtualCodes.BrightYellowColor}Wait for {configSettings.RetryTimeSpan.TotalSeconds} seconds to reconnect. Time: {DateTime.Now} Restart: {DateTime.Now.Add(configSettings.RetryTimeSpan)}{VirtualCodes.NormalizeColor}");
        }

        await Task.Delay(configSettings.RetryTimeSpan, token).ConfigureAwait(false);
        var authentication = await holder.RegetAsync(token).ConfigureAwait(false);
        if (authentication is null)
        {
            if (!pipe)
            {
                logger.LogError($"{VirtualCodes.BrightRedColor}Reconnection failed.{VirtualCodes.NormalizeColor}");
            }

            throw new IOException();
        }
        else
        {
            if (!pipe)
            {
                logger.LogInformation($"{VirtualCodes.BrightYellowColor}Reconnect.{VirtualCodes.NormalizeColor}");
            }
        }

        return authentication;
    }

    private async ValueTask<AuthenticationHeaderValue> ReconnectAsync(Exception exception, bool pipe, CancellationToken token)
    {
        if (!pipe)
        {
            logger.LogInformation($"{VirtualCodes.BrightYellowColor}Wait for {configSettings.RetryTimeSpan.TotalSeconds} seconds to reconnect. Time: {DateTime.Now} Restart: {DateTime.Now.Add(configSettings.RetryTimeSpan)}{VirtualCodes.NormalizeColor}");
        }

        await Task.Delay(configSettings.RetryTimeSpan, token).ConfigureAwait(false);
        var authentication = await holder.RegetAsync(token).ConfigureAwait(false);
        if (authentication is null)
        {
            if (!pipe)
            {
                logger.LogError($"{VirtualCodes.BrightRedColor}Reconnection failed. Time: {DateTime.Now}{VirtualCodes.NormalizeColor}");
            }

            ExceptionDispatchInfo.Throw(exception);
        }
        else
        {
            if (!pipe)
            {
                logger.LogInformation($"{VirtualCodes.BrightYellowColor}Reconnect. Time: {DateTime.Now}{VirtualCodes.NormalizeColor}");
            }
        }

        return authentication;
    }

    private async ValueTask<byte[]> RetryGetAsync(string url, AuthenticationHeaderValue authentication, bool pipe, CancellationToken token)
    {
        do
        {
            using HttpRequestMessage request = new(HttpMethod.Get, url);
            AddToHeader(request, authentication);
            using var responseMessage = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, token).ConfigureAwait(false);
            if (responseMessage.IsSuccessStatusCode)
            {
                return await responseMessage.Content.ReadAsByteArrayAsync(token).ConfigureAwait(false);
            }

            if (responseMessage.StatusCode != HttpStatusCode.Forbidden)
            {
                responseMessage.EnsureSuccessStatusCode();
            }

            token.ThrowIfCancellationRequested();
            if (!pipe)
            {
                logger.LogWarning($"{VirtualCodes.BrightYellowColor}Downloading {url} is forbidden. Retry {configSettings.RetrySeconds} seconds later. Time: {DateTime.Now} Restart: {DateTime.Now.Add(configSettings.RetryTimeSpan)}{VirtualCodes.NormalizeColor}");
            }

            await Task.Delay(configSettings.RetryTimeSpan, token).ConfigureAwait(false);
            if (!pipe)
            {
                logger.LogWarning($"{VirtualCodes.BrightYellowColor}Restart. Time: {DateTime.Now}{VirtualCodes.NormalizeColor}");
            }
        } while (true);
    }

    private async ValueTask DownloadArtworkResponses(string output, bool addBehaviour, bool pipe, string url, CancellationToken token)
    {
        var databaseTask = IOUtility.MessagePackDeserializeAsync<DatabaseFile>(output, token);
        var authentication = await ConnectAsync(token).ConfigureAwait(false);

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

        void RegisterShow(DatabaseFile database, ArtworkResponseContent item, bool pipe)
        {
            _ = database.ArtworkDictionary.AddOrUpdate(
                item.Id,
                _ =>
                {
                    ++add;
                    if (pipe)
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
            await foreach (var artworkCollection in new DownloadArtworkAsyncEnumerable(url, authentication, RetryGetAsync, ReconnectAsync, pipe).WithCancellation(token))
            {
                if (database is null)
                {
                    if (!databaseTask.IsCompleted)
                    {
                        responseList ??= new List<ArtworkResponseContent>(5010);
                        foreach (var item in artworkCollection)
                        {
                            responseList.Add(item);
                            if (pipe)
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
                    RegisterShow(database, item, pipe);
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

            if (!pipe)
            {
                logger.LogInformation($"Total: {database.ArtworkDictionary.Count} Add: {add} Update: {update}");
            }
        }
    }
}
