namespace PixivApi;

[T4("Local", Kind.Utf8)]
public partial struct BookmarkMarkdownTemplate
{
    public BookmarkMarkdownTemplate(ArtworkDatabaseInfo[] artworks, string originalDirectory, string outputFilePath)
    {
        Artworks = artworks;
        RelativePath = Path.GetRelativePath(Path.GetDirectoryName(outputFilePath) ?? string.Empty, originalDirectory);
    }

    public ArtworkDatabaseInfo[] Artworks { get; }
    public string RelativePath { get; }

    private void WritePath(ref Utf8ValueStringBuilder builder, string url)
    {
        var name = url.AsSpan((url.LastIndexOf('/') + 1));
        if (!string.IsNullOrEmpty(RelativePath))
        {
            bool isLastSlash = false;
            foreach (var c in RelativePath)
            {
                if (c == '\\' || c == '/')
                {
                    builder.GetSpan(1)[0] = (byte)'/';
                    builder.Advance(1);
                    isLastSlash = true;
                }
                else
                {
                    builder.Append(c);
                    isLastSlash = false;
                }
            }

            if (!isLastSlash)
            {
                builder.GetSpan(1)[0] = (byte)'/';
                builder.Advance(1);
            }
        }

        builder.Append(name);
    }
}
