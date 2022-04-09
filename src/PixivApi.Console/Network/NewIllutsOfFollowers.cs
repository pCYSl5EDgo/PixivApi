namespace PixivApi.Console;

public partial class NetworkClient
{
    [Command("follows-new-work")]
    public ValueTask DownloadNewIllustsOfFollowersAsync
    (
        [Option("a", ArgumentDescriptions.AddKindDescription)] bool addBehaviour = false,
        [Option("d")] bool download = false,
        [Option("p")] bool isPrivate = false
    )
    {
        if (string.IsNullOrWhiteSpace(configSettings.DatabaseFilePath))
        {
            return ValueTask.CompletedTask;
        }

        DefaultInterpolatedStringHandler url = $"https://{ApiHost}/v2/illust/follow?restrict=";
        url.AppendLiteral(isPrivate ? "private" : "public");
        return DownloadArtworkResponses(addBehaviour, download, url.ToStringAndClear(), Context.CancellationToken);
    }
}
