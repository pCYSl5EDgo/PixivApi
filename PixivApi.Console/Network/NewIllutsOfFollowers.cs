using PixivApi.Core;
using PixivApi.Core.Network;

namespace PixivApi.Console;

public partial class NetworkClient
{
    [Command("follows-new-work")]
    public async ValueTask DownloadNewIllustsOfFollowersAsync
    (
        [Option(0, $"output {ArgumentDescriptions.DatabaseDescription}")] string output,
        [Option("o", ArgumentDescriptions.OverwriteKindDescription)] OverwriteKind overwrite = OverwriteKind.diff,
        bool pipe = false
    )
    {
        if (string.IsNullOrWhiteSpace(output))
        {
            return;
        }

        var token = Context.CancellationToken;
        var authentication = await ConnectAsync(token).ConfigureAwait(false);
        var database = await IOUtility.MessagePackDeserializeAsync<Core.Local.DatabaseFile>(output, token).ConfigureAwait(false) ?? new();
        ulong add = 0UL, update = 0UL;
        try
        {
            var url = $"https://{ApiHost}/v2/illust/follow?restrict=public";
            await foreach (var artworkCollection in new DownloadArtworkAsyncEnumerable(url, authentication, RetryGetAsync, ReconnectAsync, pipe).WithCancellation(token))
            {
                var oldAdd = add;
                foreach (var item in artworkCollection)
                {
                    if (token.IsCancellationRequested)
                    {
                        return;
                    }

                    var converted = Core.Local.Artwork.ConvertFromNetwrok(item, database.TagSet, database.ToolSet, database.UserDictionary);
                    database.ArtworkDictionary.AddOrUpdate(
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

                if (overwrite == OverwriteKind.diff && add == oldAdd)
                {
                    break;
                }
            }
        }
        finally
        {
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
