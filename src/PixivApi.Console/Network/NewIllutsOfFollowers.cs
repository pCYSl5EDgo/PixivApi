namespace PixivApi.Console;

public partial class NetworkClient
{
    [Command("follows-new-work")]
    public ValueTask DownloadNewIllustsOfFollowersAsync
    (
        [Option("a", ArgumentDescriptions.AddKindDescription)] bool addBehaviour = false,
        [Option("d")] bool download = false
    )
    {
        if (string.IsNullOrWhiteSpace(configSettings.DatabaseFilePath))
        {
            return ValueTask.CompletedTask;
        }

        var url = $"https://{ApiHost}/v2/illust/follow?restrict=public";
        return DownloadArtworkResponses(addBehaviour, download, url, Context.CancellationToken);
    }
}
