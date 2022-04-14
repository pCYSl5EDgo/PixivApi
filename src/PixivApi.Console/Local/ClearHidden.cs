namespace PixivApi.Console;

public partial class LocalClient
{
    [Command("clear-hidden", "")]
    public async ValueTask ClearHiddenAsync(
    )
    {
        var token = Context.CancellationToken;
        var database = await databaseFactory.RentAsync(token).ConfigureAwait(false);
        var finderFacade = Context.ServiceProvider.GetRequiredService<FinderFacade>();
        try
        {
            static void DeleteFileWithIndex(in HiddenPageValueTuple tuple, IFinderWithIndex finder, ILogger? logger)
            {
                var info = finder.Find(tuple.Id, tuple.Extension, tuple.Index);
                if (info.Exists)
                {
                    logger?.LogTrace($"Delete: {tuple}");
                    info.Delete();
                }
            }

            static void DeleteFile(in HiddenPageValueTuple tuple, IFinder finder, ILogger? logger)
            {
                var info = finder.Find(tuple.Id, tuple.Extension);
                if (info.Exists)
                {
                    logger?.LogTrace($"Delete: {tuple}");
                    info.Delete();
                }
            }

            void Delete(in HiddenPageValueTuple tuple)
            {
                var tmpLogger = logger.IsEnabled(LogLevel.Trace) ? logger : null;
                switch (tuple.Type)
                {
                    case ArtworkType.Illust:
                        DeleteFileWithIndex(tuple, finderFacade.IllustThumbnailFinder, tmpLogger);
                        DeleteFileWithIndex(tuple, finderFacade.IllustOriginalFinder, tmpLogger);
                        break;
                    case ArtworkType.Manga:
                        DeleteFileWithIndex(tuple, finderFacade.MangaThumbnailFinder, tmpLogger);
                        DeleteFileWithIndex(tuple, finderFacade.MangaOriginalFinder, tmpLogger);
                        break;
                    case ArtworkType.Ugoira:
                        DeleteFile(tuple, finderFacade.UgoiraThumbnailFinder, tmpLogger);
                        DeleteFile(tuple, finderFacade.UgoiraOriginalFinder, tmpLogger);
                        DeleteFile(tuple, finderFacade.UgoiraZipFinder, tmpLogger);
                        break;
                    case ArtworkType.None:
                    default:
                        throw new InvalidDataException(tuple.ToString());
                }
            }

            if (database is IExtenededDatabase exteneded)
            {
                await foreach (var tuple in exteneded.EnumerateHiddenPagesAsync(token))
                {
                    Delete(tuple);
                }
            }
            else
            {
                Dictionary<ulong, HideReason> hideUser = new();
                HashSet<ulong> notHideUser = new();
                await foreach (var artwork in database.EnumerateArtworkAsync(token))
                {
                    switch (artwork.ExtraHideReason)
                    {
                        case HideReason.NotHidden:
                        case HideReason.TemporaryHidden:
                            break;
                        default:
                            for (var i = 0U; i < artwork.PageCount; i++)
                            {
                                Delete(new(artwork.Id, i, artwork.Type, artwork.Extension, artwork.ExtraHideReason));
                            }
                            continue;
                    }

                    if (!hideUser.TryGetValue(artwork.UserId, out var reason))
                    {
                        if (notHideUser.Contains(artwork.UserId))
                        {
                            var user = await database.GetUserAsync(artwork.UserId, token).ConfigureAwait(false) ?? throw new NullReferenceException();
                            reason = user.ExtraHideReason;
                            switch (reason)
                            {
                                case HideReason.NotHidden:
                                case HideReason.TemporaryHidden:
                                    notHideUser.Add(user.Id);
                                    break;
                                case HideReason.LowQuality:
                                case HideReason.Irrelevant:
                                case HideReason.ExternalLink:
                                case HideReason.Dislike:
                                case HideReason.Crop:
                                default:
                                    hideUser.Add(user.Id, user.ExtraHideReason);
                                    break;
                            }
                        }
                        else
                        {
                            reason = HideReason.NotHidden;
                        }
                    }

                    switch (reason)
                    {
                        case HideReason.NotHidden:
                        case HideReason.TemporaryHidden:
                            break;
                        default:
                            for (var i = 0U; i < artwork.PageCount; i++)
                            {
                                Delete(new(artwork.Id, i, artwork.Type, artwork.Extension, reason));
                            }
                            continue;
                    }

                    if (artwork.ExtraPageHideReasonDictionary is { Count: > 0 } dictionary)
                    {
                        foreach (var pair in dictionary)
                        {
                            switch (pair.Value)
                            {
                                case HideReason.NotHidden:
                                case HideReason.TemporaryHidden:
                                    continue;
                                default:
                                    Delete(new(artwork.Id, pair.Key, artwork.Type, artwork.Extension, pair.Value));
                                    break;
                            }
                        }
                    }
                }
            }
        }
        finally
        {
            databaseFactory.Return(ref database);
        }
    }
}
