namespace PixivApi.Console;

public partial class NetworkClient
{
    [Command("follows")]
    public async ValueTask DownloadFollowsOfUserAsync
    (
        [Option("a", ArgumentDescriptions.AddKindDescription)] bool addBehaviour = false
    )
    {
        if (string.IsNullOrWhiteSpace(configSettings.DatabaseFilePath))
        {
            return;
        }

        var logger = Context.Logger;
        if (configSettings.UserId == 0UL)
        {
            logger.LogError($"{VirtualCodes.BrightRedColor}User Id should be written in appsettings.json{VirtualCodes.NormalizeColor}");
            return;
        }

        async ValueTask<IDatabase> LoadDatabaseAsync(string output, bool addBehaviour, CancellationToken token)
        {
            var database = await databaseFactory.CreateAsync(token).ConfigureAwait(false);
            if (addBehaviour)
            {
                await Parallel.ForEachAsync(database.EnumerableUserAsync(token), token, (user, token) =>
                {
                    if (token.IsCancellationRequested)
                    {
                        return ValueTask.FromCanceled(token);
                    }

                    user.IsFollowed = false;
                    return ValueTask.CompletedTask;
                }).ConfigureAwait(false);
            }

            return database;
        }

        var token = Context.CancellationToken;
        var databaseTask = LoadDatabaseAsync(configSettings.DatabaseFilePath, addBehaviour, token);
        var database = default(IDatabase);
        var responseList = default(List<UserPreviewResponseContent>);
        var requestSender = Context.ServiceProvider.GetRequiredService<RequestSender>();
        var url = $"https://{ApiHost}/v1/user/following?user_id={configSettings.UserId}";
        ulong add = 0UL, update = 0UL, addArtwork = 0UL, updateArtwork = 0UL;

        async ValueTask<bool> RegisterNotShow(IDatabase database, UserPreviewResponseContent item, CancellationToken token)
        {
            var isAdd = true;
            await database.AddOrUpdateAsync(item.User.Id, _ => ValueTask.FromResult(item.Convert()), (v, _) =>
                {
                    isAdd = false;
                    LocalNetworkConverter.Overwrite(v, item);
                    return ValueTask.CompletedTask;
                }, token).ConfigureAwait(false);

            if (item.Illusts is { Length: > 0 } artworks)
            {
                foreach (var artwork in artworks)
                {
                    await database.AddOrUpdateAsync(artwork.Id,
                        async token =>
                        {
                            ++addArtwork;
                            return await LocalNetworkConverter.ConvertAsync(artwork, database, database, database, token).ConfigureAwait(false);
                        },
                        async (v, token) =>
                        {
                            ++updateArtwork;
                            await LocalNetworkConverter.OverwriteAsync(v, artwork, database, database, database, token).ConfigureAwait(false);
                        }, token).ConfigureAwait(false);
                }
            }

            return isAdd;
        }

        async ValueTask RegisterShow(IDatabase database, UserPreviewResponseContent item)
        {
            await database.AddOrUpdateAsync(item.User.Id,
                _ =>
                {
                    ++add;
                    if (System.Console.IsOutputRedirected)
                    {
                        logger.LogInformation($"{item.User.Id}");
                    }
                    else
                    {
                        logger.LogInformation($"{add,4}: {item.User.Id,20}");
                    }

                    return ValueTask.FromResult(item.Convert());
                },
                (v, _) =>
                {
                    ++update;
                    LocalNetworkConverter.Overwrite(v, item);
                    return ValueTask.CompletedTask;
                }, token).ConfigureAwait(false);

            if (item.Illusts is { Length: > 0 } artworks)
            {
                foreach (var artwork in artworks)
                {
                    await database.AddOrUpdateAsync(artwork.Id,
                        async token =>
                        {
                            ++addArtwork;
                            return await LocalNetworkConverter.ConvertAsync(artwork, database, database, database, token).ConfigureAwait(false);
                        },
                        async (v, token) =>
                        {
                            ++updateArtwork;
                            await LocalNetworkConverter.OverwriteAsync(v, artwork, database, database, database, token).ConfigureAwait(false);
                        }, token).ConfigureAwait(false);
                }
            }
        }

        try
        {
            await foreach (var userPreviewCollection in new DownloadUserPreviewAsyncEnumerable(url, requestSender.GetAsync).WithCancellation(token))
            {
                token.ThrowIfCancellationRequested();
                if (database is null)
                {
                    if (!databaseTask.IsCompleted)
                    {
                        responseList ??= new List<UserPreviewResponseContent>(5010);
                        foreach (var item in userPreviewCollection)
                        {
                            responseList.Add(item);
                            if (System.Console.IsOutputRedirected)
                            {
                                logger.LogInformation($"{item.User.Id}");
                            }
                            else
                            {
                                logger.LogInformation($"{responseList.Count,4}: {item.User.Id}");
                            }
                        }

                        token.ThrowIfCancellationRequested();
                        continue;
                    }

                    database = await databaseTask.ConfigureAwait(false);
                    if (responseList is { Count: > 0 })
                    {
                        foreach (var item in responseList)
                        {
                            (await RegisterNotShow(database, item, token).ConfigureAwait(false) ? ref add : ref update)++;
                        }

                        responseList = null;
                    }
                }

                var oldAdd = add;
                foreach (var item in userPreviewCollection)
                {
                    await RegisterShow(database, item).ConfigureAwait(false);
                }

                if (!addBehaviour && add == oldAdd)
                {
                    break;
                }
            }
        }
        catch (TaskCanceledException)
        {
            logger.LogError("Accept cancel. Please wait for writing to the database file.");
        }
        finally
        {
            if (database is null)
            {
                database = await databaseTask.ConfigureAwait(false);
                if (responseList is { Count: > 0 })
                {
                    foreach (var item in responseList)
                    {
                        (await RegisterNotShow(database, item, token).ConfigureAwait(false) ? ref add : ref update)++;
                    }

                    responseList = null;
                }
            }

            if (!System.Console.IsOutputRedirected)
            {
                var artworkCount = await database.CountArtworkAsync(token).ConfigureAwait(false);
                var userCount = await database.CountUserAsync(token).ConfigureAwait(false);
                logger.LogInformation($"User Total: {userCount} Add: {add} Update: {update}    Artwork Total: {artworkCount} Add: {addArtwork} Update: {updateArtwork}");
            }
        }
    }
}
