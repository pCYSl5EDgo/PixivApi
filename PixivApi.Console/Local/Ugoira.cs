using Cysharp.Diagnostics;
using PixivApi.Core;
using PixivApi.Core.Local;
using System.IO.Compression;

namespace PixivApi.Console;

public partial class LocalClient
{
    [Command("create-ugoira")]
    public async ValueTask CreateUgoiraAsync(
        [Option(0, ArgumentDescriptions.DatabaseDescription)] string path,
        [Option(1, "codec")] UgoiraCodec codec
    )
    {
        var token = Context.CancellationToken;
        if (token.IsCancellationRequested)
        {
            return;
        }

        var database = await IOUtility.MessagePackDeserializeAsync<DatabaseFile>(path, token).ConfigureAwait(false);
        if (database is null)
        {
            return;
        }

        var extension = codec.GetExtension();
        var template = new UgoiraFfmpegTemplate();
        foreach (var artwork in database.ArtworkDictionary.Values)
        {
            if (token.IsCancellationRequested)
            {
                return;
            }

            if (artwork.Type != ArtworkType.Ugoira || artwork.UgoiraFrames is not { Length: > 0 })
            {
                continue;
            }

            template.Frames = artwork.UgoiraFrames;
            var hashPath = Path.Combine(configSettings.UgoiraFolder, IOUtility.GetHashPath(artwork.Id));
            var partialPath = Path.Combine(hashPath, artwork.GetUgoiraZipFileNameWithoutExtension());
            var destPath = partialPath + extension;
            var zipPath = partialPath + ".zip";
            if (File.Exists(destPath) || !File.Exists(zipPath))
            {
                continue;
            }

            if (token.IsCancellationRequested)
            {
                return;
            }

            template.Directory = Path.GetFullPath(partialPath);
            Directory.CreateDirectory(template.Directory);
            try
            {
                bool isCompressed;
                using (var archive = ZipFile.Open(zipPath, ZipArchiveMode.Read))
                {
                    archive.ExtractToDirectory(template.Directory, true);
                    if (archive.Entries.Count == 0)
                    {
                        isCompressed = true;
                    }
                    else
                    {
                        var entry = archive.Entries[0];
                        isCompressed = entry.CompressedLength < entry.Length;
                    }
                }

                if (!isCompressed)
                {
                    File.Delete(zipPath);
                    ZipFile.CreateFromDirectory(template.Directory, zipPath, CompressionLevel.SmallestSize, false);
                }

                var builder = ZString.CreateUtf8StringBuilder();
                var textName = $"{artwork.Id}.txt";
                try
                {
                    template.TransformAppend(ref builder);
                    IOUtility.WriteToFile(Path.Combine(partialPath, textName), builder.AsSpan());
                }
                finally
                {
                    builder.Dispose();
                }

                static string CalcCommand(UgoiraCodec ugoiraCodec, string textName, string destPath)
                {
                    DefaultInterpolatedStringHandler handler = $"ffmpeg";
                    if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                    {
                        handler.AppendLiteral(".exe");
                    }

                    handler.AppendLiteral(" -f concat -i ");
                    handler.AppendFormatted(textName);
                    handler.AppendLiteral(" -c:v ");
                    handler.AppendLiteral(ugoiraCodec.GetCodecTextForFfmpeg());
                    handler.AppendLiteral(" -r 30 ");

                    if (ugoiraCodec == UgoiraCodec.h264)
                    {
                        handler.AppendLiteral("-pix_fmt yuv420p ");
                    }

                    handler.AppendLiteral(destPath);
                    return handler.ToStringAndClear();
                }

                var command = CalcCommand(codec, textName, destPath);
                await foreach (var output in ProcessX.StartAsync(command, workingDirectory: hashPath))
                {
                    logger.LogInformation(output);
                }
            }
            finally
            {
                Directory.Delete(template.Directory, true);
            }
        }
    }
}
