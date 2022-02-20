namespace PixivApi.Core.Local;

public sealed class UserFilter : IFilter<User>
{
    [JsonPropertyName("follow")] public bool? IsFollowed;
    [JsonPropertyName("only-registered")] public bool OnlyRegistered = false;
    [JsonPropertyName("id-filter")] public IdFilter? IdFilter = null;
    [JsonPropertyName("show-hidden")] public bool ShowHiddenUsers = false;

    [JsonIgnore] public ConcurrentDictionary<ulong, User>? Dictionary;

    public bool Filter(ulong userId)
    {
        if (Dictionary is null)
        {
            return IdFilter is null || IdFilter.Filter(userId);
        }

        if (!Dictionary.TryGetValue(userId, out var user))
        {
            return !OnlyRegistered;
        }

        return Filter(user);
    }

    public bool Filter(User user)
    {
        if (Dictionary is not null && IsFollowed.HasValue)
        {
            if (!Dictionary.TryGetValue(user.Id, out var value))
            {
                return !IsFollowed.Value;
            }

            if (value.IsFollowed != IsFollowed.Value)
            {
                return false;
            }
        }

        if (!ShowHiddenUsers && user.ExtraHideReason != HideReason.NotHidden)
        {
            return false;
        }

        return IdFilter is null || IdFilter.Filter(user.Id);
    }
}