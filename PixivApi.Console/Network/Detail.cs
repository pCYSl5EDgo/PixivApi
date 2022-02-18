using PixivApi.Core.Local;
using PixivApi.Core.Local.Filter;
using System.Net;
using System.Runtime.ExceptionServices;

namespace PixivApi.Console;

partial class NetworkClient
{
    [Command("detail")]
    public async ValueTask<int> DetailAsync
    (
        [Option(0, $"output {IOUtility.ArtworkDatabaseDescription}")] string output,
        [Option(1, $"{IOUtility.FilterDescription}")] string filter,
        bool pipe = false
    )
    {
        if (string.IsNullOrWhiteSpace(output) || string.IsNullOrWhiteSpace(filter))
        {
            return -1;
        }

        var token = Context.CancellationToken;
        var artworkFilter = await IOUtility.JsonDeserializeAsync<ArtworkFilter>(filter, token).ConfigureAwait(false);
        if (artworkFilter is null)
        {
            return -1;
        }

        var database = await IOUtility.MessagePackDeserializeAsync<DatabaseFile>(output, token).ConfigureAwait(false) ?? new();
        var parallelOptions = new ParallelOptions()
        {
            CancellationToken = token,
            MaxDegreeOfParallelism = config.MaxParallel,
        };
        var search = (await ArtworkEnumerable.CreateAsync(database, artworkFilter, parallelOptions).ConfigureAwait(false)!);
        if (search is null)
        {
            return 0;
        }

        if (!await Connect().ConfigureAwait(false))
        {
            return -3;
        }

        ulong update = 0;
        try
        {
            foreach (var item in search)
            {
            RETRY:
                try
                {
                    var artwork = await GetArtworkDetailAsync(item.Id, token).ConfigureAwait(false);
                    if (artwork.User.Id == 0)
                    {
                        item.IsOfficiallyRemoved = true;
                    }
                    else
                    {
                        var converted = Artwork.ConvertFromNetwrok(artwork, database.TagSet, database.ToolSet, database.UserDictionary);
                        var updated = Interlocked.Increment(ref update);
                        if (item.Type == ArtworkType.Ugoira && item.UgoiraFrames is null)
                        {
                            var ugoiraUrl = $"https://{ApiHost}/v1/ugoira/metadata?illust_id={item.Id}";
                            var ugoiraResponse = IOUtility.JsonDeserialize<Core.Network.UgoiraMetadataResponseData>((await RetryGetAsync(ugoiraUrl, token).ConfigureAwait(false)).AsSpan());
                            item.UgoiraFrames = ugoiraResponse.Value.Frames.Length == 0 ? Array.Empty<ushort>() : new ushort[ugoiraResponse.Value.Frames.Length];
                            for (int frameIndex = 0; frameIndex < item.UgoiraFrames.Length; frameIndex++)
                            {
                                item.UgoiraFrames[frameIndex] = (ushort)ugoiraResponse.Value.Frames[frameIndex].Delay;
                            }
                        }

                        item.Overwrite(converted);
                    }

                    if (pipe)
                    {
                        logger.LogInformation($"{item.Id}");
                    }
                    else
                    {
                        logger.LogInformation($"{update,4}: {item.Id,20}");
                    }
                }
                catch (HttpRequestException e) when (e.StatusCode.HasValue)
                {
                    if (e.StatusCode.Value == HttpStatusCode.NotFound)
                    {
                        item.IsOfficiallyRemoved = true;
                        var updated = Interlocked.Increment(ref update);
                        logger.LogInformation($"{updated,4}: {item.Id,20} removed");
                        continue;
                    }
                    else if (e.StatusCode.Value == HttpStatusCode.BadRequest)
                    {
                        if (!pipe)
                        {
                            logger.LogWarning($"Reconnect. Wait for {config.RetryTimeSpan.TotalSeconds} seconds.");
                        }

                        await Task.Delay(config.RetryTimeSpan, token).ConfigureAwait(false);
                        if (!(await Reconnect().ConfigureAwait(false)))
                        {
                            ExceptionDispatchInfo.Throw(e);
                        }

                        goto RETRY;
                    }
                    else
                    {
                        logger.LogError(e, "");
                    }
                }
            }
        }
        finally
        {
            if (update != 0)
            {
                await IOUtility.MessagePackSerializeAsync(output, database, FileMode.Create).ConfigureAwait(false);
            }

            if (!pipe)
            {
                logger.LogInformation($"Total: {database.Artworks.Length} Update: {update}");
            }
        }

        return 0;
    }

    private async ValueTask<Core.Network.Artwork> GetArtworkDetailAsync(ulong id, CancellationToken token)
    {
        var url = $"https://{ApiHost}/v1/illust/detail?illust_id={id}";
        var content = await RetryGetAsync(url, token).ConfigureAwait(false);
        var response = IOUtility.JsonDeserialize<Core.Network.IllustDateilResponseData>(content.AsSpan());
        return response.Illust;
    }
}
