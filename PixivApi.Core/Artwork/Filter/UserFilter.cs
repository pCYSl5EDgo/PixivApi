namespace PixivApi;

public sealed class UserFilter : IAsyncInitailizable, IFilter<UserDatabaseInfo>
{
    [JsonPropertyName("database")] public string? Database;
    [JsonPropertyName("follow")] public bool? IsFollowed;
    [JsonPropertyName("only-registered")] public bool OnlyRegistered = false;
    [JsonPropertyName("id")] public ulong[]? Ids;
    [JsonPropertyName("ignore-id")] public ulong[]? IgnoreIds;
    [JsonPropertyName("show-hidden")] public bool ShowHiddenUsers = false;

    public bool Filter(ulong userId)
    {
        if (InfoDictionary is null)
        {
            return true;
        }

        if (!InfoDictionary.TryGetValue(userId, out var user))
        {
            return !OnlyRegistered;
        }

        return Filter(user);
    }

    public bool Filter(UserDatabaseInfo user)
    {
        if (InfoDictionary is not null && IsFollowed.HasValue)
        {
            ref var value = ref CollectionsMarshal.GetValueRefOrNullRef(InfoDictionary, user.User.Id);
            if (Unsafe.IsNullRef(ref value))
            {
                return !IsFollowed.Value;
            }

            if (value.User.IsFollowed != IsFollowed.Value)
            {
                return false;
            }
        }

        if (!ShowHiddenUsers && user.ExtraInfo is { HideReason: not HideReason.NotHidden })
        {
            return false;
        }

        var userId = user.User.Id;
        if (Ids is { Length: > 0 })
        {
            foreach (var id in Ids)
            {
                if (userId == id)
                {
                    goto OK;
                }
            }

            return false;
        OK:;
        }

        if (IgnoreIds is { Length: > 0 })
        {
            foreach (var id in IgnoreIds)
            {
                if (userId == id)
                {
                    return false;
                }
            }
        }

        return true;
    }

    private Dictionary<ulong, UserDatabaseInfo>? InfoDictionary;

    public async ValueTask InitializeAsync(string? directory, CancellationToken token)
    {
        if (string.IsNullOrWhiteSpace(Database))
        {
            return;
        }

        if (!Database.EndsWith(IOUtility.UserDatabaseFileExtension))
        {
            Database += IOUtility.UserDatabaseFileExtension;
        }

        if (!System.IO.File.Exists(Database))
        {
            if (string.IsNullOrWhiteSpace(directory))
            {
                return;
            }

            Database = Path.Combine(directory, Database);
        }

        var array = await IOUtility.MessagePackDeserializeAsync<UserDatabaseInfo[]>(Database, token).ConfigureAwait(false);
        if (array is not { Length: > 0 })
        {
            InfoDictionary = null;
            return;
        }

        InfoDictionary = new Dictionary<ulong, UserDatabaseInfo>();
        foreach (var item in array)
        {
            InfoDictionary[item.User.Id] = item;
        }
    }
}