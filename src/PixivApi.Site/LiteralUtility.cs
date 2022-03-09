namespace PixivApi.Site;

internal static partial class LiteralUtility
{
    [StringLiteral.Utf8("artwork")]
    public static partial ReadOnlySpan<byte> LiteralArtwork();

    [StringLiteral.Utf8("ugoira")]
    public static partial ReadOnlySpan<byte> LiteralUgoira();

    [StringLiteral.Utf8("thumbnail")]
    public static partial ReadOnlySpan<byte> LiteralThumbnail();

    [StringLiteral.Utf8("original")]
    public static partial ReadOnlySpan<byte> LiteralOriginal();

    [StringLiteral.Utf8("\"")]
    public static partial ReadOnlySpan<byte> LiteralQuote();
}
