namespace PixivApi;

[MessagePackObject]
public sealed class UserDatabaseInfo : IEquatable<UserDatabaseInfo>, IComparable<UserDatabaseInfo>, IOverwrite<UserDatabaseInfo>
{
    [Key(0)] public User User;
    [Key(1)] public UserDetailProfile? Profile;
    [Key(2)] public UserDetailProfilePublicity? ProfilePublicity;
    [Key(3)] public UserDetailWorkspace? Workspace;
    [Key(4)] public bool IsMuted;
    [Key(5)] public UserExtraInfo? ExtraInfo;

    [SerializationConstructor]
    public UserDatabaseInfo()
    {
    }

    public UserDatabaseInfo(UserPreview user)
    {
        Profile = null;
        ProfilePublicity = null;
        Workspace = null;
        ExtraInfo = null;
        User = user.User;
        IsMuted = user.IsMuted;
    }

    public UserDatabaseInfo(UserDetailInfo userDetail, UserPreview userPreview)
    {
        User = userDetail.User.Id != 0 ? userDetail.User : userPreview.User;
        Profile = userDetail.Profile;
        ProfilePublicity = userDetail.ProfilePublicity;
        Workspace = userDetail.Workspace;
        ExtraInfo = null;
        IsMuted = userPreview.IsMuted;
    }

    public int CompareTo(UserDatabaseInfo? other) => other is null ? 1 : User.Id.CompareTo(other.User.Id);

    public bool Equals(UserDatabaseInfo? other) => other is not null && User.Id == other.User.Id;

    public override string ToString() => User.Id.ToString();

    public void Overwrite(UserDatabaseInfo source)
    {
        if (!Equals(source))
        {
            return;
        }

        User = source.User;
        OverwriteExtensions.Overwrite(ref Profile, source.Profile);
        OverwriteExtensions.Overwrite(ref ProfilePublicity, source.ProfilePublicity);
        OverwriteExtensions.Overwrite(ref Workspace, source.Workspace);
        IsMuted = source.IsMuted;
        OverwriteExtensions.Overwrite(ref ExtraInfo, source.ExtraInfo);
    }

    public override bool Equals(object? obj) => obj is UserDatabaseInfo other && Equals(other);

    public override int GetHashCode() => User.GetHashCode();

    public static bool operator ==(UserDatabaseInfo left, UserDatabaseInfo right) => left.Equals(right);

    public static bool operator !=(UserDatabaseInfo left, UserDatabaseInfo right) => !(left == right);

    public static bool operator <(UserDatabaseInfo left, UserDatabaseInfo right) => left.CompareTo(right) < 0;

    public static bool operator <=(UserDatabaseInfo left, UserDatabaseInfo right) => left.CompareTo(right) <= 0;

    public static bool operator >(UserDatabaseInfo left, UserDatabaseInfo right) => left.CompareTo(right) > 0;

    public static bool operator >=(UserDatabaseInfo left, UserDatabaseInfo right) => left.CompareTo(right) >= 0;
}
