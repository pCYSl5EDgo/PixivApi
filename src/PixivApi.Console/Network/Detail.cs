namespace PixivApi.Console;

public partial class NetworkClient
{
    [Command("update")]
    public async ValueTask UpdateAsync
    (
        bool pipe = false
    )
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

        var database = await IOUtility.MessagePackDeserializeAsync<DatabaseFile>(configSettings.DatabaseFilePath, token).ConfigureAwait(false) ?? new();
        var authentication = await ConnectAsync(token).ConfigureAwait(false);

        ulong update = 0;
        try
        {
            await foreach (var item in FilterExtensions.CreateAsyncEnumerable(finder, database, artworkFilter, token))
            {
            RETRY:
                try
                {
                    var artwork = await GetArtworkDetailAsync(item.Id, authentication, pipe, token).ConfigureAwait(false);
                    if (artwork.User.Id == 0)
                    {
                        goto REMOVED;
                    }

                    ++update;
                    if (item.Type == ArtworkType.Ugoira && item.UgoiraFrames is null)
                    {
                        item.UgoiraFrames = await GetArtworkUgoiraMetadataAsync(artwork.Id, authentication, pipe, token).ConfigureAwait(false);
                    }

                    LocalNetworkConverter.Overwrite(item, artwork, database.TagSet, database.ToolSet, database.UserDictionary);
                    if (pipe)
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

                    if (e.StatusCode.Value != HttpStatusCode.BadRequest)
                    {
                        logger.LogError(e, "");
                        continue;
                    }

                    if (!pipe)
                    {
                        logger.LogWarning($"{VirtualCodes.BrightYellowColor}Reconnect. Wait for {configSettings.RetryTimeSpan.TotalSeconds} seconds. Time: {DateTime.Now} Restart: {DateTime.Now.Add(configSettings.RetryTimeSpan)}{VirtualCodes.NormalizeColor}");
                    }

                    await Task.Delay(configSettings.RetryTimeSpan, token).ConfigureAwait(false);
                    authentication = await ReconnectAsync(e, pipe, token).ConfigureAwait(false);

                    goto RETRY;
                }

            REMOVED:
                item.IsOfficiallyRemoved = true;
                if (!pipe)
                {
                    logger.LogInformation($"{++update,4}: {item.Id,20} removed");
                }
            }
        }
        finally
        {
            if (update != 0)
            {
                await IOUtility.MessagePackSerializeAsync(configSettings.DatabaseFilePath, database, FileMode.Create).ConfigureAwait(false);
            }

            if (!pipe)
            {
                logger.LogInformation($"Total: {database.ArtworkDictionary.Count} Update: {update}");
            }
        }
    }

    private async ValueTask<ArtworkResponseContent> GetArtworkDetailAsync(ulong id, AuthenticationHeaderValue authentication, bool pipe, CancellationToken token)
    {
        var url = $"https://{ApiHost}/v1/illust/detail?illust_id={id}";
        var content = await RetryGetAsync(url, authentication, pipe, token).ConfigureAwait(false);
        var response = IOUtility.JsonDeserialize<IllustDateilResponseData>(content.AsSpan());
        return response.Illust;
    }

    private async ValueTask<ushort[]> GetArtworkUgoiraMetadataAsync(ulong id, AuthenticationHeaderValue authentication, bool pipe, CancellationToken token)
    {
        var ugoiraUrl = $"https://{ApiHost}/v1/ugoira/metadata?illust_id={id}";
        var ugoiraResponse = IOUtility.JsonDeserialize<UgoiraMetadataResponseData>((await RetryGetAsync(ugoiraUrl, authentication, pipe, token).ConfigureAwait(false)).AsSpan());
        var frames = ugoiraResponse.Value.Frames.Length == 0 ? Array.Empty<ushort>() : new ushort[ugoiraResponse.Value.Frames.Length];
        for (var frameIndex = 0; frameIndex < frames.Length; frameIndex++)
        {
            frames[frameIndex] = (ushort)ugoiraResponse.Value.Frames[frameIndex].Delay;
        }

        return frames;
    }
}