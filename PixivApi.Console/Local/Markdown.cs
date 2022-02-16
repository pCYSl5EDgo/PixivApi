using PixivApi.Core.Local.Filter;
using PixivApi.Core.Local;

namespace PixivApi.Console;

partial class LocalClient : ConsoleAppBase
{
    [Command("markdown", "Generate markdown file.")]
    public async ValueTask<int> GenerateMarkdownAsync(
            [Option(0, $"input {IOUtility.ArtworkDatabaseDescription}")] string input,
            [Option(1, IOUtility.FilterDescription)] string filter
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
        var fileName = Path.GetFileNameWithoutExtension(input) + ".md";
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

        var template = new BookmarkMarkdownTemplate(
            await ArtworkEnumerable.CreateAsync(database, artworkItemFilter, token).ConfigureAwait(false),
            configSettings.OriginalFolder, output,
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
