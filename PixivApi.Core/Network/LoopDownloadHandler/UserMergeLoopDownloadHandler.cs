namespace PixivApi;

public sealed class UserMergeLoopDownloadHandler : IMergeLoopDownloadHandler<UserPreviewsResponseData, UserDatabaseInfo>
{
    public UserMergeLoopDownloadHandler(Func<string, CancellationToken, ValueTask<byte[]?>> downloadAsync)
    {
        this.downloadAsync = downloadAsync;
        dictionary = new();
    }

    private readonly Dictionary<ulong, UserDatabaseInfo> dictionary;
    private readonly Func<string, CancellationToken, ValueTask<byte[]?>> downloadAsync;

    public IEnumerable<UserDatabaseInfo> Get()
    {
        if (dictionary.Count == 0)
        {
            return Array.Empty<UserDatabaseInfo>();
        }

        return dictionary.Values;
    }

    public async ValueTask<string?> GetNextUrlAsync(UserPreviewsResponseData container, CancellationToken token)
    {
        var array = container.GetContainer();
        if (array.Length == 0)
        {
            return null;
        }

        bool allContaind = true;
        foreach (var userPreview in array)
        {
            ulong userId = userPreview.User.Id;
            if (userId == 0)
            {
                continue;
            }


            var content = await downloadAsync($"https://app-api.pixiv.net/v1/user/detail?user_id={userId}", token).ConfigureAwait(false); ;
            if (content is not null && IOUtility.JsonDeserialize<UserDetailInfo>(content) is UserDetailInfo userDetail)
            {
                Renew(dictionary);
                void Renew(Dictionary<ulong, UserDatabaseInfo> dictionary)
                {
                    ref var target = ref CollectionsMarshal.GetValueRefOrAddDefault(dictionary, userId, out var exists);
                    var value = new UserDatabaseInfo(userDetail, userPreview);
                    if (exists)
                    {
                        OverwriteExtensions.Overwrite(ref target, value);
                    }
                    else
                    {
                        target = value;
                        allContaind = false;
                    }
                }
            }
            else
            {
                Renew(dictionary);
                void Renew(Dictionary<ulong, UserDatabaseInfo> dictionary)
                {
                    ref var target = ref CollectionsMarshal.GetValueRefOrAddDefault(dictionary, userId, out var exists);
                    var value = new UserDatabaseInfo(userPreview);
                    if (exists)
                    {
                        OverwriteExtensions.Overwrite(ref target, value);
                    }
                    else
                    {
                        target = value;
                        allContaind = false;
                    }
                }
            }
        }

        if (allContaind)
        {
            return null;
        }

        return container.NextUrl;
    }

    public void Initialize(IEnumerable<UserDatabaseInfo>? enumerable)
    {
        if (enumerable is null)
        {
            return;
        }

        foreach (var item in enumerable)
        {
            ulong id = item.User.Id;
            if (id == 0)
            {
                continue;
            }

            ref var target = ref CollectionsMarshal.GetValueRefOrAddDefault(dictionary, id, out var exists);
            if (exists)
            {
                OverwriteExtensions.Overwrite(ref target, item);
            }
            else
            {
                target = item;
            }
        }
    }

    public void Dispose()
    {
        dictionary.Clear();
    }
}
