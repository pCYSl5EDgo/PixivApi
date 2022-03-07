namespace PixivApi.Console;

public partial class LocalClient
{
    [Command("status", "")]
    public async ValueTask StatusAsync()
    {
        var token = Context.CancellationToken;
        if (token.IsCancellationRequested || string.IsNullOrWhiteSpace(configSettings.DatabaseFilePath))
        {
            return;
        }

        var database = await IOUtility.MessagePackDeserializeAsync<DatabaseFile>(configSettings.DatabaseFilePath, token).ConfigureAwait(false);
        if (database is null)
        {
            return;
        }

        logger.LogInformation($"Artwork: {database.ArtworkDictionary.Count} User: {database.UserDictionary.Count}\nTag: {database.TagSet.Reverses.Count} Tool: {database.ToolSet.Reverses.Count}");
    }
}
