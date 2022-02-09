namespace PixivApi;

public sealed class UserLoopDownloadHandler : ILoopDownloadHandler<UserPreviewsResponseData, UserDatabaseInfo>
{
    public UserLoopDownloadHandler(Func<string, CancellationToken, ValueTask<byte[]?>> downloadAsync)
    {
        list = new();
        this.downloadAsync = downloadAsync;
    }

    private readonly List<UserDatabaseInfo> list;
    private readonly Func<string, CancellationToken, ValueTask<byte[]?>> downloadAsync;

    public IEnumerable<UserDatabaseInfo> Get()
    {
        if (list.Count == 0)
        {
            return Array.Empty<UserDatabaseInfo>();
        }

        return list;
    }

    public async ValueTask<string?> GetNextUrlAsync(UserPreviewsResponseData container, CancellationToken token)
    {
        var array = container.GetContainer();
        if (array.Length == 0)
        {
            return null;
        }

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
                list.Add(new UserDatabaseInfo(userDetail, userPreview));
            }
            else
            {
                list.Add(new UserDatabaseInfo(userPreview));
            }
        }

        return container.NextUrl;
    }

    public void Dispose()
    {
        list.Clear();
    }
}
