namespace PixivApi.Core.SqliteDatabase;

internal sealed partial class Database
{
    [StringLiteral.Utf8("\"UserTable\"")] private static partial ReadOnlySpan<byte> Literal_UserTable();
    [StringLiteral.Utf8("\"ArtworkTable\"")] private static partial ReadOnlySpan<byte> Literal_ArtworkTable();
    [StringLiteral.Utf8("\"TagTable\"")] private static partial ReadOnlySpan<byte> Literal_TagTable();
    [StringLiteral.Utf8("\"ToolTable\"")] private static partial ReadOnlySpan<byte> Literal_ToolTable();
    [StringLiteral.Utf8("\"RankingTable\"")] private static partial ReadOnlySpan<byte> Literal_RankingTable();
}
