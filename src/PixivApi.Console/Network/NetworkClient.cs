using System.Threading.Tasks;

namespace PixivApi.Console;

[Command("net")]
public sealed partial class NetworkClient : ConsoleAppBase, IDisposable
{
    private const string ApiHost = "app-api.pixiv.net";
    private readonly ConfigSettings configSettings;
    private readonly HttpClient client;
    private readonly AuthenticationHeaderValueHolder holder;
    private readonly IDatabaseFactory databaseFactory;
    private readonly IArtworkFilterFactory<FileInfo> filterFactory;

    public NetworkClient(ConfigSettings config, HttpClient client, AuthenticationHeaderValueHolder holder, IDatabaseFactory databaseFactory, IArtworkFilterFactory<FileInfo> filterFactory)
    {
        configSettings = config;
        this.client = client;
        this.holder = holder;
        this.databaseFactory = databaseFactory;
        this.filterFactory = filterFactory;
    }

    public void Dispose() => holder.Dispose();

    private async ValueTask DownloadArtworkResponses(bool addBehaviour, bool download, string url, CancellationToken token)
    {
        var logger = Context.Logger;
        var database = await databaseFactory.RentAsync(token).ConfigureAwait(false);
        var requestSender = Context.ServiceProvider.GetRequiredService<RequestSender>();
        ulong addCount = 0UL, updateCount = 0UL, downloadCount = 0UL, transferByteCount = 0UL;
        var transactional = database as ITransactionalDatabase;
        if (transactional is not null)
        {
            await transactional.BeginExclusiveTransactionAsync(token).ConfigureAwait(false);
        }

        try
        {
            if (download)
            {
                if (addBehaviour)
                {
                    (addCount, updateCount, downloadCount, transferByteCount) = await PrivateDownloadAllArtworkResponsesAndFiles(url, logger, database, requestSender, token).ConfigureAwait(false);
                }
                else
                {
                    (addCount, updateCount, downloadCount, transferByteCount) = await PrivateDownloadNewArtworkResponsesAndFiles(url, logger, database, requestSender, token).ConfigureAwait(false);
                }
            }
            else
            {
                if (addBehaviour)
                {
                    (addCount, updateCount) = await PrivateDownloadAllArtworkResponses(url, logger, database, requestSender, token).ConfigureAwait(false);
                }
                else
                {
                    (addCount, updateCount) = await PrivateDownloadNewArtworkResponses(url, logger, database, requestSender, token).ConfigureAwait(false);
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
                logger.LogInformation($"Total: {artworkCount} Add: {addCount} Update: {updateCount} Download: {downloadCount} Transfer: {ByteAmountUtility.ToDisplayable(transferByteCount)}");
            }

            if (transactional is not null)
            {
                await transactional.EndTransactionAsync(CancellationToken.None).ConfigureAwait(false);
            }

            databaseFactory.Return(ref database);
        }
    }

    private static async ValueTask<(ulong add, ulong update)> PrivateDownloadNewArtworkResponses(string url, ILogger<ConsoleApp> logger, IDatabase database, RequestSender requestSender, CancellationToken token)
    {
        ulong add = 0UL, update = 0UL;
        bool logInfo = logger.IsEnabled(LogLevel.Information), logTrace = logger.IsEnabled(LogLevel.Trace), logDebug = logger.IsEnabled(LogLevel.Debug);
        if (logDebug)
        {
            logger.LogDebug("No-Download No-All-Add");
        }

        try
        {
            await foreach (var collection in new DownloadArtworkAsyncEnumerable(url, requestSender.GetAsync, logger))
            {
                if (database is IExtenededDatabase extended)
                {
                    var (_add, _update) = await extended.ArtworkAddOrUpdateAsync(collection, token).ConfigureAwait(false);
                    if (logInfo)
                    {
                        logger.LogInformation($"Add: {_add} Update: {_update}");
                    }

                    update += _update;
                    if (_add == 0)
                    {
                        break;
                    }

                    add += _add;
                }
                else
                {
                    var oldAdd = add;
                    foreach (var item in collection)
                    {
                        await database.AddOrUpdateAsync(item.User.Id,
                            token => ValueTask.FromResult(item.User.Convert()),
                            (user, token) =>
                            {
                                user.Overwrite(item.User);
                                return ValueTask.CompletedTask;
                            },
                            token).ConfigureAwait(false);
                        if (await database.AddOrUpdateAsync(item.Id,
                            token => LocalNetworkConverter.ConvertAsync(item, database, database, database, token),
                            (v, token) => LocalNetworkConverter.OverwriteAsync(v, item, database, database, database, token),
                            token).ConfigureAwait(false))
                        {
                            ++add;
                            if (logInfo)
                            {
                                logger.LogInformation($"Art-A {add,10}: {item.Id,16}");
                            }
                        }
                        else
                        {
                            ++update;
                            if (logTrace)
                            {
                                logger.LogTrace($"Art-U {update,10}: {item.Id,16}");
                            }
                        }
                    }

                    if (add == oldAdd)
                    {
                        break;
                    }
                }
            }
        }
        catch (Exception e)
        {
            logger.LogError(e, "Error happened");
        }

        return (add, update);
    }

    private static async ValueTask<(ulong add, ulong update)> PrivateDownloadAllArtworkResponses(string url, ILogger<ConsoleApp> logger, IDatabase database, RequestSender requestSender, CancellationToken token)
    {
        ulong add = 0UL, update = 0UL;
        bool logInfo = logger.IsEnabled(LogLevel.Information), logTrace = logger.IsEnabled(LogLevel.Trace), logDebug = logger.IsEnabled(LogLevel.Debug);
        if (logDebug)
        {
            logger.LogDebug("No-Download All-Add");
        }

        try
        {
            await foreach (var collection in new DownloadArtworkAsyncEnumerable(url, requestSender.GetAsync, logger))
            {
                if (database is IExtenededDatabase extended)
                {
                    var (_add, _update) = await extended.ArtworkAddOrUpdateAsync(collection, token).ConfigureAwait(false);
                    if (logInfo)
                    {
                        logger.LogInformation($"Add: {_add} Update: {_update}");
                    }

                    add += _add;
                    update += _update;
                }
                else
                {
                    foreach (var item in collection)
                    {
                        await database.AddOrUpdateAsync(item.User.Id,
                            token => ValueTask.FromResult(item.User.Convert()),
                            (user, token) =>
                            {
                                user.Overwrite(item.User);
                                return ValueTask.CompletedTask;
                            },
                            token).ConfigureAwait(false);
                        if (await database.AddOrUpdateAsync(item.Id,
                            token => LocalNetworkConverter.ConvertAsync(item, database, database, database, token),
                            (v, token) => LocalNetworkConverter.OverwriteAsync(v, item, database, database, database, token),
                            token).ConfigureAwait(false))
                        {
                            ++add;
                            if (logInfo)
                            {
                                logger.LogInformation($"Art-A {add,10}: {item.Id,16}");
                            }
                        }
                        else
                        {
                            ++update;
                            if (logTrace)
                            {
                                logger.LogTrace($"Art-U {update,10}: {item.Id,16}");
                            }
                        }
                    }
                }
            }
        }
        catch (Exception e)
        {
            logger.LogError(e, "Error happened");
        }

        return (add, update);
    }

    private async ValueTask<(ulong add, ulong update, ulong download, ulong transfer)> PrivateDownloadNewArtworkResponsesAndFiles(string url, ILogger<ConsoleApp> logger, IDatabase database, RequestSender requestSender, CancellationToken token)
    {
        if (string.IsNullOrWhiteSpace(configSettings.ArtworkFilterFilePath))
        {
            return (0, 0, 0, 0);
        }

        ulong add = 0UL, update = 0UL;
        var filter = await filterFactory.CreateAsync(database, new FileInfo(configSettings.ArtworkFilterFilePath), token).ConfigureAwait(false);
        if (filter is not { FileExistanceFilter: { } fileFilter } || fileFilter is { Original: null, Thumbnail: null, Ugoira: null })
        {
            (add, update) = await PrivateDownloadNewArtworkResponses(url, logger, database, requestSender, token).ConfigureAwait(false);
            return (add, update, 0, 0);
        }

        bool logInfo = logger.IsEnabled(LogLevel.Information), logTrace = logger.IsEnabled(LogLevel.Trace), logDebug = logger.IsEnabled(LogLevel.Debug);
        if (logDebug)
        {
            logger.LogDebug("Download No-All-Add");
        }

        var machine = new DownloadAsyncMachine(this, database, token);
        var shouldDownloadOriginal = fileFilter.Original is not null;
        var shouldDownloadThumbnail = fileFilter.Thumbnail is not null;
        var shouldDownloadUgoira = fileFilter.Ugoira.HasValue;
        var converter = Context.ServiceProvider.GetRequiredService<ConverterFacade>();
        var finder = Context.ServiceProvider.GetRequiredService<FinderFacade>();

        try
        {
            await foreach (var collection in new DownloadArtworkAsyncEnumerable(url, requestSender.GetAsync, logger))
            {
                if (token.IsCancellationRequested)
                {
                    break;
                }

                var oldAdd = add;
                foreach (var item in collection)
                {
                    if (token.IsCancellationRequested)
                    {
                        break;
                    }

                    bool isAdded;
                    if (database is IExtenededDatabase exteneded)
                    {
                        isAdded = await exteneded.ArtworkAddOrUpdateAsync(item, token).ConfigureAwait(false);
                    }
                    else
                    {
                        await database.AddOrUpdateAsync(item.User.Id,
                            token => ValueTask.FromResult(item.User.Convert()),
                            (user, token) =>
                            {
                                user.Overwrite(item.User);
                                return ValueTask.CompletedTask;
                            },
                            token).ConfigureAwait(false);
                        isAdded = await database.AddOrUpdateAsync(item.Id,
                            token => LocalNetworkConverter.ConvertAsync(item, database, database, database, token),
                            (artwork, token) => LocalNetworkConverter.OverwriteAsync(artwork, item, database, database, database, token),
                            token).ConfigureAwait(false);
                    }

                    var artwork = await database.GetArtworkAsync(item.Id, token).ConfigureAwait(false);
                    if (artwork is not null && filter.FastFilter(artwork) && await filter.SlowFilter(artwork, token).ConfigureAwait(false))
                    {
                        if (artwork.Type == ArtworkType.Ugoira)
                        {
                            await ProcessDownloadUgoiraAsync(machine, artwork, shouldDownloadOriginal, shouldDownloadThumbnail, shouldDownloadUgoira, finder, converter, token).ConfigureAwait(false);
                        }
                        else
                        {
                            await ProcessDownloadNotUgoiraAsync(machine, artwork, shouldDownloadOriginal, shouldDownloadThumbnail, finder, converter, token).ConfigureAwait(false);
                        }
                    }

                    if (isAdded)
                    {
                        ++add;
                        if (logInfo)
                        {
                            logger.LogInformation($"Art-A {add,10}: {item.Id,16}");
                        }
                    }
                    else
                    {
                        ++update;
                        if (logTrace)
                        {
                            logger.LogTrace($"Art-U {update,10}: {item.Id,16}");
                        }
                    }
                }

                if (add == oldAdd || token.IsCancellationRequested)
                {
                    break;
                }
            }
        }
        catch (Exception e)
        {
            logger.LogError(e, "Error happened");
        }

        return (add, update, (ulong)machine.DownloadFileCount, machine.DownloadByteCount);
    }

    private async ValueTask<(ulong add, ulong update, ulong download, ulong transfer)> PrivateDownloadAllArtworkResponsesAndFiles(string url, ILogger<ConsoleApp> logger, IDatabase database, RequestSender requestSender, CancellationToken token)
    {
        if (string.IsNullOrWhiteSpace(configSettings.ArtworkFilterFilePath))
        {
            return (0, 0, 0, 0);
        }

        ulong add = 0UL, update = 0UL;
        var filter = await filterFactory.CreateAsync(database, new FileInfo(configSettings.ArtworkFilterFilePath), token).ConfigureAwait(false);
        if (filter is not { FileExistanceFilter: { } fileFilter } || fileFilter is { Original: null, Thumbnail: null, Ugoira: null })
        {
            (add, update) = await PrivateDownloadNewArtworkResponses(url, logger, database, requestSender, token).ConfigureAwait(false);
            return (add, update, 0, 0);
        }

        bool logInfo = logger.IsEnabled(LogLevel.Information), logTrace = logger.IsEnabled(LogLevel.Trace), logDebug = logger.IsEnabled(LogLevel.Debug);
        if (logDebug)
        {
            logger.LogDebug("Download All-Add");
        }

        var machine = new DownloadAsyncMachine(this, database, token);
        var shouldDownloadOriginal = fileFilter.Original is not null;
        var shouldDownloadThumbnail = fileFilter.Thumbnail is not null;
        var shouldDownloadUgoira = fileFilter.Ugoira.HasValue;
        var converter = Context.ServiceProvider.GetRequiredService<ConverterFacade>();
        var finder = Context.ServiceProvider.GetRequiredService<FinderFacade>();

        try
        {
            await foreach (var collection in new DownloadArtworkAsyncEnumerable(url, requestSender.GetAsync, logger))
            {
                if (token.IsCancellationRequested)
                {
                    break;
                }

                var oldAdd = add;
                foreach (var item in collection)
                {
                    if (token.IsCancellationRequested)
                    {
                        break;
                    }

                    bool isAdded;
                    if (database is IExtenededDatabase exteneded)
                    {
                        isAdded = await exteneded.ArtworkAddOrUpdateAsync(item, token).ConfigureAwait(false);
                    }
                    else
                    {
                        await database.AddOrUpdateAsync(item.User.Id,
                            token => ValueTask.FromResult(item.User.Convert()),
                            (user, token) =>
                            {
                                user.Overwrite(item.User);
                                return ValueTask.CompletedTask;
                            },
                            token).ConfigureAwait(false);
                        isAdded = await database.AddOrUpdateAsync(item.Id,
                            token => LocalNetworkConverter.ConvertAsync(item, database, database, database, token),
                            (artwork, token) => LocalNetworkConverter.OverwriteAsync(artwork, item, database, database, database, token),
                            token).ConfigureAwait(false);
                    }

                    var artwork = await database.GetArtworkAsync(item.Id, token).ConfigureAwait(false);
                    if (artwork is not null && filter.FastFilter(artwork) && await filter.SlowFilter(artwork, token).ConfigureAwait(false))
                    {
                        if (artwork.Type == ArtworkType.Ugoira)
                        {
                            await ProcessDownloadUgoiraAsync(machine, artwork, shouldDownloadOriginal, shouldDownloadThumbnail, shouldDownloadUgoira, finder, converter, token).ConfigureAwait(false);
                        }
                        else
                        {
                            await ProcessDownloadNotUgoiraAsync(machine, artwork, shouldDownloadOriginal, shouldDownloadThumbnail, finder, converter, token).ConfigureAwait(false);
                        }
                    }

                    if (isAdded)
                    {
                        ++add;
                        if (logInfo)
                        {
                            logger.LogInformation($"Art-A {add,10}: {item.Id,16}");
                        }
                    }
                    else
                    {
                        ++update;
                        if (logTrace)
                        {
                            logger.LogTrace($"Art-U {update,10}: {item.Id,16}");
                        }
                    }

                    if (add == 1)
                    {
                        throw new TaskCanceledException();
                    }
                }
            }
        }
        catch (Exception e)
        {
            logger.LogError(e, "Error happened");
        }

        return (add, update, (ulong)machine.DownloadFileCount, machine.DownloadByteCount);
    }
}
