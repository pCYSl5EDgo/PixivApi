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

        var database = await databaseFactory.CreateAsync(token).ConfigureAwait(false);
        var logger = Context.Logger;
        var requestSender = Context.ServiceProvider.GetRequiredService<RequestSender>();
        ulong update = 0;
        try
        {
            var collection = database.ArtworkFilterAsync(artworkFilter, token);
            await foreach (var item in collection)
            {
            RETRY:
                try
                {
                    var artwork = await GetArtworkDetailAsync(requestSender, item.Id, token).ConfigureAwait(false);
                    if (artwork.User.Id == 0)
                    {
                        goto REMOVED;
                    }

                    ++update;
                    if (item.Type == ArtworkType.Ugoira && item.UgoiraFrames is null)
                    {
                        item.UgoiraFrames = await GetArtworkUgoiraMetadataAsync(requestSender, artwork.Id, token).ConfigureAwait(false);
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
                }
                catch (HttpRequestException e) when (e.StatusCode.HasValue)
                {
                    if (e.StatusCode.Value == HttpStatusCode.NotFound)
                    {
                        goto REMOVED;
                    }

                    if (!System.Console.IsOutputRedirected)
                    {
                        logger.LogWarning($"{VirtualCodes.BrightYellowColor}Reconnect. Wait for {configSettings.RetryTimeSpan.TotalSeconds} seconds. Time: {DateTime.Now} Restart: {DateTime.Now.Add(configSettings.RetryTimeSpan)}{VirtualCodes.NormalizeColor}");
                    }

                    await Task.Delay(configSettings.RetryTimeSpan, token).ConfigureAwait(false);
                    goto RETRY;
                }

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
        }
    }

    private static async ValueTask<ArtworkResponseContent> GetArtworkDetailAsync(RequestSender requestSender, ulong id, CancellationToken token)
    {
        var url = $"https://{ApiHost}/v1/illust/detail?illust_id={id}";
        using var responseMessage = await requestSender.GetAsync(url, token).ConfigureAwait(false);
        var content = await responseMessage.Content.ReadAsByteArrayAsync(token).ConfigureAwait(false);
        var response = IOUtility.JsonDeserialize<IllustDateilResponseData>(content.AsSpan());
        return response.Illust;
    }

    private static async ValueTask<ushort[]> GetArtworkUgoiraMetadataAsync(RequestSender requestSender, ulong id, CancellationToken token)
    {
        var ugoiraUrl = $"https://{ApiHost}/v1/ugoira/metadata?illust_id={id}";
        using var responseMessage = await requestSender.GetAsync(ugoiraUrl, token).ConfigureAwait(false);
        var content = await responseMessage.Content.ReadAsByteArrayAsync(token).ConfigureAwait(false);
        var ugoiraResponse = IOUtility.JsonDeserialize<UgoiraMetadataResponseData>(content.AsSpan());
        var frames = ugoiraResponse.Value.Frames.Length == 0 ? Array.Empty<ushort>() : new ushort[ugoiraResponse.Value.Frames.Length];
        for (var frameIndex = 0; frameIndex < frames.Length; frameIndex++)
        {
            frames[frameIndex] = (ushort)ugoiraResponse.Value.Frames[frameIndex].Delay;
        }

        return frames;
    }
}
