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

        static async ValueTask<DatabaseFile?> LoadDatabaseAsync(string output, bool addBehaviour, CancellationToken token)
        {
            var database = await IOUtility.MessagePackDeserializeAsync<DatabaseFile>(output, token).ConfigureAwait(false);
            if (addBehaviour && database is not null)
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

            return database;
        }

        var token = Context.CancellationToken;
        var databaseTask = LoadDatabaseAsync(configSettings.DatabaseFilePath, addBehaviour, token);
        var database = default(DatabaseFile);
        var responseList = default(List<UserPreviewResponseContent>);
        var requestSender = Context.ServiceProvider.GetRequiredService<RequestSender>();
        var url = $"https://{ApiHost}/v1/user/following?user_id={configSettings.UserId}";
        ulong add = 0UL, update = 0UL, addArtwork = 0UL, updateArtwork = 0UL;

        bool RegisterNotShow(DatabaseFile database, UserPreviewResponseContent item)
        {
            var isAdd = true;
            _ = database.UserDictionary.AddOrUpdate(item.User.Id, _ => item.Convert(), (_, v) =>
                {
                    isAdd = false;
                    LocalNetworkConverter.Overwrite(v, item);
                    return v;
                });

            if (item.Illusts is { Length: > 0 } artworks)
            {
                foreach (var artwork in artworks)
                {
                    _ = database.ArtworkDictionary.AddOrUpdate(artwork.Id,
                        _ =>
                        {
                            ++addArtwork;
                            return LocalNetworkConverter.Convert(artwork, database.TagSet, database.ToolSet, database.UserDictionary);
                        },
                        (_, v) =>
                        {
                            ++updateArtwork;
                            LocalNetworkConverter.Overwrite(v, artwork, database.TagSet, database.ToolSet, database.UserDictionary);
                            return v;
                        });
                }
            }

            return isAdd;
        }

        void RegisterShow(DatabaseFile database, UserPreviewResponseContent item)
        {
            _ = database.UserDictionary.AddOrUpdate(item.User.Id,
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
                    _ = database.ArtworkDictionary.AddOrUpdate(artwork.Id,
                        _ =>
                        {
                            ++addArtwork;
                            return LocalNetworkConverter.Convert(artwork, database.TagSet, database.ToolSet, database.UserDictionary);
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

                    database = await databaseTask.ConfigureAwait(false) ?? new();
                    if (responseList is { Count: > 0 })
                    {
                        foreach (var item in responseList)
                        {
                            (RegisterNotShow(database, item) ? ref add : ref update)++;
                        }

                        responseList = null;
                    }
                }

                var oldAdd = add;
                foreach (var item in userPreviewCollection)
                {
                    RegisterShow(database, item);
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
                database = await databaseTask.ConfigureAwait(false) ?? new();
                if (responseList is { Count: > 0 })
                {
                    foreach (var item in responseList)
                    {
                        (RegisterNotShow(database, item) ? ref add : ref update)++;
                    }

                    responseList = null;
                }
            }

            if (add != 0 || update != 0 || addArtwork != 0 || updateArtwork != 0)
            {
                await IOUtility.MessagePackSerializeAsync(configSettings.DatabaseFilePath, database, FileMode.Create).ConfigureAwait(false);
            }

            if (!System.Console.IsOutputRedirected)
            {
                logger.LogInformation($"User Total: {database.UserDictionary.Count} Add: {add} Update: {update}    Artwork Total: {database.ArtworkDictionary.Count} Add: {addArtwork} Update: {updateArtwork}");
            }
        }
    }
}
