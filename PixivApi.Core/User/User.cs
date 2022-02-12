namespace PixivApi;

[MessagePackObject]
public struct User : IEquatable<User>
{
    [Key(0), JsonPropertyName("id")] public ulong Id;
    [Key(1), JsonPropertyName("name")] public string Name;
    [Key(2), JsonPropertyName("account")] public string Account;
    [Key(3), JsonPropertyName("is_followed")] public bool IsFollowed;
    [Key(4), JsonPropertyName("profile_image_urls")] public ImageUrls ProfileImageUrls;
    [Key(5), JsonPropertyName("comment")] public string? Comment;

    public bool Equals(User other) => Id == other.Id;

    public override bool Equals(object? obj) => obj is User user && Equals(user);

    public static bool operator ==(User left, User right) => left.Equals(right);

    public static bool operator !=(User left, User right)
    {
        return !(left == right);
    }

    public override int GetHashCode() => HashCode.Combine(Id);
}
