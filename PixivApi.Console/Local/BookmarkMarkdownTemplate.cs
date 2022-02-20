using PixivApi.Core.Local;

namespace PixivApi.Console;

[T4("Local", Kind.Utf8)]
public partial struct BookmarkMarkdownTemplate
{
    public BookmarkMarkdownTemplate(IEnumerable<Artwork> artworks, string originalDirectory, string thumbnailDirectory, string outputFilePath, StringSet tagSet, ConcurrentDictionary<ulong, User> userDictionary)
    {
        Artworks = artworks;
        TagSet = tagSet;
        UserDictionary = userDictionary;
        var outputDirectory = Path.GetDirectoryName(outputFilePath) ?? string.Empty;
        static string Format(string outputDirectory, string directory)
        {
            var relative = Path.GetRelativePath(outputDirectory, directory);
            if (relative is not { Length: > 0 })
            {
                return string.Empty;
            }

            if (relative[^1] == Path.DirectorySeparatorChar || relative[^1] == Path.AltDirectorySeparatorChar)
            {
                if (!relative.Contains('\\'))
                {
                    return relative;
                }

                return string.Create(relative.Length, relative, static (span, value) =>
                {
                    var source = value.AsSpan();
                    for (int i = 0; i < source.Length; i++)
                    {
                        var c = source[i];
                        span[i] = c == '\\' ? '/' : c;
                    }
                });
            }
            else
            {
                return string.Create(relative.Length + 1, relative, static (span, value) =>
                {
                    var source = value.AsSpan();
                    for (int i = 0; i < source.Length; i++)
                    {
                        var c = source[i];
                        span[i] = c == '\\' ? '/' : c;
                    }

                    span[source.Length] = '/';
                });
            }
        }

        RelativePathToOriginal = Format(outputDirectory, originalDirectory);
        RelativePathToThumbnail = Format(outputDirectory, thumbnailDirectory);
        if (artworks.TryGetNonEnumeratedCount(out var nonEnumeratedCount))
        {
            ArtworkCount = nonEnumeratedCount;
        }
        else
        {
            ArtworkCount = 0;
        }
    }

    public IEnumerable<Artwork> Artworks { get; }
    public StringSet TagSet { get; }
    public ConcurrentDictionary<ulong, User> UserDictionary { get; }
    public string RelativePathToOriginal { get; }
    public string RelativePathToThumbnail { get; }
    public int ArtworkCount { get; private set; }

    private void WritePath(ref Utf8ValueStringBuilder builder, ulong id, string name)
    {
        builder.Append(RelativePathToOriginal);
        builder.Append(IOUtility.GetHashPath(id));
        builder.Append('/');
        builder.Append(name);
    }
}
