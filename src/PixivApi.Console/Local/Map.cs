namespace PixivApi.Console;

public partial class LocalClient
{
  [Command("map")]
  public async ValueTask MapAsync(
      string? filter = null,
      bool toString = false
  )
  {
    filter ??= configSettings.ArtworkFilterFilePath;
    if (string.IsNullOrWhiteSpace(filter) || string.IsNullOrWhiteSpace(configSettings.DatabaseFilePath))
    {
      return;
    }

    var token = Context.CancellationToken;
    var database = await databaseFactory.RentAsync(token).ConfigureAwait(false);
    try
    {
      var artworkFilter = await filterFactory.CreateAsync(database, new(filter), token).ConfigureAwait(false);
      if (token.IsCancellationRequested)
      {
        return;
      }

      var first = true;
      if (!System.Console.IsOutputRedirected)
      {
        logger.LogInformation("[");
      }

      var collection = artworkFilter is null ? database.EnumerateArtworkAsync(token) : database.FilterAsync(artworkFilter, token);
      await foreach (var item in collection)
      {
        if (token.IsCancellationRequested)
        {
          return;
        }

        if (toString)
        {
          await item.StringifyAsync(database, database, database, token).ConfigureAwait(false);
        }

        if (first)
        {
          first = false;
          logger.LogInformation(IOUtility.JsonStringSerialize(item, true));
        }
        else
        {
          logger.LogInformation($", {IOUtility.JsonStringSerialize(item, true)}");
        }
      }

      if (!System.Console.IsOutputRedirected)
      {
        logger.LogInformation("]");
      }
    }
    finally
    {
      databaseFactory.Return(ref database);
    }
  }
}
