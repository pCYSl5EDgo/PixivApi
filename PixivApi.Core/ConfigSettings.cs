namespace PixivApi.Core;

public sealed class ConfigSettings
{
    public string RefreshToken { get; set; } = "";
    public string AppOS { get; set; } = "ios";
    public string AppOSVersion { get; set; } = "14.6";
    public string UserAgent { get; set; } = "PixivIOSApp/7.13.3 (iOS 14.6; iPhone13,2)";
    public string ClientId { get; set; } = "MOBrBDS8blbauoSck0ZfDbtuzpyT";
    public string ClientSecret { get; set; } = "lsACyCD94FhDUtGTXi3QzcFE2uU1hqtDaKeqrdwj";
    public string HashSecret { get; set; } = "";
    public double RetrySeconds { get; set; } = 300d;
    public string OriginalFolder { get; set; } = "Original";
    public string ThumbnailFolder { get; set; } = "Thumbnail";
    public string UgoiraFolder { get; set; } = "Ugoira";
    public ulong UserId { get; set; }

    public string? UgoiraZipPlugin { get; set; }
    public string? UgoiraThumbnailPlugin { get; set; }
    public string? UgoiraOriginalPlugin { get; set; }

    public string? IllustZipPlugin { get; set; }
    public string? IllustThumbnailPlugin { get; set; }
    public string? IllustOriginalPlugin { get; set; }

    public string? MangaZipPlugin { get; set; }
    public string? MangaThumbnailPlugin { get; set; }
    public string? MangaOriginalPlugin { get; set; }

    [JsonIgnore] public TimeSpan RetryTimeSpan => TimeSpan.FromSeconds(RetrySeconds);

    public ConfigSettings() { }
}
