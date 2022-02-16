using PixivApi.Core.Local;

namespace PixivApi.Console;

[T4("Local", Kind.Utf8)]
public partial struct BookmarkMarkdownTemplate
{
    public BookmarkMarkdownTemplate(IEnumerable<Artwork> artworks, string originalDirectory, string outputFilePath, StringSet tagSet, ConcurrentDictionary<ulong, User> userDictionary)
    {
        Artworks = artworks;
        TagSet = tagSet;
        UserDictionary = userDictionary;
        RelativePath = Path.GetRelativePath(Path.GetDirectoryName(outputFilePath) ?? string.Empty, originalDirectory);
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
    public string RelativePath { get; }

    public int ArtworkCount { get; private set; }

    private void WritePath(ref Utf8ValueStringBuilder builder, ulong id, string name)
    {
        if (!string.IsNullOrEmpty(RelativePath))
        {
            bool isLastSlash = false;
            var enumerator = RelativePath.EnumerateRunes();
            while (enumerator.MoveNext())
            {
                var c = enumerator.Current;
                if (c.Value == '/' || c.Value == '\\')
                {
                    builder.GetSpan(1)[0] = (byte)'/';
                    builder.Advance(1);
                    isLastSlash = true;
                }
                else
                {
                    builder.Advance(c.EncodeToUtf8(builder.GetSpan(4)));
                    isLastSlash = false;
                }
            }

            if (!isLastSlash)
            {
                builder.GetSpan(1)[0] = (byte)'/';
                builder.Advance(1);
            }
        }

        builder.Append((byte)(id & 255), new StandardFormat('X', 2));
        builder.Append('/');
        builder.Append(name);
    }
}
