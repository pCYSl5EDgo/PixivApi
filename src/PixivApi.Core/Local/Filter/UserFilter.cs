﻿namespace PixivApi.Core.Local;

public sealed class UserFilter
{
    [JsonPropertyName("follow")] public bool? IsFollowed;
    [JsonPropertyName("only-registered")] public bool OnlyRegistered = false;
    [JsonPropertyName("id-filter")] public IdFilter? IdFilter = null;
    [JsonPropertyName("name-filter")] public TextFilter? NameFilter = null;
    [JsonPropertyName("show-hidden")] public bool ShowHiddenUsers = false;
    [JsonPropertyName("tag-filter")] public TagFilter? TagFilter = null;

    [JsonIgnore] public ConcurrentDictionary<ulong, User>? Dictionary;

    [MemberNotNull(nameof(Dictionary))]
    public async ValueTask InitializeAsync(ConcurrentDictionary<ulong, User> userDictionary, StringSet tagSet, CancellationToken token)
    {
        Dictionary = userDictionary;
        if (TagFilter is not null)
        {
            await TagFilter.InitializeAsync(tagSet, token).ConfigureAwait(false);
        }
    }

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
        if (user.ExtraHideReason != HideReason.NotHidden)
        {
            return false;
        }

        if (IsFollowed.HasValue && user.IsFollowed != IsFollowed.Value)
        {
            return false;
        }

        if (!ShowHiddenUsers && user.ExtraHideReason != HideReason.NotHidden)
        {
            return false;
        }

        if (IdFilter is not null && !IdFilter.Filter(user.Id))
        {
            return false;
        }

        if (NameFilter is not null && !NameFilter.Filter(MemoryMarshal.CreateReadOnlySpan(ref user.Name, 1)))
        {
            return false;
        }

        if (TagFilter is not null && !TagFilter.Filter(Array.Empty<uint>(), user.ExtraTags, null))
        {
            return false;
        }

        return true;
    }
}