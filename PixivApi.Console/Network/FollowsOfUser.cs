using PixivApi.Core.Network;

namespace PixivApi.Console;

partial class NetworkClient
{
    [Command("follows")]
    public async ValueTask<int> DownloadFollowsOfUserAsync
    (
        [Option(0, $"output {IOUtility.ArtworkDatabaseDescription}")] string output,
        [Option(null, IOUtility.OverwriteKindDescription)] string overwrite = "add",
        bool pipe = false
    )
    {
        if (string.IsNullOrWhiteSpace(output))
        {
            return -1;
        }

        if (config.UserId == 0UL)
        {
            logger.LogError($"{IOUtility.ErrorColor}User Id should be written in appsettings.json{IOUtility.NormalizeColor}");
            return -1;
        }

        if (!await Connect().ConfigureAwait(false))
        {
            return -3;
        }

        var token = Context.CancellationToken;
        var overwriteKind = OverwriteKindExtensions.Parse(overwrite);
        var database = overwriteKind == OverwriteKind.ClearAndAdd ?
            null :
            await IOUtility.MessagePackDeserializeAsync<Core.Local.DatabaseFile>(output, token).ConfigureAwait(false);
        var dictionary = new ConcurrentDictionary<ulong, Core.Local.Artwork>();
        if (database is null)
        {
            overwriteKind = OverwriteKind.ClearAndAdd;
            database = new();
        }

        if (overwriteKind == OverwriteKind.ClearAndAdd)
        {
            database.UserDictionary = new();
        }
        else
        {
            foreach (var item in database.Artworks)
            {
                dictionary.TryAdd(item.Id, item);
            }
        }

        ulong add = 0UL, update = 0UL, addArtwork = 0UL, updateArtwork = 0UL;
        var enumerator = new DownloadUserPreviewAsyncEnumerable(RetryGetAsync, $"https://{ApiHost}/v1/user/following?user_id={config.UserId}").GetAsyncEnumerator(token);
        try
        {
            while (await enumerator.MoveNextAsync().ConfigureAwait(false))
            {
                var c = enumerator.Current;
                var oldAdd = add;
                var oldUpdate = update;
                await Parallel.ForEachAsync(c, token, (item, token) =>
                {
                    token.ThrowIfCancellationRequested();
                    Core.Local.User converted = item;
                    database.UserDictionary.AddOrUpdate(item.User.Id,
                        _ =>
                        {
                            var added = Interlocked.Increment(ref add);
                            if (pipe)
                            {
                                logger.LogInformation($"{converted.Id}");
                            }
                            else
                            {
                                logger.LogInformation($"{added,4}: {converted.Id,20}");
                            }
                            return converted;
                        },
                        (_, v) =>
                        {
                            Interlocked.Increment(ref update);
                            v.Overwrite(converted);
                            return v;
                        });

                    if (item.Illusts is { Length: > 0 } artworks)
                    {
                        foreach (var artwork in artworks)
                        {
                            token.ThrowIfCancellationRequested();
                            var convertedArtwork = Core.Local.Artwork.ConvertFromNetwrok(artwork, database.TagSet, database.ToolSet, database.UserDictionary);
                            dictionary.AddOrUpdate(artwork.Id,
                                _ =>
                                {
                                    Interlocked.Increment(ref addArtwork);
                                    return convertedArtwork;
                                },
                                (_, v) =>
                                {
                                    Interlocked.Increment(ref updateArtwork);
                                    v.Overwrite(convertedArtwork);
                                    return v;
                                });
                        }
                    }

                    return ValueTask.CompletedTask;
                }).ConfigureAwait(false);

                if (overwriteKind == OverwriteKind.Add && add == oldAdd)
                {
                    break;
                }
            }
        }
        finally
        {
            if (enumerator is not null)
            {
                await enumerator.DisposeAsync().ConfigureAwait(false);
            }

            if (addArtwork != 0 || updateArtwork != 0)
            {
                database.Artworks = dictionary.Values.ToArray();
            }

            if (add != 0 || update != 0)
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
