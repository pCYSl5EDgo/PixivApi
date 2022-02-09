namespace PixivApi;

[MessagePackObject]
public struct ImageUrls
{
    [Key(0), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull), JsonPropertyName("square_medium")] public string? SquareMedium;
    [Key(1), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull), JsonPropertyName("medium")] public string? Medium;
    [Key(2), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull), JsonPropertyName("large")] public string? Large;
    [Key(3), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull), JsonPropertyName("original")] public string? Original;
}