namespace PixivApi;

partial class NetworkClient
{
    [Command("follows-new-work")]
    public async ValueTask<int> DownloadNewIllustsOfFollowersAsync
    (
        [Option(0, $"output {IOUtility.ArtworkDatabaseDescription}")] string output,
        bool pipe = false
    )
    {
        output = IOUtility.FindArtworkDatabase(output, true)!;
        if (string.IsNullOrWhiteSpace(output))
        {
            return -2;
        }

        if (!await Connect().ConfigureAwait(false))
        {
            return -3;
        }

        var token = Context.CancellationToken;
        var url = $"https://{ApiHost}/v2/illust/follow?restrict=public";
        await OverwriteLoopDownloadAsync<DefaultLoopDownloadHandler<IllustsResponseData, ArtworkDatabaseInfo>, MergeLoopDownloadHandler<IllustsResponseData, ArtworkDatabaseInfo>, IllustsResponseData, ArtworkDatabaseInfo>(
            output, url, OverwriteKind.Add, pipe,
            new(),
            new(),
            IOUtility.MessagePackDeserializeAsync<ArtworkDatabaseInfo[]>,
            IOUtility.MessagePackSerializeAsync
        ).ConfigureAwait(false);
        return 0;
    }
}
