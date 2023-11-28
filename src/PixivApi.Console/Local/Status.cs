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

    var database = await databaseFactory.RentAsync(token).ConfigureAwait(false);
    try
    {
      var artworkCount = await database.CountArtworkAsync(token).ConfigureAwait(false);
      var userCount = await database.CountUserAsync(token).ConfigureAwait(false);
      var tagCount = await database.CountTagAsync(token).ConfigureAwait(false);
      var toolCount = await database.CountToolAsync(token).ConfigureAwait(false);
      var rankingCount = await database.CountRankingAsync(token).ConfigureAwait(false);
      logger.LogInformation($"Version: {database.Version} Artwork: {artworkCount} User: {userCount}\nTag: {tagCount} Tool: {toolCount} Ranking: {rankingCount}");
    }
    finally
    {
      databaseFactory.Return(ref database);
    }
  }
}
