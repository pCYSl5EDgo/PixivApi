namespace PixivApi.Console;

public partial class NetworkClient
{
    [Command("bookmarks")]
    public ValueTask DownloadBookmarksOfUserAsync
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

        if (configSettings.UserId == 0UL)
        {
            logger.LogError($"{VirtualCodes.BrightRedColor}User Id should be written in appsettings.json{VirtualCodes.NormalizeColor}");
            return ValueTask.CompletedTask;
        }

        var url = $"https://{ApiHost}/v1/user/bookmarks/illust?user_id={configSettings.UserId}&restrict=public";
        return DownloadArtworkResponses(output, addBehaviour, pipe, url, Context.CancellationToken);
    }
}
