﻿using Cysharp.Text;
using Microsoft.Extensions.Logging;
using PixivApi.Core;
using PixivApi.Core.Local;
using System.IO.Compression;
using System.Runtime.CompilerServices;

[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Usage", "CA2254")]

namespace PixivApi.Plugin.UgoiraConverter.Ffmpeg;

public sealed record class Implementation(string ExePath, ConfigSettings ConfigSettings) : IFinder, IConverter
{
    public static Task<IPlugin?> CreateAsync(string dllPath, ConfigSettings configSettings, CancellationToken cancellationToken)
    {
        if (cancellationToken.IsCancellationRequested)
        {
            return Task.FromCanceled<IPlugin?>(cancellationToken);
        }

        var exePath = PluginUtility.Find(dllPath, "ffmpeg");
        return Task.FromResult<IPlugin?>(exePath is null ? null : new Implementation(exePath, configSettings));
    }

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;

    private string GetMp4Path(Artwork artwork) => Path.Combine(ConfigSettings.UgoiraFolder, IOUtility.GetHashPath(artwork.Id), $"{artwork.Id}.mp4");

    private string GetZipPath(Artwork artwork) => Path.Combine(ConfigSettings.UgoiraFolder, IOUtility.GetHashPath(artwork.Id), artwork.GetUgoiraZipFileName());

    public FileInfo Find(Artwork artwork)
    {
        var file = new FileInfo(GetZipPath(artwork));
        if (file.Exists)
        {
            return file;
        }

        return new(GetMp4Path(artwork));
    }

    public async ValueTask<bool> TryConvertAsync(Artwork artwork, ILogger? logger, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (artwork is not { Type: ArtworkType.Ugoira, UgoiraFrames.Length: > 0 })
        {
            return false;
        }

        var zipPath = GetZipPath(artwork);
        if (!File.Exists(zipPath))
        {
            return false;
        }

        var outputName = $"{artwork.Id}.mp4";
        var mp4Path = GetMp4Path(artwork);
        if (File.Exists(mp4Path))
        {
            return false;
        }

        cancellationToken.ThrowIfCancellationRequested();
        var textName = $"{artwork.Id}.txt";
        try
        {
            var tempDirectory = Path.GetTempPath();
            try
            {
                Decompress(zipPath, tempDirectory);
                CreateTextFile(textName, artwork.UgoiraFrames, tempDirectory);
                await ExecuteAsync(logger, textName, artwork.UgoiraFrames, outputName).ConfigureAwait(false);
                File.Move(outputName, mp4Path);
            }
            finally
            {
                Directory.Delete(tempDirectory, true);
            }
        }
        finally
        {
            if (File.Exists(textName))
            {
                File.Delete(textName);
            }
        }

        return true;
    }

    private Task ExecuteAsync(ILogger? logger, string textName, ushort[] frames, string outputName) => PluginUtility.ExecuteAsync(logger, ExePath, $"-f concat -safe 0 -i {textName} -c:v libaom-av1 -r {(TryCalculateFps(frames, out var fps) ? fps : 60)} {outputName}");

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

    private static void CreateTextFile(string textName, ushort[] frames, string tempDirectory)
    {
        var template = new UgoiraFfmpegTemplate(frames, tempDirectory);
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
    }

    private static void Decompress(string zipPath, string tempDirectory)
    {
        var shouldCompress = false;
        using (var archive = ZipFile.OpenRead(zipPath))
        {
            archive.ExtractToDirectory(tempDirectory, true);

            var entries = archive.Entries;
            if (entries.Count != 0 && entries[0].Length == entries[0].CompressedLength)
            {
                shouldCompress = true;
            }
        }

        if (shouldCompress)
        {
            File.Delete(zipPath);
            ZipFile.CreateFromDirectory(tempDirectory, zipPath, CompressionLevel.SmallestSize, false);
        }
    }

    public void DeleteUnneccessaryOriginal(Artwork artwork, ILogger? logger)
    {
        if (artwork is not { Type: ArtworkType.Ugoira, UgoiraFrames.Length: > 0 })
        {
            return;
        }

        var zipPath = GetZipPath(artwork);
        if (!File.Exists(zipPath))
        {
            return;
        }

        var outputName = $"{artwork.Id}.mp4";
        var mp4Path = GetMp4Path(artwork);
        if (!File.Exists(mp4Path))
        {
            return;
        }

        logger?.LogInformation($"Delete: {outputName}");
        File.Delete(mp4Path);
    }
}
