﻿namespace PixivApi.Console;

public partial class NetworkClient
{
    [Command("follows")]
    public async ValueTask DownloadFollowsOfUserAsync
    (
        [Option("a", ArgumentDescriptions.AddKindDescription)] bool addBehaviour = false,
        bool allWork = false,
        [Option("d")] bool download = false,
        ulong? idOffset = null,
        [Option("p")] bool isPrivate = false
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

        var token = Context.CancellationToken;
        var requestSender = Context.ServiceProvider.GetRequiredService<RequestSender>();
        var url = $"https://{ApiHost}/v1/user/following?user_id={configSettings.UserId}";
        if (isPrivate)
        {
            url += "&restrict=private";
        }
        ulong add = 0UL, update = 0UL, addArtwork = 0UL, updateArtwork = 0UL, downloadCount = 0UL, transferByteCount = 0UL;
        var database = await databaseFactory.RentAsync(token).ConfigureAwait(false);
        var transactional = database as ITransactionalDatabase;
        if (transactional is not null)
        {
            await transactional.BeginTransactionAsync(token).ConfigureAwait(false);
        }

        try
        {
            if (download)
            {
                var filter = string.IsNullOrWhiteSpace(configSettings.ArtworkFilterFilePath) ? null : await filterFactory.CreateAsync(database, new FileInfo(configSettings.ArtworkFilterFilePath), token).ConfigureAwait(false);
                if (addBehaviour)
                {
                    (add, update, addArtwork, updateArtwork, downloadCount, transferByteCount) = await PrivateDownloadFollowsOfUser_Download_All_All_Async(database, requestSender, filter, url, idOffset, token).ConfigureAwait(false);
                }
                else
                {
                    (add, update, addArtwork, updateArtwork, downloadCount, transferByteCount) = await PrivateDownloadFollowsOfUser_Download_New_All_Async(database, requestSender, filter, url, idOffset, token).ConfigureAwait(false);
               }
            }
            else
            {
                if (addBehaviour)
                {
                    if (allWork)
                    {
                        (add, update, addArtwork, updateArtwork) = await PrivateDownloadFollowsOfUser_All_All_Async(database, requestSender, url, idOffset, token).ConfigureAwait(false);
                    }
                    else
                    {
                        (add, update, addArtwork, updateArtwork) = await PrivateDownloadFollowsOfUser_All_OnlyPreview_Async(database, requestSender, url, token).ConfigureAwait(false);
                    }
                }
                else
                {
                    if (allWork)
                    {
                        (add, update, addArtwork, updateArtwork) = await PrivateDownloadFollowsOfUser_New_All_Async(database, requestSender, url, idOffset, token).ConfigureAwait(false);
                    }
                    else
                    {
                        (add, update, addArtwork, updateArtwork) = await PrivateDownloadFollowsOfUser_New_OnlyPreview_Async(database, requestSender, url, token).ConfigureAwait(false);
                    }
                }
            }
        }
        catch (Exception e) when (transactional is not null && e is not TaskCanceledException && e is not OperationCanceledException)
        {
            await transactional.RollbackTransactionAsync(token).ConfigureAwait(false);
            transactional = null;
            throw;
        }
        finally
        {
            if (!System.Console.IsOutputRedirected)
            {
                var artworkCount = await database.CountArtworkAsync(CancellationToken.None).ConfigureAwait(false);
                var userCount = await database.CountUserAsync(CancellationToken.None).ConfigureAwait(false);
                logger.LogInformation($"User Total: {userCount} Add: {add} Update: {update}    Artwork Total: {artworkCount} Add: {addArtwork} Update: {updateArtwork} Download: {downloadCount} Transfer: {ByteAmountUtility.ToDisplayable(transferByteCount)}");
            }

            if (transactional is not null)
            {
                await transactional.EndTransactionAsync(CancellationToken.None).ConfigureAwait(false);
            }

            databaseFactory.Return(ref database);
        }
    }

    private async ValueTask<(ulong add, ulong update, ulong addArtwork, ulong updateArtwork)> PrivateDownloadFollowsOfUser_New_OnlyPreview_Async(IDatabase database, RequestSender requestSender, string url, CancellationToken token)
    {
        var logger = Context.Logger;
        var logInfo = logger.IsEnabled(LogLevel.Information) ? logger : null;
        var logDebug = logger.IsEnabled(LogLevel.Debug) ? logger : null;
        var logTrace = logger.IsEnabled(LogLevel.Trace) ? logger : null;
        ulong add = 0, update = 0, addArtwork = 0, updateArtwork = 0;
        try
        {
            await foreach (var collection in new DownloadUserPreviewAsyncEnumerable(url, requestSender.GetAsync, logger).WithCancellation(token))
            {
                if (token.IsCancellationRequested)
                {
                    goto RETURN;
                }

                var oldAdd = add;
                foreach (var item in collection)
                {
                    if (token.IsCancellationRequested)
                    {
                        goto RETURN;
                    }

                    if (await database.AddOrUpdateAsync(item.User.Id, _ => ValueTask.FromResult(LocalNetworkConverter.Convert(item)), (v, _) =>
                    {
                        LocalNetworkConverter.Overwrite(v, item);
                        return ValueTask.CompletedTask;
                    }, token).ConfigureAwait(false))
                    {
                        ++add;
                        logInfo?.LogInformation($"User-A {add,10}: {item.User.Id,20}");
                    }
                    else
                    {
                        ++update;
                        logDebug?.LogDebug($"User-U {update,10}: {item.User.Id,20}");
                    }

                    if (item.Illusts is not { Length: > 0 } illusts)
                    {
                        continue;
                    }

                    foreach (var illust in illusts)
                    {
                        if (token.IsCancellationRequested)
                        {
                            goto RETURN;
                        }

                        var isAddTask = database is IExtenededDatabase exteneded ?
                            exteneded.ArtworkAddOrUpdateAsync(illust, token) :
                            database.AddOrUpdateAsync(illust.Id,
                                token => LocalNetworkConverter.ConvertAsync(illust, database, database, database, token),
                                (v, token) => LocalNetworkConverter.OverwriteAsync(v, illust, database, database, database, token),
                                token);
                        if (await isAddTask.ConfigureAwait(false))
                        {
                            ++addArtwork;
                            logTrace?.LogTrace($"Art-A {addArtwork,10}: {illust.Id,20}");
                        }
                        else
                        {
                            ++updateArtwork;
                            logTrace?.LogTrace($"Art-U {updateArtwork,10}: {illust.Id,20}");
                        }
                    }
                }

                if (add == oldAdd)
                {
                    goto RETURN;
                }
            }
        }
        catch (Exception e)
        {
            logger.LogError(e, "Error happened");
        }

    RETURN:
        return (add, update, addArtwork, updateArtwork);
    }

    private async ValueTask<(ulong add, ulong update, ulong addArtwork, ulong updateArtwork)> PrivateDownloadFollowsOfUser_New_All_Async(IDatabase database, RequestSender requestSender, string url, ulong? idOffset, CancellationToken token)
    {
        var logger = Context.Logger;
        var logInfo = logger.IsEnabled(LogLevel.Information) ? logger : null;
        var logDebug = logger.IsEnabled(LogLevel.Debug) ? logger : null;
        var idOffsetDone = !idOffset.HasValue;
        ulong add = 0, update = 0, addArtwork = 0, updateArtwork = 0;
        try
        {
            await foreach (var collection in new DownloadUserPreviewAsyncEnumerable(url, requestSender.GetAsync, logger).WithCancellation(token))
            {
                if (token.IsCancellationRequested)
                {
                    goto RETURN;
                }

                var oldAdd = add;
                foreach (var item in collection)
                {
                    if (token.IsCancellationRequested)
                    {
                        goto RETURN;
                    }

                    if (await database.AddOrUpdateAsync(item.User.Id, _ => ValueTask.FromResult(LocalNetworkConverter.Convert(item)), (v, _) =>
                    {
                        LocalNetworkConverter.Overwrite(v, item);
                        return ValueTask.CompletedTask;
                    }, token).ConfigureAwait(false))
                    {
                        ++add;
                        logInfo?.LogInformation($"User-A {add,10}: {item.User.Id,20}");
                    }
                    else
                    {
                        ++update;
                        logDebug?.LogDebug($"User-U {update,10}: {item.User.Id,20}");
                    }

                    if (!idOffsetDone)
                    {
                        if (item.User.Id == idOffset)
                        {
                            idOffsetDone = true;
                        }
                        else
                        {
                            continue;
                        }
                    }

                    var illustsUrl = $"https://{ApiHost}/v1/user/illusts?user_id={item.User.Id}";
                    var (_addArtwork, _updateArtwork) = await PrivateDownloadAllArtworkResponses(illustsUrl, logger, database, requestSender, token).ConfigureAwait(false);
                    addArtwork += _addArtwork;
                    updateArtwork += _updateArtwork;
                }

                if (add == oldAdd)
                {
                    goto RETURN;
                }
            }
        }
        catch (Exception e)
        {
            logger.LogError(e, "Error happened");
        }

    RETURN:
        return (add, update, addArtwork, updateArtwork);
    }

    private async ValueTask<(ulong add, ulong update, ulong addArtwork, ulong updateArtwork)> PrivateDownloadFollowsOfUser_All_OnlyPreview_Async(IDatabase database, RequestSender requestSender, string url, CancellationToken token)
    {
        var logger = Context.Logger;
        var logInfo = logger.IsEnabled(LogLevel.Information) ? logger : null;
        var logDebug = logger.IsEnabled(LogLevel.Debug) ? logger : null;
        var logTrace = logger.IsEnabled(LogLevel.Trace) ? logger : null;
        ulong add = 0, update = 0, addArtwork = 0, updateArtwork = 0;
        try
        {
            await foreach (var collection in new DownloadUserPreviewAsyncEnumerable(url, requestSender.GetAsync, logger).WithCancellation(token))
            {
                if (token.IsCancellationRequested)
                {
                    goto RETURN;
                }

                foreach (var item in collection)
                {
                    if (token.IsCancellationRequested)
                    {
                        goto RETURN;
                    }

                    if (await database.AddOrUpdateAsync(item.User.Id, _ => ValueTask.FromResult(LocalNetworkConverter.Convert(item)), (v, _) =>
                    {
                        LocalNetworkConverter.Overwrite(v, item);
                        return ValueTask.CompletedTask;
                    }, token).ConfigureAwait(false))
                    {
                        ++add;
                        logInfo?.LogInformation($"User-A {add,10}: {item.User.Id,20}");
                    }
                    else
                    {
                        ++update;
                        logDebug?.LogDebug($"User-U {update,10}: {item.User.Id,20}");
                    }

                    if (item.Illusts is not { Length: > 0 } illusts)
                    {
                        continue;
                    }

                    foreach (var illust in illusts)
                    {
                        if (token.IsCancellationRequested)
                        {
                            goto RETURN;
                        }

                        var isAddTask = database is IExtenededDatabase exteneded ? exteneded.ArtworkAddOrUpdateAsync(illust, token) : database.AddOrUpdateAsync(illust.Id, token => LocalNetworkConverter.ConvertAsync(illust, database, database, database, token), (v, token) => LocalNetworkConverter.OverwriteAsync(v, illust, database, database, database, token), token);
                        if (await isAddTask.ConfigureAwait(false))
                        {
                            ++addArtwork;
                            logTrace?.LogInformation($"Art-A {addArtwork,10}: {illust.Id,20}");
                        }
                        else
                        {
                            ++updateArtwork;
                            logTrace?.LogTrace($"Art-U {updateArtwork,10}: {illust.Id,20}");
                        }
                    }
                }
            }
        }
        catch (Exception e)
        {
            logger.LogError(e, "Error happened");
        }

    RETURN:
        return (add, update, addArtwork, updateArtwork);
    }

    private async ValueTask<(ulong add, ulong update, ulong addArtwork, ulong updateArtwork)> PrivateDownloadFollowsOfUser_All_All_Async(IDatabase database, RequestSender requestSender, string url, ulong? idOffset, CancellationToken token)
    {
        var logger = Context.Logger;
        var logInfo = logger.IsEnabled(LogLevel.Information) ? logger : null;
        var logDebug = logger.IsEnabled(LogLevel.Debug) ? logger : null;
        var idOffsetDone = !idOffset.HasValue;
        ulong add = 0, update = 0, addArtwork = 0, updateArtwork = 0;
        try
        {
            await foreach (var collection in new DownloadUserPreviewAsyncEnumerable(url, requestSender.GetAsync, logger).WithCancellation(token))
            {
                if (token.IsCancellationRequested)
                {
                    goto RETURN;
                }

                foreach (var item in collection)
                {
                    if (token.IsCancellationRequested)
                    {
                        goto RETURN;
                    }

                    if (await database.AddOrUpdateAsync(item.User.Id, _ => ValueTask.FromResult(LocalNetworkConverter.Convert(item)), (v, _) =>
                    {
                        LocalNetworkConverter.Overwrite(v, item);
                        return ValueTask.CompletedTask;
                    }, token).ConfigureAwait(false))
                    {
                        ++add;
                        logInfo?.LogInformation($"User-A {add,10}: {item.User.Id,20}");
                    }
                    else
                    {
                        ++update;
                        logDebug?.LogDebug($"User-U {update,10}: {item.User.Id,20}");
                    }

                    if (!idOffsetDone)
                    {
                        if (item.User.Id == idOffset)
                        {
                            idOffsetDone = true;
                        }
                        else
                        {
                            continue;
                        }
                    }

                    var illustsUrl = $"https://{ApiHost}/v1/user/illusts?user_id={item.User.Id}";
                    var (_addArtwork, _updateArtwork) = await PrivateDownloadAllArtworkResponses(illustsUrl, logger, database, requestSender, token).ConfigureAwait(false);
                    addArtwork += _addArtwork;
                    updateArtwork += _updateArtwork;
                }
            }
        }
        catch (Exception e)
        {
            logger.LogError(e, "Error happened");
        }

    RETURN:
        return (add, update, addArtwork, updateArtwork);
    }

    private async ValueTask<(ulong add, ulong update, ulong addArtwork, ulong updateArtwork, ulong downloadCount, ulong transferByteCount)> PrivateDownloadFollowsOfUser_Download_New_All_Async(IDatabase database, RequestSender requestSender, ArtworkFilter? filter, string url, ulong? idOffset, CancellationToken token)
    {
        var logger = Context.Logger;
        var logInfo = logger.IsEnabled(LogLevel.Information) ? logger : null;
        var logDebug = logger.IsEnabled(LogLevel.Debug) ? logger : null;
        var idOffsetDone = !idOffset.HasValue;
        ulong add = 0, update = 0, addArtwork = 0, updateArtwork = 0, download = 0, transfer = 0;
        try
        {
            await foreach (var collection in new DownloadUserPreviewAsyncEnumerable(url, requestSender.GetAsync, logger).WithCancellation(token))
            {
                if (token.IsCancellationRequested)
                {
                    goto RETURN;
                }

                var oldAdd = add;
                foreach (var item in collection)
                {
                    if (token.IsCancellationRequested)
                    {
                        goto RETURN;
                    }

                    if (await database.AddOrUpdateAsync(item.User.Id, _ => ValueTask.FromResult(LocalNetworkConverter.Convert(item)), (v, _) =>
                    {
                        LocalNetworkConverter.Overwrite(v, item);
                        return ValueTask.CompletedTask;
                    }, token).ConfigureAwait(false))
                    {
                        ++add;
                        logInfo?.LogInformation($"User-A {add,10}: {item.User.Id,20}");
                    }
                    else
                    {
                        ++update;
                        logDebug?.LogDebug($"User-U {update,10}: {item.User.Id,20}");
                    }

                    if (!idOffsetDone)
                    {
                        if (item.User.Id == idOffset)
                        {
                            idOffsetDone = true;
                        }
                        else
                        {
                            continue;
                        }
                    }

                    var illustsUrl = $"https://{ApiHost}/v1/user/illusts?user_id={item.User.Id}";
                    var (_addArtwork, _updateArtwork, _download, _transfer) = await PrivateDownloadAllArtworkResponsesAndFiles(illustsUrl, logger, database, requestSender, filter, token).ConfigureAwait(false);
                    addArtwork += _addArtwork;
                    updateArtwork += _updateArtwork;
                    download += _download;
                    transfer += _transfer;
                }

                if (add == oldAdd)
                {
                    goto RETURN;
                }
            }
        }
        catch (Exception e)
        {
            logger.LogError(e, "Error happened");
        }

    RETURN:
        return (add, update, addArtwork, updateArtwork, download, transfer);
    }

    private async ValueTask<(ulong add, ulong update, ulong addArtwork, ulong updateArtwork, ulong downloadCount, ulong transferByteCount)> PrivateDownloadFollowsOfUser_Download_All_All_Async(IDatabase database, RequestSender requestSender, ArtworkFilter? filter, string url, ulong? idOffset, CancellationToken token)
    {
        var logger = Context.Logger;
        var logInfo = logger.IsEnabled(LogLevel.Information) ? logger : null;
        var logDebug = logger.IsEnabled(LogLevel.Debug) ? logger : null;
        var idOffsetDone = !idOffset.HasValue;
        ulong add = 0, update = 0, addArtwork = 0, updateArtwork = 0, download = 0, transfer = 0;
        try
        {
            await foreach (var collection in new DownloadUserPreviewAsyncEnumerable(url, requestSender.GetAsync, logger).WithCancellation(token))
            {
                if (token.IsCancellationRequested)
                {
                    goto RETURN;
                }

                foreach (var item in collection)
                {
                    if (token.IsCancellationRequested)
                    {
                        goto RETURN;
                    }

                    if (await database.AddOrUpdateAsync(item.User.Id, _ => ValueTask.FromResult(LocalNetworkConverter.Convert(item)), (v, _) =>
                    {
                        LocalNetworkConverter.Overwrite(v, item);
                        return ValueTask.CompletedTask;
                    }, token).ConfigureAwait(false))
                    {
                        ++add;
                        logInfo?.LogInformation($"User-A {add,10}: {item.User.Id,20}");
                    }
                    else
                    {
                        ++update;
                        logDebug?.LogDebug($"User-U {update,10}: {item.User.Id,20}");
                    }

                    if (!idOffsetDone)
                    {
                        if (item.User.Id == idOffset)
                        {
                            idOffsetDone = true;
                        }
                        else
                        {
                            continue;
                        }
                    }

                    var illustsUrl = $"https://{ApiHost}/v1/user/illusts?user_id={item.User.Id}";
                    var (_addArtwork, _updateArtwork, _download, _transfer) = await PrivateDownloadAllArtworkResponsesAndFiles(illustsUrl, logger, database, requestSender, filter, token).ConfigureAwait(false);
                    addArtwork += _addArtwork;
                    updateArtwork += _updateArtwork;
                    download += _download;
                    transfer += _transfer;
                }
            }
        }
        catch (Exception e)
        {
            logger.LogError(e, "Error happend.");
        }

    RETURN:
        return (add, update, addArtwork, updateArtwork, download, transfer);
    }
}
