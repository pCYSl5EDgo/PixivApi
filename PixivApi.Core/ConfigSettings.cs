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

    public string? UgoiraZipFinderPlugin { get; set; }
    public string? UgoiraThumbnailFinderPlugin { get; set; }
    public string? UgoiraOriginalFinderPlugin { get; set; }
    public string? IllustThumbnailFinderPlugin { get; set; }
    public string? IllustOriginalFinderPlugin { get; set; }
    public string? MangaThumbnailFinderPlugin { get; set; }
    public string? MangaOriginalFinderPlugin { get; set; }

    public string? UgoiraZipConverterPlugin { get; set; }
    public string? UgoiraThumbnailConverterPlugin { get; set; }
    public string? UgoiraOriginalConverterPlugin { get; set; }
    public string? IllustThumbnailConverterPlugin { get; set; }
    public string? IllustOriginalConverterPlugin { get; set; }
    public string? MangaThumbnailConverterPlugin { get; set; }
    public string? MangaOriginalConverterPlugin { get; set; }

    [JsonIgnore] public TimeSpan RetryTimeSpan => TimeSpan.FromSeconds(RetrySeconds);
}
