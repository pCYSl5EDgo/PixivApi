namespace PixivApi;

[MessagePackObject]
public record struct Tag(
    [property: Key(0), JsonPropertyName("name")] string Name,
    [property: Key(1), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull), JsonPropertyName("translated_name")] string? TranslatedName
) : ITag
{
    [JsonIgnore] string ITag.Tag => Name;
}
