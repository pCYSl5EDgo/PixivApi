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
        var twoTasks = new Task[2];
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
            var partialPath = Path.Combine(hashPath, artwork.Id.ToString());
            var zipPath = Path.Combine(hashPath, artwork.GetUgoiraZipFileName());
            var destPath = partialPath + extension;
            if (File.Exists(destPath) || !File.Exists(zipPath))
            {
                continue;
            }

            if (token.IsCancellationRequested)
            {
                return;
            }

            var textName = $"{artwork.Id}.txt";
            template.Directory = Path.GetFullPath(partialPath);
            Directory.CreateDirectory(template.Directory);
            try
            {
                Decompress(template.Directory, zipPath);

                var builder = ZString.CreateUtf8StringBuilder();
                try
                {
                    template.TransformAppend(ref builder);
                    IOUtility.WriteToFile(textName, builder.AsSpan());
                }
                finally
                {
                    builder.Dispose();
                }

                var tempDest = $"{artwork.Id}{extension}";
                string CalcCommand()
                {
                    DefaultInterpolatedStringHandler handler = $"ffmpeg";
                    if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                    {
                        handler.AppendLiteral(".exe");
                    }

                    handler.AppendLiteral(" -f concat -safe 0 -i ");
                    handler.AppendLiteral(textName);
                    handler.AppendLiteral(" -c:v ");
                    handler.AppendLiteral(codec.GetCodecTextForFfmpeg());
                    handler.AppendLiteral(" -r ");

                    if (TryCalculateFps(artwork.UgoiraFrames, out var fps))
                    {
                        handler.AppendFormatted(fps);
                    }
                    else
                    {
                        handler.AppendFormatted(60);
                    }

                    handler.AppendLiteral(" ");
                    if (codec == UgoiraCodec.h264)
                    {
                        handler.AppendLiteral("-pix_fmt yuv420p ");
                    }

                    handler.AppendLiteral(tempDest);
                    return handler.ToStringAndClear();
                }

                var command = CalcCommand();
                logger.LogInformation(command);
                var (_, standardOutput, standardError) = ProcessX.GetDualAsyncEnumerable(command);
                twoTasks[0] = Task.Run(async () =>
                {
                    await foreach (var item in standardOutput)
                    {
                        logger.LogInformation(item);
                    }
                });
                twoTasks[1] = Task.Run(async () =>
                {
                    await foreach (var item in standardError)
                    {
                        logger.LogWarning(item);
                    }
                });

                await Task.WhenAll(twoTasks).ConfigureAwait(false);
                if (File.Exists(tempDest))
                {
                    File.Move(tempDest, destPath);
                }
            }
            finally
            {
                if (!string.IsNullOrWhiteSpace(textName) && File.Exists(textName))
                {
                    File.Delete(textName);
                }

                Directory.Delete(template.Directory, true);
            }
        }
    }

    private static bool TryCalculateFps(ushort[] frames, out uint framePerSecond)
    {
        if (frames.Length == 0)
        {
            goto FAIL;
        }

        var first = frames[0];
        if (first == 0)
        {
            goto FAIL;
        }

        framePerSecond = (uint)(1000 / first);
        if (framePerSecond == 0)
        {
            goto FAIL;
        }

        for (var i = 1; i < frames.Length; i++)
        {
            if (frames[i] != first)
            {
                goto FAIL;
            }
        }

        return true;

    FAIL:
        Unsafe.SkipInit(out framePerSecond);
        return false;
    }


    private static void Decompress(string directory, string zipPath)
    {
        bool isCompressed;
        using (var archive = ZipFile.Open(zipPath, ZipArchiveMode.Read))
        {
            archive.ExtractToDirectory(directory, true);
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
            ZipFile.CreateFromDirectory(directory, zipPath, CompressionLevel.SmallestSize, false);
        }
    }
}
