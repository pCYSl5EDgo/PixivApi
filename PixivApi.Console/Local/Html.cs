using PixivApi.Core;
using PixivApi.Core.Local;

namespace PixivApi.Console;

public sealed partial class LocalClient
{
    [Command("html")]
    public async ValueTask<int> HtmlAsync(
        [Option(0, $"input {ArgumentDescriptions.DatabaseDescription}")] string input,
        [Option(1, ArgumentDescriptions.FilterDescription)] string filter
    )
    {
        if (string.IsNullOrWhiteSpace(input))
        {
            return -1;
        }

        var token = Context.CancellationToken;
        var artworkItemFilter = await IOUtility.JsonDeserializeAsync<ArtworkFilter>(filter, token).ConfigureAwait(false);
        if (artworkItemFilter is null)
        {
            return 0;
        }

        string output;
        var dir = Path.GetDirectoryName(input);
        var fileName = Path.GetFileNameWithoutExtension(input) + ".html";
        if (dir is null)
        {
            output = fileName;
        }
        else
        {
            output = Path.Combine(dir, fileName);
        }

        var database = await IOUtility.MessagePackDeserializeAsync<DatabaseFile>(input, token).ConfigureAwait(false);
        if (database is null)
        {
            return 0;
        }

        var parallelOptions = new ParallelOptions()
        {
            CancellationToken = token,
            MaxDegreeOfParallelism = configSettings.MaxParallel,
        };
        var template = new HtmlTemplate(
            await ArtworkEnumerableHelper.CreateAsync(configSettings, database, artworkItemFilter, parallelOptions).ConfigureAwait(false),
            configSettings.OriginalFolder, configSettings.ThumbnailFolder, configSettings.UgoiraFolder, output,
            database.TagSet, database.UserDictionary
        );
        var builder = ZString.CreateUtf8StringBuilder();
        try
        {
            template.TransformAppend(ref builder);
            using var handle = File.OpenHandle(output, FileMode.Create, FileAccess.Write, FileShare.Read, FileOptions.Asynchronous);
            await RandomAccess.WriteAsync(handle, builder.AsMemory(), 0, token).ConfigureAwait(false);
        }
        finally
        {
            builder.Dispose();
        }

        logger.LogInformation($"{template.ArtworkCount}");
        return 0;
    }
}