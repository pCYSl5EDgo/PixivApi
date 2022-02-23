using PixivApi.Core;
using PixivApi.Core.Network;

namespace PixivApi.Console;

public partial class NetworkClient
{
    [Command("follows-new-work")]
    public async ValueTask<int> DownloadNewIllustsOfFollowersAsync
    (
        [Option(0, $"output {ArgumentDescriptions.DatabaseDescription}")] string output,
        [Option("o", ArgumentDescriptions.OverwriteKindDescription)] OverwriteKind overwrite = OverwriteKind.add,
        bool pipe = false
    )
    {
        if (string.IsNullOrWhiteSpace(output))
        {
            return -1;
        }

        if (!await Connect().ConfigureAwait(false))
        {
            return -3;
        }

        var token = Context.CancellationToken;
        var database = await IOUtility.MessagePackDeserializeAsync<Core.Local.DatabaseFile>(output, token).ConfigureAwait(false) ?? new();
        var dictionary = new ConcurrentDictionary<ulong, Core.Local.Artwork>();
        foreach (var item in database.Artworks)
        {
            dictionary.TryAdd(item.Id, item);
        }

        var add = 0UL;
        var update = 0UL;
        try
        {
            await foreach (var artworkCollection in new DownloadArtworkAsyncEnumerable($"https://{ApiHost}/v2/illust/follow?restrict=public", RetryGetAsync, ReconnectAsync, pipe).WithCancellation(token))
            {
                var oldAdd = add;
                foreach (var item in artworkCollection)
                {
                    if (token.IsCancellationRequested)
                    {
                        return 0;
                    }

                    var converted = Core.Local.Artwork.ConvertFromNetwrok(item, database.TagSet, database.ToolSet, database.UserDictionary);
                    dictionary.AddOrUpdate(
                        item.Id,
                        _ =>
                        {
                            ++add;
                            if (pipe)
                            {
                                logger.LogInformation($"{converted.Id}");
                            }
                            else
                            {
                                logger.LogInformation($"{add,4}: {converted.Id,20}");
                            }
                            return converted;
                        },
                        (_, v) =>
                        {
                            ++update;
                            v.Overwrite(converted);
                            return v;
                        }
                    );
                }

                if (overwrite == OverwriteKind.add && add == oldAdd)
                {
                    break;
                }
            }
        }
        finally
        {
            if (add != 0 || update != 0)
            {
                database.Artworks = dictionary.Values.ToArray();
                await IOUtility.MessagePackSerializeAsync(output, database, FileMode.Create).ConfigureAwait(false);
            }

            if (!pipe)
            {
                logger.LogInformation($"Total: {database.Artworks.Length} Add: {add} Update: {update}");
            }
        }

        return 0;
    }
}
