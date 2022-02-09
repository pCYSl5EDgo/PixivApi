namespace PixivApi;

[MessagePackObject]
public sealed class UserPreview : IComparable<UserPreview>, IEquatable<UserPreview>, IOverwrite<UserPreview>
{
    [Key(0), JsonPropertyName("user")] public User User;
    [Key(1), JsonPropertyName("illusts")] public ArtworkDatabaseInfo[]? Illusts;
    [Key(3), JsonPropertyName("is_muted")] public bool IsMuted;

    public UserPreview()
    {
        User = default;
        Illusts = default;
        IsMuted = default;
    }

    public UserPreview(User user, ArtworkDatabaseInfo[]? illusts, bool isMuted)
    {
        User = user;
        Illusts = illusts;
        IsMuted = isMuted;
    }

    public int CompareTo(UserPreview? other)
    {
        if (other is null)
        {
            return 1;
        }

        if (ReferenceEquals(this, other))
        {
            return 0;
        }

        if (IsMuted)
        {
            if (!other.IsMuted)
            {
                return 1;
            }
        }
        else
        {
            if (other.IsMuted)
            {
                return -1;
            }
        }

        return User.Id.CompareTo(other.User.Id);
    }

    public bool Equals(UserPreview? other) => other is not null && User.Equals(other.User);

    public void Overwrite(UserPreview source)
    {
        if (!Equals(source))
        {
            return;
        }

        User = source.User;
        IsMuted = source.IsMuted;
        OverwriteExtensions.Overwrite(ref Illusts, source.Illusts);
    }

    public override bool Equals(object? obj) => ReferenceEquals(this, obj) || Equals(obj as UserPreview);

    public override int GetHashCode() => HashCode.Combine(User.Id);

    public static bool operator ==(UserPreview? left, UserPreview? right) => left is null ? right is null : left.Equals(right);
    public static bool operator !=(UserPreview? left, UserPreview? right) => !(left == right);
    public static bool operator <=(UserPreview? left, UserPreview? right) => left is null || left.CompareTo(right) <= 0;
    public static bool operator >=(UserPreview? left, UserPreview? right) => left is null ? right is null : left.CompareTo(right) >= 0;
    public static bool operator <(UserPreview? left, UserPreview? right) => left is null ? right is not null : left.CompareTo(right) < 0;
    public static bool operator >(UserPreview? left, UserPreview? right) => left is not null && left.CompareTo(right) > 0;
}
