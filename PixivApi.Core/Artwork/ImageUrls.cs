namespace PixivApi;

[MessagePackObject]
public struct ImageUrls
{
    [Key(0), JsonPropertyName("square_medium")] public string? SquareMedium;
    [Key(1), JsonPropertyName("medium")] public string? Medium;
    [Key(2), JsonPropertyName("large")] public string? Large;
    [Key(3), JsonPropertyName("original")] public string? Original;
}