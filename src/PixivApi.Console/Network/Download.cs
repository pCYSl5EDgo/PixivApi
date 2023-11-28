namespace PixivApi.Console;

public partial class NetworkClient
{
  [Command("download")]
  public async ValueTask DownloadFileFromDatabaseAsync(
      [Option("g")] ulong gigaByteCount = 2UL,
      [Option("mask")] int maskPowerOf2 = 10,
      bool encode = true
  )
  {
    if (string.IsNullOrWhiteSpace(configSettings.ArtworkFilterFilePath) || string.IsNullOrWhiteSpace(configSettings.DatabaseFilePath))
    {
      return;
    }

    var token = Context.CancellationToken;
    var finder = Context.ServiceProvider.GetRequiredService<FinderFacade>();
    var database = await databaseFactory.RentAsync(token).ConfigureAwait(false);
    var transactional = database as ITransactionalDatabase;
    if (transactional is not null)
    {
      await transactional.BeginTransactionAsync(token).ConfigureAwait(false);
    }

    try
    {
      var artworkFilter = await filterFactory.CreateAsync(database, new(configSettings.ArtworkFilterFilePath), token).ConfigureAwait(false);
      if (artworkFilter is not { FileExistanceFilter: { } fileFilter })
      {
        return;
      }

      artworkFilter.IsOfficiallyRemoved = false;
      var artworks = PrepareDownloadFileAsync(database, artworkFilter, gigaByteCount);
      if (artworks is null)
      {
        return;
      }

      var shouldDownloadOriginal = fileFilter.Original is not null;
      var shouldDownloadUgoira = fileFilter.Ugoira.HasValue;
      if (!shouldDownloadOriginal && !shouldDownloadUgoira)
      {
        return;
      }

      var converter = encode ? Context.ServiceProvider.GetRequiredService<ConverterFacade>() : null;
      var downloadItemCount = 0;
      var alreadyCount = 0;
      var machine = new DownloadAsyncMachine(this, database, token);
      var logger = Context.Logger;
      logger.LogInformation("Start downloading.");
      try
      {
        await foreach (var artwork in artworks)
        {
          if (token.IsCancellationRequested)
          {
            return;
          }

          if ((machine.DownloadByteCount >> 30) >= gigaByteCount)
          {
            return;
          }

          var downloadResult = artwork.Type == ArtworkType.Ugoira ?
              await ProcessDownloadUgoiraAsync(machine, artwork, shouldDownloadOriginal, shouldDownloadUgoira, finder, converter, token).ConfigureAwait(false) :
              await ProcessDownloadNotUgoiraAsync(machine, artwork, shouldDownloadOriginal, finder, converter, token).ConfigureAwait(false);
          if (downloadResult != DownloadResult.None)
          {
            await database.AddOrUpdateAsync(artwork, token).ConfigureAwait(false);
          }

          if ((downloadResult & DownloadResult.Success) != 0)
          {
            Interlocked.Increment(ref downloadItemCount);
          }
          else
          {
            Interlocked.Increment(ref alreadyCount);
          }
        }
      }
      finally
      {
        if (!System.Console.IsOutputRedirected)
        {
          logger.LogInformation($"Item: {downloadItemCount}, File: {machine.DownloadFileCount}, Already: {alreadyCount}, Transfer: {ByteAmountUtility.ToDisplayable(machine.DownloadByteCount)}");
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
      if (transactional is not null)
      {
        await transactional.EndTransactionAsync(CancellationToken.None).ConfigureAwait(false);
      }

      databaseFactory.Return(ref database);
    }
  }

  [Flags]
  private enum DownloadResult
  {
    None = 0,
    Success = 1,
    Update = 2,
  }

  private static async ValueTask<DownloadResult> ProcessDownloadNotUgoiraAsync(DownloadAsyncMachine machine, Artwork artwork, bool shouldDownloadOriginal, FinderFacade finder, ConverterFacade? converter, CancellationToken token)
  {
    IFinderWithIndex finderWithIndexOriginal, finderWithIndexOriginalDefault;
    switch (artwork.Type)
    {
      case ArtworkType.Illust:
        finderWithIndexOriginal = finder.IllustOriginalFinder;
        finderWithIndexOriginalDefault = finder.DefaultIllustOriginalFinder;
        break;
      case ArtworkType.Manga:
        finderWithIndexOriginal = finder.MangaOriginalFinder;
        finderWithIndexOriginalDefault = finder.DefaultMangaOriginalFinder;
        break;
      default:
        return DownloadResult.None;
    }

    var downloadAny = false;
    var noDetailDownload = true;
    foreach (var pageIndex in artwork)
    {
      if (token.IsCancellationRequested)
      {
        goto END;
      }

      if (shouldDownloadOriginal)
      {
        if (token.IsCancellationRequested)
        {
          goto END;
        }

        if (finderWithIndexOriginal.Exists(artwork, pageIndex))
        {
          continue;
        }

        var dest = finderWithIndexOriginalDefault.Find(artwork.Id, artwork.Extension, pageIndex);
        var (Success, NoDetailDownload) = await machine.DownloadAsync(dest, artwork, noDetailDownload, converter?.OriginalConverter, artwork.GetNotUgoiraOriginalUrl, pageIndex).ConfigureAwait(false);
        noDetailDownload = NoDetailDownload;
        downloadAny = true;
        if (!Success)
        {
          if (noDetailDownload)
          {
            artwork.IsOfficiallyRemoved = true;
          }
          else
          {
            artwork.ExtraHideReason = HideReason.TemporaryHidden;
          }
          goto END;
        }
      }
    }

  END:
    return CalculateDownloadResult(downloadAny, noDetailDownload);
  }

  private static async ValueTask<DownloadResult> ProcessDownloadUgoiraAsync(DownloadAsyncMachine machine, Artwork artwork, bool shouldDownloadOriginal, bool shouldDownloadUgoira, FinderFacade finder, ConverterFacade? converter, CancellationToken token)
  {
    var downloadAny = false;
    var noDetailDownload = true;
    if (shouldDownloadUgoira && !finder.UgoiraZipFinder.Exists(artwork))
    {
      if (token.IsCancellationRequested)
      {
        goto END;
      }

      var dest = finder.DefaultUgoiraZipFinder.Find(artwork.Id, artwork.Extension);
      var (Success, NoDetailDownload) = await machine.DownloadAsync(dest, artwork, noDetailDownload, converter?.UgoiraZipConverter, artwork.GetUgoiraZipUrl).ConfigureAwait(false);
      noDetailDownload = NoDetailDownload;
      downloadAny = true;
      if (!Success)
      {
        if (noDetailDownload)
        {
          artwork.IsOfficiallyRemoved = true;
        }
        else
        {
          artwork.ExtraHideReason = HideReason.TemporaryHidden;
        }
        goto END;
      }
    }

    if (shouldDownloadOriginal && !finder.UgoiraOriginalFinder.Exists(artwork))
    {
      if (token.IsCancellationRequested)
      {
        goto END;
      }

      var dest = finder.DefaultUgoiraOriginalFinder.Find(artwork.Id, artwork.Extension);
      var (Success, NoDetailDownload) = await machine.DownloadAsync(dest, artwork, noDetailDownload, converter?.OriginalConverter, artwork.GetUgoiraOriginalUrl).ConfigureAwait(false);
      noDetailDownload = NoDetailDownload;
      downloadAny = true;
      if (!Success)
      {
        if (noDetailDownload)
        {
          artwork.IsOfficiallyRemoved = true;
        }
        else
        {
          artwork.ExtraHideReason = HideReason.TemporaryHidden;
        }
        goto END;
      }
    }
  END:
    return CalculateDownloadResult(downloadAny, noDetailDownload);
  }

  private static DownloadResult CalculateDownloadResult(bool downloadAny, bool noDetailDownload) => (DownloadResult)((downloadAny ? 1 : 0) | (noDetailDownload ? 2 : 0));

  private IAsyncEnumerable<Artwork>? PrepareDownloadFileAsync(
      IDatabase database,
      ArtworkFilter? filter,
      ulong gigaByteCount
  )
  {
    if (filter is null || gigaByteCount == 0)
    {
      return null;
    }

    var token = Context.CancellationToken;
    filter.PageCount ??= new();
    filter.PageCount.Min ??= 1;
    return database.FilterAsync(filter, token);
  }

  private static readonly Uri referer = new("https://app-api.pixiv.net/");
}
