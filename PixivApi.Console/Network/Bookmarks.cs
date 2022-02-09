namespace PixivApi;

partial class NetworkClient
{
    [Command("bookmarks")]
    public async ValueTask<int> DownloadBookmarksOfUserAsync
    (
        [Option(0, IOUtility.UserIdDescription)] ulong id,
        [Option("o", $"output {IOUtility.ArtworkDatabaseDescription}")] string? output = null,
        bool isPublic = true,
        [Option(null, IOUtility.OverwriteKindDescription)] string overwrite = "add",
        bool pipe = false
    )
    {
        if (!await Connect().ConfigureAwait(false))
        {
            return -3;
        }

        var token = Context.CancellationToken;
        output = IOUtility.FindArtworkDatabase(output, false) ?? $"{id}{IOUtility.ArtworkDatabaseFileExtension}";
        var url = GetUrl(id, isPublic);

        await OverwriteLoopDownloadAsync<DefaultLoopDownloadHandler<IllustsResponseData, ArtworkDatabaseInfo>, MergeLoopDownloadHandler<IllustsResponseData, ArtworkDatabaseInfo>, IllustsResponseData, ArtworkDatabaseInfo>(
            output, url, OverwriteKindExtensions.Parse(overwrite), pipe,
            new(),
            new(),
            IOUtility.MessagePackDeserializeAsync<ArtworkDatabaseInfo[]>,
            IOUtility.MessagePackSerializeAsync
        ).ConfigureAwait(false);
        return 0;

        static string GetUrl(ulong userId, bool isPublic)
        {
            DefaultInterpolatedStringHandler url = $"https://{ApiHost}/v1/user/bookmarks/illust?user_id={userId}";
            if (isPublic)
            {
                url.AppendLiteral("&restrict=public");
            }
            else
            {
                url.AppendLiteral("&restrict=private");
            }

            return url.ToString();
        }
    }
}
