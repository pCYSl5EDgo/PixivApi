namespace PixivApi;

public record struct IllustsResponseData(
    [property: JsonPropertyName("illusts")] ArtworkDatabaseInfo[] Illusts,
    [property: JsonPropertyName("next_url")] string? NextUrl
) : INext, IArrayContainer<ArtworkDatabaseInfo>
{
    public ArtworkDatabaseInfo[] GetContainer() => Illusts;
}
