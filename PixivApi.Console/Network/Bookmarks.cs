using PixivApi.Core;
using PixivApi.Core.Network;

namespace PixivApi.Console;

public partial class NetworkClient
{
    [Command("bookmarks")]
    public async ValueTask<int> DownloadBookmarksOfUserAsync
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

        if (configSettings.UserId == 0UL)
        {
            logger.LogError($"{ConsoleUtility.ErrorColor}User Id should be written in appsettings.json{ConsoleUtility.NormalizeColor}");
            return -1;
        }

        if (!await Connect().ConfigureAwait(false))
        {
            return -3;
        }

        var token = Context.CancellationToken;
        var database = await IOUtility.MessagePackDeserializeAsync<Core.Local.DatabaseFile>(output, token).ConfigureAwait(false) ?? new();
        var add = 0UL;
        var update = 0UL;
        try
        {
            await foreach (var artworkEnumerable in new DownloadArtworkAsyncEnumerable($"https://{ApiHost}/v1/user/bookmarks/illust?user_id={configSettings.UserId}&restrict=public", RetryGetAsync, ReconnectAsync, pipe).WithCancellation(token))
            {
                var oldAdd = add;
                foreach (var item in artworkEnumerable)
                {
                    if (token.IsCancellationRequested)
                    {
                        return 0;
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
                await IOUtility.MessagePackSerializeAsync(output, database, FileMode.Create).ConfigureAwait(false);
            }

            if (!pipe)
            {
                logger.LogInformation($"Total: {database.ArtworkDictionary.Count} Add: {add} Update: {update}");
            }
        }

        return 0;
    }
}
