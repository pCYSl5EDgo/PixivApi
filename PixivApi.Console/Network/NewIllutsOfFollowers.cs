using PixivApi.Core.Network;

namespace PixivApi.Console;

public partial class NetworkClient
{
    [Command("follows-new-work")]
    public async ValueTask DownloadNewIllustsOfFollowersAsync
    (
        [Option(0, $"output {ArgumentDescriptions.DatabaseDescription}")] string output,
        [Option("a", ArgumentDescriptions.AddKindDescription)] bool addBehaviour = false,
        bool pipe = false
    )
    {
        if (string.IsNullOrWhiteSpace(output))
        {
            return;
        }

        var token = Context.CancellationToken;
        var authentication = await ConnectAsync(token).ConfigureAwait(false);
        var database = await IOUtility.MessagePackDeserializeAsync<DatabaseFile>(output, token).ConfigureAwait(false) ?? new();
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
                        (_, v) =>
                        {
                            ++update;
                            LocalNetworkConverter.Overwrite(v, item, database.TagSet, database.ToolSet, database.UserDictionary);
                            return v;
                        }
                    );
                }

                if (!addBehaviour && add == oldAdd)
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
