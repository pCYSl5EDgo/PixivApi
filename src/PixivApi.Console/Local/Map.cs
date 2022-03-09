namespace PixivApi.Console;

public partial class LocalClient
{
    [Command("map")]
    public async ValueTask MapAsync(
        bool toString = false
    )
    {
        if (string.IsNullOrWhiteSpace(configSettings.DatabaseFilePath))
        {
            return;
        }

        var token = Context.CancellationToken;
        var database = await databaseFactory.CreateAsync(token).ConfigureAwait(false);
        if (database is null)
        {
            if (!System.Console.IsOutputRedirected)
            {
                logger.LogInformation("null");
            }

            return;
        }

        var artworkFilter = string.IsNullOrWhiteSpace(configSettings.ArtworkFilterFilePath) ? null : await filterFactory.CreateAsync(new(configSettings.ArtworkFilterFilePath), token).ConfigureAwait(false);
        if (token.IsCancellationRequested)
        {
            return;
        }

        var first = true;
        if (!System.Console.IsOutputRedirected)
        {
            logger.LogInformation("[");
        }

        var collection = artworkFilter is null ? database.EnumerableArtworkAsync(token) : database.ArtworkFilterAsync(artworkFilter, token);
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
}
