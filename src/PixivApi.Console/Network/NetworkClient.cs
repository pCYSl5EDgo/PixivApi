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
        ulong addCount = 0UL, updateCount = 0UL, downloadCount = 0UL;
        try
        {
            if (download)
            {
                if (addBehaviour)
                {
                    (addCount, updateCount, downloadCount) = await PrivateDownloadAllArtworkResponsesAndFiles(url, logger, database, requestSender, token).ConfigureAwait(false);
                }
                else
                {
                    (addCount, updateCount, downloadCount) = await PrivateDownloadNewArtworkResponsesAndFiles(url, logger, database, requestSender, token).ConfigureAwait(false);
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
        finally
        {
            if (!System.Console.IsOutputRedirected)
            {
                var artworkCount = await database.CountArtworkAsync(token).ConfigureAwait(false);
                logger.LogInformation($"Total: {artworkCount} Add: {addCount} Update: {updateCount} Download: {downloadCount}");
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
        await foreach (var collection in new DownloadArtworkAsyncEnumerable(url, requestSender.GetAsync))
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
                            logger.LogInformation($"A {add,10}: {item.Id,16}");
                        }
                    }
                    else
                    {
                        ++update;
                        if (logTrace)
                        {
                            logger.LogTrace($"U {update,10}: {item.Id,16}");
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

        return (add, update);
    }

    private static async ValueTask<(ulong add, ulong update)> PrivateDownloadAllArtworkResponses(string url, ILogger<ConsoleApp> logger, IDatabase database, RequestSender requestSender, CancellationToken token)
    {
        ulong add = 0UL, update = 0UL;
        bool logInfo = logger.IsEnabled(LogLevel.Information), logTrace = logger.IsEnabled(LogLevel.Trace);
#pragma warning disable IDE0019
        var extended = database as IExtenededDatabase;
#pragma warning restore IDE0019
        await foreach (var collection in new DownloadArtworkAsyncEnumerable(url, requestSender.GetAsync))
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
                            logger.LogInformation($"A {add,10}: {item.Id,16}");
                        }
                    }
                    else
                    {
                        ++update;
                        if (logTrace)
                        {
                            logger.LogTrace($"U {update,10}: {item.Id,16}");
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

        return (add, update);
    }

    private async ValueTask<(ulong add, ulong update, ulong download)> PrivateDownloadNewArtworkResponsesAndFiles(string url, ILogger<ConsoleApp> logger, IDatabase database, RequestSender requestSender, CancellationToken token)
    {
        if (string.IsNullOrWhiteSpace(configSettings.ArtworkFilterFilePath))
        {
            return (0, 0, 0);
        }

        ulong add = 0UL, update = 0UL;
        var filter = await filterFactory.CreateAsync(database, new FileInfo(configSettings.ArtworkFilterFilePath), token).ConfigureAwait(false);
        if (filter is not { FileExistanceFilter: { } fileFilter } || fileFilter is { Original: null, Thumbnail: null, Ugoira: null })
        {
            (add, update) = await PrivateDownloadNewArtworkResponses(url, logger, database, requestSender, token).ConfigureAwait(false);
            return (add, update, 0);
        }

        bool logInfo = logger.IsEnabled(LogLevel.Information), logTrace = logger.IsEnabled(LogLevel.Trace);
        var queue = new ConcurrentQueue<Artwork>();
        object done = false;
        var dequeDownloadTask = Task.Run(async () =>
        {
            var machine = new DownloadAsyncMachine(this, database, token);
            var timeSpan = TimeSpan.FromSeconds(30d);

            var shouldDownloadOriginal = fileFilter.Original is not null;
            var shouldDownloadThumbnail = fileFilter.Thumbnail is not null;
            var shouldDownloadUgoira = fileFilter.Ugoira.HasValue;
            var converter = Context.ServiceProvider.GetRequiredService<ConverterFacade>();
            var finder = Context.ServiceProvider.GetRequiredService<FinderFacade>();

            while (!Unsafe.Unbox<bool>(done))
            {
                if (token.IsCancellationRequested)
                {
                    break;
                }

                if (!queue.TryDequeue(out var artwork))
                {
                    await Task.Delay(timeSpan, token).ConfigureAwait(false);
                    continue;
                }

                if (!filter.FastFilter(artwork) || !await filter.SlowFilter(artwork, token).ConfigureAwait(false))
                {
                    continue;
                }

                if (token.IsCancellationRequested)
                {
                    break;
                }

                var downloadResult = artwork.Type == ArtworkType.Ugoira ?
                    await ProcessDownloadUgoiraAsync(machine, artwork, shouldDownloadOriginal, shouldDownloadThumbnail, shouldDownloadUgoira, finder, converter, token).ConfigureAwait(false) :
                    await ProcessDownloadNotUgoiraAsync(machine, artwork, shouldDownloadOriginal, shouldDownloadThumbnail, finder, converter, token).ConfigureAwait(false);
            }

            return (ulong)machine.DownloadFileCount;
        });
        await foreach (var collection in new DownloadArtworkAsyncEnumerable(url, requestSender.GetAsync))
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
                    queue.Enqueue(artwork);
                    return artwork;
                }, (v, token) => LocalNetworkConverter.OverwriteAsync(v, item, database, database, database, token), token).ConfigureAwait(false))
                {
                    ++add;
                    if (logInfo)
                    {
                        logger.LogInformation($"A {add,10}: {item.Id,16}");
                    }
                }
                else
                {
                    ++update;
                    if (logTrace)
                    {
                        logger.LogTrace($"U {update,10}: {item.Id,16}");
                    }

                    continue;
                }
            }

            if (add == oldAdd || token.IsCancellationRequested)
            {
                break;
            }
        }

        Unsafe.Unbox<bool>(done) = true;
        return (add, update, await dequeDownloadTask.ConfigureAwait(false));
    }

    private async ValueTask<(ulong add, ulong update, ulong download)> PrivateDownloadAllArtworkResponsesAndFiles(string url, ILogger<ConsoleApp> logger, IDatabase database, RequestSender requestSender, CancellationToken token)
    {
        if (string.IsNullOrWhiteSpace(configSettings.ArtworkFilterFilePath))
        {
            return (0, 0, 0);
        }

        ulong add = 0UL, update = 0UL;
        var filter = await filterFactory.CreateAsync(database, new FileInfo(configSettings.ArtworkFilterFilePath), token).ConfigureAwait(false);
        if (filter is not { FileExistanceFilter: { } fileFilter } || fileFilter is { Original: null, Thumbnail: null, Ugoira: null })
        {
            (add, update) = await PrivateDownloadNewArtworkResponses(url, logger, database, requestSender, token).ConfigureAwait(false);
            return (add, update, 0);
        }

        bool logInfo = logger.IsEnabled(LogLevel.Information), logTrace = logger.IsEnabled(LogLevel.Trace);
        var queue = new ConcurrentQueue<Artwork>();
        object done = false;
        var dequeDownloadTask = Task.Run(async () =>
        {
            var machine = new DownloadAsyncMachine(this, database, token);
            var timeSpan = TimeSpan.FromSeconds(30d);

            var shouldDownloadOriginal = fileFilter.Original is not null;
            var shouldDownloadThumbnail = fileFilter.Thumbnail is not null;
            var shouldDownloadUgoira = fileFilter.Ugoira.HasValue;
            var converter = Context.ServiceProvider.GetRequiredService<ConverterFacade>();
            var finder = Context.ServiceProvider.GetRequiredService<FinderFacade>();

            while (!Unsafe.Unbox<bool>(done))
            {
                if (token.IsCancellationRequested)
                {
                    break;
                }

                if (!queue.TryDequeue(out var artwork))
                {
                    await Task.Delay(timeSpan, token).ConfigureAwait(false);
                    continue;
                }

                if (!filter.FastFilter(artwork) || !await filter.SlowFilter(artwork, token).ConfigureAwait(false))
                {
                    continue;
                }

                if (token.IsCancellationRequested)
                {
                    break;
                }

                var downloadResult = artwork.Type == ArtworkType.Ugoira ?
                    await ProcessDownloadUgoiraAsync(machine, artwork, shouldDownloadOriginal, shouldDownloadThumbnail, shouldDownloadUgoira, finder, converter, token).ConfigureAwait(false) :
                    await ProcessDownloadNotUgoiraAsync(machine, artwork, shouldDownloadOriginal, shouldDownloadThumbnail, finder, converter, token).ConfigureAwait(false);
            }

            return (ulong)machine.DownloadFileCount;
        });
        await foreach (var collection in new DownloadArtworkAsyncEnumerable(url, requestSender.GetAsync))
        {
            if (token.IsCancellationRequested)
            {
                break;
            }

            foreach (var item in collection)
            {
                if (await database.AddOrUpdateAsync(item.Id, async token =>
                {
                    var artwork = await LocalNetworkConverter.ConvertAsync(item, database, database, database, token).ConfigureAwait(false);
                    queue.Enqueue(artwork);
                    return artwork;
                }, (v, token) => LocalNetworkConverter.OverwriteAsync(v, item, database, database, database, token), token).ConfigureAwait(false))
                {
                    ++add;
                    if (logInfo)
                    {
                        logger.LogInformation($"A {add,10}: {item.Id,16}");
                    }
                }
                else
                {
                    ++update;
                    if (logTrace)
                    {
                        logger.LogTrace($"U {update,10}: {item.Id,16}");
                    }

                    continue;
                }
            }
        }

        Unsafe.Unbox<bool>(done) = true;
        return (add, update, await dequeDownloadTask.ConfigureAwait(false));
    }
}
