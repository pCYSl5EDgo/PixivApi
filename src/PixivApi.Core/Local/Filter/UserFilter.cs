namespace PixivApi.Core.Local;

public sealed class UserFilter : IFilter<User>
{
  [JsonPropertyName("follow")] public bool? IsFollowed;
  [JsonPropertyName("only-registered")] public bool OnlyRegistered = false;
  [JsonPropertyName("id-filter")] public IdFilter? IdFilter = null;
  [JsonPropertyName("name-filter")] public TextFilter? NameFilter = null;
  [JsonPropertyName("hide-filter")] public HideFilter? HideFilter = null;
  [JsonPropertyName("tag-filter")] public TagFilter? TagFilter = null;

  public bool HasSlowFilter => false;

  public void Initialize(IDatabase database) => TagFilter?.Initialize(database);

  public bool FastFilter(User user)
  {
    if (user.ExtraHideReason != HideReason.NotHidden)
    {
      return false;
    }

    if (IsFollowed.HasValue && user.IsFollowed != IsFollowed.Value)
    {
      return false;
    }

    if (HideFilter is null)
    {
      if (user.ExtraHideReason != HideReason.NotHidden)
      {
        return false;
      }
    }
    else if (!HideFilter.Filter(user.ExtraHideReason))
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

    return true;
  }

  public async ValueTask<bool> SlowFilter(User user, CancellationToken token)
  {
    if (TagFilter is not null && !await TagFilter.FilterAsync(user.ExtraTags?.ToDictionary(_ => 1U) ?? new(), token).ConfigureAwait(false))
    {
      return false;
    }

    return true;
  }
}
