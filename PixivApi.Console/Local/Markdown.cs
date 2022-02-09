namespace PixivApi;

partial class LocalClient : ConsoleAppBase
{
    [Command("markdown", "Generate markdown file.")]
    public async ValueTask<int> GenerateMarkdownAsync(
            [Option(0, $"input {IOUtility.ArtworkDatabaseDescription}")] string input,
            [Option(1, IOUtility.FilterDescription)] string filter
        )
    {
        input = IOUtility.FindArtworkDatabase(input, true)!;
        if (string.IsNullOrWhiteSpace(input))
        {
            return -1;
        }

        var token = Context.CancellationToken;
        var artworkItemFilter = await IOUtility.JsonParseAsync<ArtworkDatabaseInfoFilter>(filter, token).ConfigureAwait(false);
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

        var template = new BookmarkMarkdownTemplate(
            (await ArtworkDatabaseInfoEnumerable.CreateAsync(await IOUtility.MessagePackDeserializeAsync<ArtworkDatabaseInfo[]>(input, token).ConfigureAwait(false) ?? Array.Empty<ArtworkDatabaseInfo>(), artworkItemFilter, token).ConfigureAwait(false)).ToArray(),
            configSettings.OriginalFolder, output
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

        logger.LogInformation($"Count: {template.Artworks.Length}");
        return 0;
    }
}
