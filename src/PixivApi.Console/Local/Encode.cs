namespace PixivApi.Console;

public partial class LocalClient
{
  [Command("encode", "")]
  public async ValueTask EncodeAsync(
      [Option("o")] bool original = false,
      [Option("u")] bool ugoira = false,
      [Option("d")] bool delete = false
  )
  {
    if (!original && !ugoira)
    {
      return;
    }

    var token = Context.CancellationToken;
    var converter = Context.ServiceProvider.GetRequiredService<ConverterFacade>();
    ulong originalCount = 0UL, ugoiraCount = 0UL;
    if (original && converter.OriginalConverter is { } originalConverter)
    {
      originalCount = await EncodeConvertAsync(originalConverter, configSettings.OriginalFolder, delete, token).ConfigureAwait(false);
    }

    if (ugoira && converter.UgoiraZipConverter is { } ugoiraConverter)
    {
      ugoiraCount = await EncodeConvertAsync(ugoiraConverter, configSettings.UgoiraFolder, delete, token).ConfigureAwait(false);
    }

    logger.LogInformation($"Original: {originalCount} Ugoira: {ugoiraCount}");
  }

  private async ValueTask<ulong> EncodeConvertAsync(IConverter converter, string folder, bool delete, CancellationToken token)
  {
    var count = 0UL;
    for (var i = 0; i < 256; i++)
    {
      var folder0 = Path.Combine(folder, IOUtility.ByteTexts[i]);
      for (var j = 0; j < 256; j++)
      {
        var folder1 = Path.Combine(folder0, IOUtility.ByteTexts[j]);
        logger.LogInformation(folder1);
        await Parallel.ForEachAsync(Directory.EnumerateFiles(folder1, "*", SearchOption.TopDirectoryOnly), token, async (file, token) =>
        {
          token.ThrowIfCancellationRequested();
          var info = new FileInfo(file);
          if (await converter.TryConvertAsync(info, logger, token).ConfigureAwait(false) && delete)
          {
            _ = Interlocked.Increment(ref count);
            logger.LogInformation($"{VirtualCodes.BrightGreenColor}{info.Name}{VirtualCodes.NormalizeColor}");
            info.Delete();
          }
        }).ConfigureAwait(false);
      }
    }

    return count;
  }
}
