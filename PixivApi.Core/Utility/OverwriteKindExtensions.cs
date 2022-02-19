namespace PixivApi;

public static class OverwriteKindExtensions
{
    public static OverwriteKind Parse(string? value) => value switch
    {
        "search-add" or "add-search" or "search-and-add" or "add-and-search" => OverwriteKind.SearchAndAdd,
        _ => OverwriteKind.Add,
    };
}
