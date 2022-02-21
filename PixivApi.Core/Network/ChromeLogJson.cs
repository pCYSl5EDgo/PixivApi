namespace PixivApi.Core.Network;

public sealed class ChromeLogJson
{
    [JsonPropertyName("message")]
    public InnerChromeLogJson? Message { get; set; }
}

public sealed class InnerChromeLogJson
{
    [JsonPropertyName("method")]
    public string? Method { get; set; }

    [JsonPropertyName("params")]
    public InnerInnerChromeLogJson? Params { get; set; }
}

public sealed class InnerInnerChromeLogJson
{
    [JsonPropertyName("documentURL")]
    public string? DocumentUrl { get; set; }
}