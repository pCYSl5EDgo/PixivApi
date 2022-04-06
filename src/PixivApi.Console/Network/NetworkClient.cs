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
            await transactional.BeginTransactionAsync(token).ConfigureAwait(false);
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
            await transactional.RollbackTransactionAsync(CancellationToken.None).ConfigureAwait(false);
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
        bool logInfo = logger.IsEnabled(LogLevel.Information), logTrace = logger.IsEnabled(LogLevel.Trace);
#pragma warning disable IDE0019
        var extended = database as IExtenededDatabase;
#pragma warning restore IDE0019
        try
        {
            await foreach (var collection in new DownloadArtworkAsyncEnumerable(url, requestSender.GetAsync, logger))
            {
                if (extended is null)
                {
                    var oldAdd = add;
                    foreach (var item in collection)
                    {
                        if (await database.AddOrUpdateAsync(item.Id, token => LocalNetworkConverter.ConvertAsync(item, database, database, database, token), (v, token) => LocalNetworkConverter.OverwriteAsync(v, item, database, database, database, token), token).ConfigureAwait(false))
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
                else
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
        bool logInfo = logger.IsEnabled(LogLevel.Information), logTrace = logger.IsEnabled(LogLevel.Trace);
#pragma warning disable IDE0019
        var extended = database as IExtenededDatabase;
#pragma warning restore IDE0019
        try
        {
            await foreach (var collection in new DownloadArtworkAsyncEnumerable(url, requestSender.GetAsync, logger))
            {
                if (extended is null)
                {
                    foreach (var item in collection)
                    {
                        if (await database.AddOrUpdateAsync(item.Id, token => LocalNetworkConverter.ConvertAsync(item, database, database, database, token), (v, token) => LocalNetworkConverter.OverwriteAsync(v, item, database, database, database, token), token).ConfigureAwait(false))
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
                else
                {
                    var (_add, _update) = await extended.ArtworkAddOrUpdateAsync(collection, token).ConfigureAwait(false);
                    if (logInfo)
                    {
                        logger.LogInformation($"Add: {_add} Update: {_update}");
                    }

                    add += _add;
                    update += _update;
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

        bool logInfo = logger.IsEnabled(LogLevel.Information), logTrace = logger.IsEnabled(LogLevel.Trace);
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

                    if (await database.AddOrUpdateAsync(item.Id, async token =>
                    {
                        var artwork = await LocalNetworkConverter.ConvertAsync(item, database, database, database, token).ConfigureAwait(false);
                        if (filter.FastFilter(artwork) && await filter.SlowFilter(artwork, token).ConfigureAwait(false))
                        {
                            _ = artwork.Type == ArtworkType.Ugoira ?
                                await ProcessDownloadUgoiraAsync(machine, artwork, shouldDownloadOriginal, shouldDownloadThumbnail, shouldDownloadUgoira, finder, converter, token).ConfigureAwait(false) :
                                await ProcessDownloadNotUgoiraAsync(machine, artwork, shouldDownloadOriginal, shouldDownloadThumbnail, finder, converter, token).ConfigureAwait(false);
                        }

                        return artwork;
                    }, async (artwork, token) =>
                    {
                        await LocalNetworkConverter.OverwriteAsync(artwork, item, database, database, database, token).ConfigureAwait(false);
                        if (filter.FastFilter(artwork) && await filter.SlowFilter(artwork, token).ConfigureAwait(false))
                        {
                            _ = artwork.Type == ArtworkType.Ugoira ?
                                await ProcessDownloadUgoiraAsync(machine, artwork, shouldDownloadOriginal, shouldDownloadThumbnail, shouldDownloadUgoira, finder, converter, token).ConfigureAwait(false) :
                                await ProcessDownloadNotUgoiraAsync(machine, artwork, shouldDownloadOriginal, shouldDownloadThumbnail, finder, converter, token).ConfigureAwait(false);
                        }

                    }, token).ConfigureAwait(false))
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

                        continue;
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

        bool logInfo = logger.IsEnabled(LogLevel.Information), logTrace = logger.IsEnabled(LogLevel.Trace);
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

                    if (await database.AddOrUpdateAsync(item.Id, async token =>
                    {
                        var artwork = await LocalNetworkConverter.ConvertAsync(item, database, database, database, token).ConfigureAwait(false);
                        if (filter.FastFilter(artwork) && await filter.SlowFilter(artwork, token).ConfigureAwait(false))
                        {
                            _ = artwork.Type == ArtworkType.Ugoira ?
                                await ProcessDownloadUgoiraAsync(machine, artwork, shouldDownloadOriginal, shouldDownloadThumbnail, shouldDownloadUgoira, finder, converter, token).ConfigureAwait(false) :
                                await ProcessDownloadNotUgoiraAsync(machine, artwork, shouldDownloadOriginal, shouldDownloadThumbnail, finder, converter, token).ConfigureAwait(false);
                        }

                        return artwork;
                    }, async (artwork, token) =>
                    {
                        await LocalNetworkConverter.OverwriteAsync(artwork, item, database, database, database, token).ConfigureAwait(false);
                        if (filter.FastFilter(artwork) && await filter.SlowFilter(artwork, token).ConfigureAwait(false))
                        {
                            _ = artwork.Type == ArtworkType.Ugoira ?
                                await ProcessDownloadUgoiraAsync(machine, artwork, shouldDownloadOriginal, shouldDownloadThumbnail, shouldDownloadUgoira, finder, converter, token).ConfigureAwait(false) :
                                await ProcessDownloadNotUgoiraAsync(machine, artwork, shouldDownloadOriginal, shouldDownloadThumbnail, finder, converter, token).ConfigureAwait(false);
                        }

                    }, token).ConfigureAwait(false))
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

                        continue;
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
