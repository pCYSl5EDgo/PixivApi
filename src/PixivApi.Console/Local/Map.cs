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
        var database = await IOUtility.MessagePackDeserializeAsync<DatabaseFile>(configSettings.DatabaseFilePath, token).ConfigureAwait(false);
        if (database is null)
        {
            if (!System.Console.IsOutputRedirected)
            {
                logger.LogInformation("null");
            }

            return;
        }

        var itemFilter = string.IsNullOrWhiteSpace(configSettings.ArtworkFilterFilePath) ? null : await IOUtility.JsonDeserializeAsync<ArtworkFilter>(configSettings.ArtworkFilterFilePath, token).ConfigureAwait(false);
        var artworks = itemFilter is null
            ? database.ArtworkDictionary.Values
            : await FilterExtensions.CreateEnumerableAsync(finder, database, itemFilter, token).ConfigureAwait(false);

        if (toString)
        {
            switch (artworks)
            {
                case Artwork[]:
                case List<Artwork>:
                    break;
                default:
                    artworks = artworks.ToList();
                    break;
            }

            await Parallel.ForEachAsync(artworks, token, (artwork, token) =>
            {
                if (token.IsCancellationRequested)
                {
                    return ValueTask.FromCanceled(token);
                }

                artwork.Stringify(database.UserDictionary, database.TagSet, database.ToolSet);
                return ValueTask.CompletedTask;
            }).ConfigureAwait(false);
        }

        token.ThrowIfCancellationRequested();
        var first = true;
        if (!System.Console.IsOutputRedirected)
        {
            logger.LogInformation("[");
        }

        foreach (var item in artworks)
        {
            token.ThrowIfCancellationRequested();
            if (System.Console.IsOutputRedirected)
            {
                logger.LogInformation(IOUtility.JsonStringSerialize(item, false));
            }
            else if (first)
            {
                first = false;
                logger.LogInformation($"{IOUtility.JsonStringSerialize(item, true)}");
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

    [Command("map-user")]
    public async ValueTask MapUserAsync(
        [Option(0, $"input {ArgumentDescriptions.DatabaseDescription}")] string input,
        [Option(1, ArgumentDescriptions.FilterDescription)] string? filter = null,
        uint count = uint.MaxValue,
        uint offset = 0
    )
    {
        if (string.IsNullOrWhiteSpace(input))
        {
            return;
        }

        var token = Context.CancellationToken;
        var database = await IOUtility.MessagePackDeserializeAsync<DatabaseFile>(input, token).ConfigureAwait(false);
        if (database is null)
        {
            if (!System.Console.IsOutputRedirected)
            {
                logger.LogInformation("null");
            }

            return;
        }

        IEnumerable<User> users = database.UserDictionary.Values;
        var userFilter = string.IsNullOrWhiteSpace(filter) ? null : await IOUtility.JsonDeserializeAsync<UserFilter>(filter, token).ConfigureAwait(false);
        if (userFilter is not null)
        {
            await userFilter.InitializeAsync(database.UserDictionary, database.TagSet, token).ConfigureAwait(false);
            users = users.Where(userFilter.Filter);
        }

        if (offset > 0)
        {
            users = users.Skip((int)offset);
        }

        if (count < int.MaxValue)
        {
            users = users.Take((int)count);
        }

        token.ThrowIfCancellationRequested();
        var first = true;
        if (!System.Console.IsOutputRedirected)
        {
            logger.LogInformation("[");
        }

        foreach (var item in users)
        {
            token.ThrowIfCancellationRequested();
            if (System.Console.IsOutputRedirected)
            {
                logger.LogInformation(IOUtility.JsonStringSerialize(item, false));
            }
            else if (first)
            {
                first = false;
                logger.LogInformation($"{IOUtility.JsonStringSerialize(item, true)}");
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
