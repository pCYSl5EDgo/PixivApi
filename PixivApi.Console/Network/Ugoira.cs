using PixivApi.Core;
using PixivApi.Core.Local;
using System.IO.Compression;
using Xabe.FFmpeg;
using Xabe.FFmpeg.Downloader;

namespace PixivApi.Console;

public sealed partial class NetworkClient
{
    [Command("create-ugoira-webm")]
    public async ValueTask CreateUgoiraWebmAsync(
        [Option(0, ArgumentDescriptions.DatabaseDescription)] string path,
        string ffmpegDirectory = "Chrome",
        bool pipe = false
    )
    {
        var token = Context.CancellationToken;
        var database = await IOUtility.MessagePackDeserializeAsync<DatabaseFile>(path, token).ConfigureAwait(false);
        if (database is null)
        {
            return;
        }

        FFmpeg.SetExecutablesPath(ffmpegDirectory);
        if (!Directory.EnumerateFiles(ffmpegDirectory, "ffmpeg*", SearchOption.TopDirectoryOnly).Any())
        {
            System.Console.Write($"{ConsoleUtility.WarningColor}Cannot cancel while downloading ffmpeg{ConsoleUtility.NormalizeColor}");
            await FFmpegDownloader.GetLatestVersion(FFmpegVersion.Official, "Chrome").ConfigureAwait(false);
            System.Console.Write(ConsoleUtility.DeleteLine1);
        }

        static ulong GetId(string path)
        {
            var name = Path.GetFileNameWithoutExtension(path.AsSpan());
            return ulong.Parse(name[0..name.IndexOf('_')]);
        }

        var update = 0UL;
        var zipCount = 0UL;
        const string videExtension = ".webm";
        foreach (var zipPath in Directory.EnumerateFiles(configSettings.UgoiraFolder, "*.zip", SearchOption.AllDirectories))
        {
            var videoPath = $"{zipPath.AsSpan(0, zipPath.Length - 4)}{videExtension}";
            if (File.Exists(videoPath))
            {
                return;
            }

            var id = GetId(zipPath);
            var artwork = database.Artworks.FirstOrDefault(x => x.Id == id);
            if (artwork is null)
            {
                return;
            }

            if (artwork.UgoiraFrames is not { Length: > 0 })
            {
                var frames = await GetUgoiraMetadataAsync(id, token).ConfigureAwait(false);
                Array.Sort(frames, (x, y) => GetId(x.File).CompareTo(GetId(y.File)));
                artwork.UgoiraFrames = frames.Length == 0 ? Array.Empty<ushort>() : new ushort[frames.Length];
                for (var i = 0; i < frames.Length; i++)
                {
                    artwork.UgoiraFrames[i] = (ushort)frames[i].Delay;
                }
                Interlocked.Increment(ref update);
            }

            var myCount = Interlocked.Increment(ref zipCount);
            var folder = Path.Combine(Path.GetTempPath(), $"zip_expand_{myCount}");
            ZipFile.ExtractToDirectory(zipPath, folder, true);
            try
            {
                var conversion = FFmpeg.Conversions.New();
                conversion = conversion.SetInputFrameRate(30d);
                conversion = conversion.BuildVideoFromImages(artwork.UgoiraFrames.Select((x, i) => Path.Combine(folder, $"{i:D6}{artwork.GetExtensionText()}")));
                conversion.SetFrameRate(30);
                conversion.SetOutput(videoPath);
                var conversionResult = await conversion.Start().ConfigureAwait(false);
                logger.LogInformation($"{videoPath}: Duration: {conversionResult.Duration} Arguments: {conversionResult.Arguments}");
            }
            finally
            {
                Directory.Delete(folder, true);
            }
        }

        if (update != 0)
        {
            await IOUtility.MessagePackSerializeAsync(path, database, FileMode.Create).ConfigureAwait(false);
        }
    }

    private async ValueTask<Core.Network.UgoiraMetadataResponseData.Frame[]> GetUgoiraMetadataAsync(ulong id, CancellationToken token)
    {
        var url = $"https://{ApiHost}/v1/ugoira/metadata?illust_id={id}";
        var content = await RetryGetAsync(url, token).ConfigureAwait(false);
        var response = IOUtility.JsonDeserialize<Core.Network.UgoiraMetadataResponseData>(content.AsSpan());
        return response.Value.Frames;
    }
}
