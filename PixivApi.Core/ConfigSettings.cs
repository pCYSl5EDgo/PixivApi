namespace PixivApi;

public partial class ConfigSettings : IEquatable<ConfigSettings>
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

    [JsonIgnore] public TimeSpan RetryTimeSpan => TimeSpan.FromSeconds(RetrySeconds);

    public ConfigSettings() { }

    public bool Equals(ConfigSettings? other) => other is not null
        && other.RefreshToken == RefreshToken
        && other.AppOS == AppOS
        && other.AppOSVersion == AppOSVersion
        && other.UserAgent == UserAgent
        && other.ClientId == ClientId
        && other.ClientSecret == ClientSecret
        && other.HashSecret == HashSecret;

    public override bool Equals(object? obj) => Equals(obj as ConfigSettings);

    public override int GetHashCode() => HashCode.Combine(RefreshToken, AppOS, AppOSVersion, UserAgent, ClientId, ClientSecret, HashSecret);
}
