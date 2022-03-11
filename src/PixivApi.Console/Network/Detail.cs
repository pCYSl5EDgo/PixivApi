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
        var artworkFilter = await IOUtility.JsonDeserializeAsync<ArtworkFilter>(configSettings.ArtworkFilterFilePath, token).ConfigureAwait(false);
        if (artworkFilter is null)
        {
            return;
        }

        var logger = Context.Logger;
        var requestSender = Context.ServiceProvider.GetRequiredService<RequestSender>();
        ulong update = 0;
        var database = await databaseFactory.RentAsync(token).ConfigureAwait(false);
        try
        {
            var collection = database.ArtworkFilterAsync(artworkFilter, token);
            await foreach (var item in collection)
            {
                ArtworkResponseContent artwork;
                do
                {
                    using var response = await GetArtworkDetailAsync(requestSender, item.Id, token).ConfigureAwait(false);
                    if (response.IsSuccessStatusCode)
                    {
                        var array = await response.Content.ReadAsByteArrayAsync(token).ConfigureAwait(false);
                        artwork = IOUtility.JsonDeserialize<IllustDateilResponseData>(array).Illust;
                        break;
                    }

                    if (response.StatusCode == HttpStatusCode.NotFound)
                    {
                        goto REMOVED;
                    }

                    response.EnsureSuccessStatusCode();
                    continue;
                } while (true);

                if (artwork.User.Id == 0)
                {
                    goto REMOVED;
                }

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
                database.Update();
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
        finally
        {
            if (!System.Console.IsOutputRedirected)
            {
                var artworkCount = await database.CountArtworkAsync(token).ConfigureAwait(false);
                logger.LogInformation($"Total: {artworkCount} Update: {update}");
            }

            databaseFactory.Return(ref database);
        }
    }

    private static async ValueTask<HttpResponseMessage> GetArtworkDetailAsync(RequestSender requestSender, ulong id, CancellationToken token) => await requestSender.GetAsync($"https://{ApiHost}/v1/illust/detail?illust_id={id}", token).ConfigureAwait(false);

    private static async ValueTask<HttpResponseMessage> GetArtworkUgoiraMetadataAsync(RequestSender requestSender, ulong id, CancellationToken token) => await requestSender.GetAsync($"https://{ApiHost}/v1/ugoira/metadata?illust_id={id}", token).ConfigureAwait(false);
}
