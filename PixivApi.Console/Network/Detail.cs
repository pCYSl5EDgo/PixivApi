using PixivApi.Core.Local;
using PixivApi.Core.Local.Filter;

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
        var search = (await ArtworkEnumerable.CreateAsync(database, artworkFilter, token).ConfigureAwait(false)!);
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
                try
                {
                    var url = $"https://{ApiHost}/v1/illust/detail?illust_id={item.Id}";
                    var content = await RetryGetAsync(url, token).ConfigureAwait(false);
                    var response = IOUtility.JsonDeserialize<Core.Network.IllustDateilResponseData>(content.AsSpan());
                    var converted = Artwork.ConvertFromNetwrok(response.Illust, database.TagSet, database.ToolSet, database.UserDictionary);
                    var updated = Interlocked.Increment(ref update);
                    item.Overwrite(converted);
                    if (pipe)
                    {
                        logger.LogInformation($"{converted.Id}");
                    }
                    else
                    {
                        logger.LogInformation($"{update,4}: {converted.Id,20}");
                    }
                }
                catch (Exception e) when (e is not TaskCanceledException)
                {
                    logger.LogError(e, "");
                }
            }
        }
        finally
        {
            if (update != 0)
            {
                await IOUtility.MessagePackSerializeAsync(output, database, FileMode.Create).ConfigureAwait(false);
            }
        }

        if (!pipe)
        {
            logger.LogInformation($"Total: {database.Artworks.Length} Update: {update}");
        }

        return 0;
    }
}
