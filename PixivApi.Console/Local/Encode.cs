namespace PixivApi.Console;

public partial class LocalClient
{
    [Command("encode", "")]
    public async ValueTask EncodeAsync(
        [Option("o")] bool original = false,
        [Option("t")] bool thumbanil = false,
        [Option("u")] bool ugoira = false,
        [Option("d")] bool delete = false
    )
    {
        if (!original && !thumbanil && !ugoira)
        {
            return;
        }

        var token = Context.CancellationToken;
        if (original && converter.OriginalConverter is { } originalConverter)
        {
            for (var i = 0; i < 256; i++)
            {
                var folder0 = Path.Combine(configSettings.OriginalFolder, IOUtility.ByteTexts[i]);
                for (var j = 0; j < 256; j++)
                {
                    var folder1 = Path.Combine(folder0, IOUtility.ByteTexts[j]);
                    logger.LogInformation($"{i:X2}/{j:X2}");
                    foreach (var file in Directory.EnumerateFiles(folder1, "*", SearchOption.TopDirectoryOnly))
                    {
                        if (token.IsCancellationRequested)
                        {
                            return;
                        }

                        var info = new FileInfo(file);
                        if (await originalConverter.TryConvertAsync(info, logger, token).ConfigureAwait(false) && delete)
                        {
                            logger.LogInformation($"{VirtualCodes.BrightGreenColor}{info.Name}{VirtualCodes.NormalizeColor}");
                            info.Delete();
                        }
                    }
                }
            }
        }

        if (thumbanil && converter.ThumbnailConverter is { } thumbnailConverter)
        {
            for (var i = 0; i < 256; i++)
            {
                var folder0 = Path.Combine(configSettings.ThumbnailFolder, IOUtility.ByteTexts[i]);
                for (var j = 0; j < 256; j++)
                {
                    var folder1 = Path.Combine(folder0, IOUtility.ByteTexts[j]);
                    logger.LogInformation($"{i:X2}/{j:X2}");
                    foreach (var file in Directory.EnumerateFiles(folder1, "*", SearchOption.TopDirectoryOnly))
                    {
                        if (token.IsCancellationRequested)
                        {
                            return;
                        }

                        var info = new FileInfo(file);
                        if (await thumbnailConverter.TryConvertAsync(info, logger, token).ConfigureAwait(false) && delete)
                        {
                            logger.LogInformation($"{VirtualCodes.BrightGreenColor}{info.Name}{VirtualCodes.NormalizeColor}");
                            info.Delete();
                        }
                    }
                }
            }
        }

        if (ugoira && converter.UgoiraZipConverter is { } ugoiraConverter)
        {
            for (var i = 0; i < 256; i++)
            {
                var folder0 = Path.Combine(configSettings.UgoiraFolder, IOUtility.ByteTexts[i]);
                for (var j = 0; j < 256; j++)
                {
                    var folder1 = Path.Combine(folder0, IOUtility.ByteTexts[j]);
                    logger.LogInformation($"{i:X2}/{j:X2}");
                    foreach (var file in Directory.EnumerateFiles(folder1, "*", SearchOption.TopDirectoryOnly))
                    {
                        if (token.IsCancellationRequested)
                        {
                            return;
                        }

                        var info = new FileInfo(file);
                        if (await ugoiraConverter.TryConvertAsync(info, logger, token).ConfigureAwait(false) && delete)
                        {
                            logger.LogInformation($"{VirtualCodes.BrightGreenColor}{info.Name}{VirtualCodes.NormalizeColor}");
                            info.Delete();
                        }
                    }
                }
            }
        }
    }
}
