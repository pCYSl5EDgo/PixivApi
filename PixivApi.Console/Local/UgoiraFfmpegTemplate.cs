namespace PixivApi.Console;

[T4("Local", Kind.Utf8)]
public partial struct UgoiraFfmpegTemplate
{
    /// <summary>
    /// Without trailing slash
    /// </summary>
    public string Directory { get; set; }

    public ushort[] Frames { get; set; }

    public UgoiraFfmpegTemplate()
    {
        Frames = Array.Empty<ushort>();
        Directory = string.Empty;
    }
}
