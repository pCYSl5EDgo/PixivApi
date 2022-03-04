namespace PixivApi.Console;

public partial class NetworkClient
{
    [Command("follows-new-work")]
    public ValueTask DownloadNewIllustsOfFollowersAsync
    (
        [Option(0, $"output {ArgumentDescriptions.DatabaseDescription}")] string output,
        [Option("a", ArgumentDescriptions.AddKindDescription)] bool addBehaviour = false,
        bool pipe = false
    )
    {
        if (string.IsNullOrWhiteSpace(output))
        {
            return ValueTask.CompletedTask;
        }

        var url = $"https://{ApiHost}/v2/illust/follow?restrict=public";
        return DownloadArtworkResponses(output, addBehaviour, pipe, url, Context.CancellationToken);
    }
}
