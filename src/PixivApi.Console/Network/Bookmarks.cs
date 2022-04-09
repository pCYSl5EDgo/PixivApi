namespace PixivApi.Console;

public partial class NetworkClient
{
    [Command("bookmarks")]
    public ValueTask DownloadBookmarksOfUserAsync
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

        if (configSettings.UserId == 0UL)
        {
            Context.Logger.LogError($"{VirtualCodes.BrightRedColor}User Id should be written in appsettings.json{VirtualCodes.NormalizeColor}");
            return ValueTask.CompletedTask;
        }

        var privateOrPublic = isPrivate ? "private" : "public";
        var url = $"https://{ApiHost}/v1/user/bookmarks/illust?user_id={configSettings.UserId}&restrict={privateOrPublic}";
        return DownloadArtworkResponses(addBehaviour, download, url, Context.CancellationToken);
    }
}
