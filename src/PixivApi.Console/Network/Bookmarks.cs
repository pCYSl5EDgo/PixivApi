namespace PixivApi.Console;

public partial class NetworkClient
{
    [Command("bookmarks")]
    public ValueTask DownloadBookmarksOfUserAsync
    (
        [Option("a", ArgumentDescriptions.AddKindDescription)] bool addBehaviour = false
    )
    {
        if (string.IsNullOrWhiteSpace(configSettings.DatabaseFilePath))
        {
            return ValueTask.CompletedTask;
        }

        if (configSettings.UserId == 0UL)
        {
            Context.Logger.LogError($"{VirtualCodes.BrightRedColor}User Id should be written in appsettings.json{VirtualCodes.NormalizeColor}");
            return ValueTask.CompletedTask;
        }

        var url = $"https://{ApiHost}/v1/user/bookmarks/illust?user_id={configSettings.UserId}&restrict=public";
        return DownloadArtworkResponses(addBehaviour, url, Context.CancellationToken);
    }
}
