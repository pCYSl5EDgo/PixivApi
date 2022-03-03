using PixivApi.Core.Network;

namespace PixivApi.Console;

public partial class NetworkClient
{
    [Command("follows")]
    public async ValueTask DownloadFollowsOfUserAsync
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

        if (configSettings.UserId == 0UL)
        {
            logger.LogError($"{VirtualCodes.BrightRedColor}User Id should be written in appsettings.json{VirtualCodes.NormalizeColor}");
            return;
        }

        var token = Context.CancellationToken;
        var database = await IOUtility.MessagePackDeserializeAsync<DatabaseFile>(output, token).ConfigureAwait(false) ?? new();
        if (addBehaviour)
        {
            await Parallel.ForEachAsync(database.UserDictionary.Values, token, (user, token) =>
            {
                if (token.IsCancellationRequested)
                {
                    return ValueTask.FromCanceled(token);
                }

                user.IsFollowed = false;
                return ValueTask.CompletedTask;
            }).ConfigureAwait(false);
        }

        var authentication = await ConnectAsync(token).ConfigureAwait(false);
        var url = $"https://{ApiHost}/v1/user/following?user_id={configSettings.UserId}";
        ulong add = 0UL, update = 0UL, addArtwork = 0UL, updateArtwork = 0UL;
        try
        {
            await foreach (var userPreviewCollection in new DownloadUserPreviewAsyncEnumerable(url, authentication, RetryGetAsync, ReconnectAsync, pipe).WithCancellation(token))
            {
                var oldAdd = add;
                foreach (var item in userPreviewCollection)
                {
                    if (token.IsCancellationRequested)
                    {
                        return;
                    }

                    _ = database.UserDictionary.AddOrUpdate(item.User.Id,
                        _ =>
                        {
                            ++add;
                            if (pipe)
                            {
                                logger.LogInformation($"{item.User.Id}");
                            }
                            else
                            {
                                logger.LogInformation($"{add,4}: {item.User.Id,20}");
                            }

                            return item.Convert();
                        },
                        (_, v) =>
                        {
                            ++update;
                            LocalNetworkConverter.Overwrite(v, item);
                            return v;
                        });

                    if (item.Illusts is { Length: > 0 } artworks)
                    {
                        foreach (var artwork in artworks)
                        {
                            if (token.IsCancellationRequested)
                            {
                                return;
                            }

                            _ = database.ArtworkDictionary.AddOrUpdate(artwork.Id,
                                _ =>
                                {
                                    ++addArtwork;
                                    var convertedArtwork = LocalNetworkConverter.Convert(artwork, database.TagSet, database.ToolSet, database.UserDictionary);
                                    return convertedArtwork;
                                },
                                (_, v) =>
                                {
                                    ++updateArtwork;
                                    LocalNetworkConverter.Overwrite(v, artwork, database.TagSet, database.ToolSet, database.UserDictionary);
                                    return v;
                                });
                        }
                    }
                }

                if (!addBehaviour && add == oldAdd)
                {
                    break;
                }
            }
        }
        finally
        {
            if (add != 0 || update != 0 || addArtwork != 0 || updateArtwork != 0)
            {
                await IOUtility.MessagePackSerializeAsync(output, database, FileMode.Create).ConfigureAwait(false);
            }

            if (!pipe)
            {
                logger.LogInformation($"User Total: {database.UserDictionary.Count} Add: {add} Update: {update}    Artwork Total: {database.ArtworkDictionary.Count} Add: {addArtwork} Update: {updateArtwork}");
            }
        }
    }
}
