using PixivApi.Core;
using PixivApi.Core.Network;

namespace PixivApi.Console;

public partial class NetworkClient
{
    [Command("follows")]
    public async ValueTask<int> DownloadFollowsOfUserAsync
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
        var dictionary = new ConcurrentDictionary<ulong, Core.Local.Artwork>();
        foreach (var item in database.Artworks)
        {
            if (token.IsCancellationRequested)
            {
                return 0;
            }

            dictionary.TryAdd(item.Id, item);
        }

        ulong add = 0UL, update = 0UL, addArtwork = 0UL, updateArtwork = 0UL;
        try
        {
            await foreach (var userPreviewCollection in new DownloadUserPreviewAsyncEnumerable(RetryGetAsync, $"https://{ApiHost}/v1/user/following?user_id={configSettings.UserId}", ReconnectAsync, pipe).WithCancellation(token))
            {
                var oldAdd = add;
                foreach (var item in userPreviewCollection)
                {
                    if (token.IsCancellationRequested)
                    {
                        return 0;
                    }

                    Core.Local.User converted = item;
                    database.UserDictionary.AddOrUpdate(item.User.Id,
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
                        });

                    if (item.Illusts is { Length: > 0 } artworks)
                    {
                        foreach (var artwork in artworks)
                        {
                            if (token.IsCancellationRequested)
                            {
                                return 0;
                            }

                            var convertedArtwork = Core.Local.Artwork.ConvertFromNetwrok(artwork, database.TagSet, database.ToolSet, database.UserDictionary);
                            dictionary.AddOrUpdate(artwork.Id,
                                _ =>
                                {
                                    ++addArtwork;
                                    return convertedArtwork;
                                },
                                (_, v) =>
                                {
                                    ++updateArtwork;
                                    v.Overwrite(convertedArtwork);
                                    return v;
                                });
                        }
                    }
                }

                if (overwrite == OverwriteKind.add && add == oldAdd)
                {
                    break;
                }
            }
        }
        finally
        {
            if (addArtwork != 0 || updateArtwork != 0)
            {
                database.Artworks = dictionary.Values.ToArray();
            }

            if (add != 0 || update != 0 || addArtwork != 0 || updateArtwork != 0)
            {
                await IOUtility.MessagePackSerializeAsync(output, database, FileMode.Create).ConfigureAwait(false);
            }

            if (!pipe)
            {
                logger.LogInformation($"User Total: {database.UserDictionary.Count} Add: {add} Update: {update}    Artwork Total: {database.Artworks.Length} Add: {addArtwork} Update: {updateArtwork}");
            }
        }

        return 0;
    }
}
