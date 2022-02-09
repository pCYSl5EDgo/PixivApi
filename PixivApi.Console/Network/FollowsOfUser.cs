namespace PixivApi;

partial class NetworkClient
{
    [Command("follows")]
    public async ValueTask<int> DownloadFollowsOfUserAsync
    (
        [Option(0, IOUtility.UserIdDescription)] ulong id,
        [Option("o", $"output {IOUtility.UserDatabaseDescription}")] string? output = null,
        [Option(null, IOUtility.OverwriteKindDescription)] string overwrite = "add",
        bool pipe = false
    )
    {
        output = IOUtility.FindUserDatabase(output, false) ?? $"{id}{IOUtility.UserDatabaseFileExtension}";
        if (!await Connect().ConfigureAwait(false))
        {
            return -3;
        }

        var token = Context.CancellationToken;
        var url = $"https://{ApiHost}/v1/user/following?user_id={id}";

        await OverwriteLoopDownloadAsync<UserLoopDownloadHandler, UserMergeLoopDownloadHandler, UserPreviewsResponseData, UserDatabaseInfo>(
            output, url, OverwriteKindExtensions.Parse(overwrite), pipe,
            new(RetryGetAsync),
            new(RetryGetAsync),
            IOUtility.MessagePackDeserializeAsync<UserDatabaseInfo[]>,
            IOUtility.MessagePackSerializeAsync
        ).ConfigureAwait(false);
        return 0;
    }
}
