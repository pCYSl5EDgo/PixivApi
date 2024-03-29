namespace PixivApi.Console;

public partial class NetworkClient
{
    [Command("update")]
    public async ValueTask UpdateAsync()
    {
        if (string.IsNullOrWhiteSpace(configSettings.DatabaseFilePath) || string.IsNullOrWhiteSpace(configSettings.ArtworkFilterFilePath))
        {
            return;
        }

        var token = Context.CancellationToken;
        var logger = Context.Logger;
        var requestSender = Context.ServiceProvider.GetRequiredService<RequestSender>();
        ulong update = 0, removed = 0;
        var database = await databaseFactory.RentAsync(token).ConfigureAwait(false);
        var transactional = database as ITransactionalDatabase;
        if (transactional is not null)
        {
            await transactional.BeginTransactionAsync(token).ConfigureAwait(false);
        }

        try
        {
            var artworkFilter = await filterFactory.CreateAsync(database, new(configSettings.ArtworkFilterFilePath), token).ConfigureAwait(false) ?? throw new NullReferenceException();
            var collection = database.FilterAsync(artworkFilter, token);
            await foreach (var item in collection)
            {
                ArtworkResponseContent artwork;
                do
                {
                    token.ThrowIfCancellationRequested();
                    using var response = await GetArtworkDetailAsync(requestSender, item.Id, token).ConfigureAwait(false);
                    if (response.IsSuccessStatusCode)
                    {
                        var array = await response.Content.ReadAsByteArrayAsync(token).ConfigureAwait(false);
                        artwork = IOUtility.JsonDeserialize<IllustDateilResponseData>(array).Illust;
                        break;
                    }

                    if (response.StatusCode == HttpStatusCode.NotFound)
                    {
                        removed++;
                        goto REMOVED;
                    }

                    response.EnsureSuccessStatusCode();
                    continue;
                } while (true);

                if (artwork.User.Id == 0)
                {
                    removed++;
                    goto REMOVED;
                }

                token.ThrowIfCancellationRequested();
                ++update;
                if (item.Type == ArtworkType.Ugoira && item.UgoiraFrames is null)
                {
                    using var response = await GetArtworkUgoiraMetadataAsync(requestSender, artwork.Id, token).ConfigureAwait(false);
                    response.EnsureSuccessStatusCode();
                    var ugoira = IOUtility.JsonDeserialize<UgoiraMetadataResponseData>(await response.Content.ReadAsByteArrayAsync(token).ConfigureAwait(false));
                    var frames = ugoira.Value.Frames;
                    item.UgoiraFrames = frames.Length == 0 ? Array.Empty<ushort>() : new ushort[frames.Length];
                    for (var i = 0; i < frames.Length; i++)
                    {
                        item.UgoiraFrames[i] = (ushort)frames[i].Delay;
                    }
                }

                await LocalNetworkConverter.OverwriteAsync(item, artwork, database, database, database, token).ConfigureAwait(false);
                if (System.Console.IsOutputRedirected)
                {
                    logger.LogInformation($"{item.Id}");
                }
                else
                {
                    logger.LogInformation($"{update,4}: {item.Id,20}");
                }

                continue;

            REMOVED:
                item.IsOfficiallyRemoved = true;
                if (!System.Console.IsOutputRedirected)
                {
                    logger.LogInformation($"{++update,4}: {item.Id,20} removed");
                }
            }
        }
        catch (Exception e) when (transactional is not null && e is not TaskCanceledException && e is not OperationCanceledException)
        {
            await transactional.RollbackTransactionAsync(token).ConfigureAwait(false);
            transactional = null;
            throw;
        }
        finally
        {
            if (!System.Console.IsOutputRedirected)
            {
                var artworkCount = await database.CountArtworkAsync(CancellationToken.None).ConfigureAwait(false);
                logger.LogInformation($"Total: {artworkCount} Update: {update} Removed: {removed}");
            }

            if (transactional is not null)
            {
                await transactional.EndTransactionAsync(CancellationToken.None).ConfigureAwait(false);
            }

            databaseFactory.Return(ref database);
        }
    }

    private static async ValueTask<HttpResponseMessage> GetArtworkDetailAsync(RequestSender requestSender, ulong id, CancellationToken token) => await requestSender.GetAsync($"https://{ApiHost}/v1/illust/detail?illust_id={id}", token).ConfigureAwait(false);

    private static async ValueTask<HttpResponseMessage> GetArtworkUgoiraMetadataAsync(RequestSender requestSender, ulong id, CancellationToken token) => await requestSender.GetAsync($"https://{ApiHost}/v1/ugoira/metadata?illust_id={id}", token).ConfigureAwait(false);

    private static async ValueTask<HttpResponseMessage> GetUserDetailAsync(RequestSender requestSender, ulong id, CancellationToken token) => await requestSender.GetAsync($"https://{ApiHost}/v1/user/detail?user_id={id}", token).ConfigureAwait(false);
}
