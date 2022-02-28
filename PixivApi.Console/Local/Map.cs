using PixivApi.Core;
using PixivApi.Core.Local;

namespace PixivApi.Console;

public partial class LocalClient
{
    [Command("map")]
    public async ValueTask<int> MapAsync(
        [Option(0, $"input {ArgumentDescriptions.DatabaseDescription}")] string input,
        [Option(1, ArgumentDescriptions.FilterDescription)] string filter,
        bool toString = false,
        bool pipe = false
    )
    {
        if (string.IsNullOrWhiteSpace(input))
        {
            goto END;
        }

        var token = Context.CancellationToken;
        var database = await IOUtility.MessagePackDeserializeAsync<DatabaseFile>(input, token).ConfigureAwait(false);
        if (database is null)
        {
            if (!pipe)
            {
                logger.LogInformation("null");
            }
            goto END;
        }

        var itemFilter = await IOUtility.JsonDeserializeAsync<ArtworkFilter>(filter, token).ConfigureAwait(false);
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
        if (!pipe)
        {
            logger.LogInformation("[");
        }

        foreach (var item in artworks)
        {
            token.ThrowIfCancellationRequested();
            if (pipe)
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

        if (!pipe)
        {
            logger.LogInformation("]");
        }

    END:
        return 0;
    }

    [Command("map-user")]
    public async ValueTask<int> MapUserAsync(
        [Option(0, $"input {ArgumentDescriptions.DatabaseDescription}")] string input,
        [Option(1, ArgumentDescriptions.FilterDescription)] string? filter = null,
        uint count = uint.MaxValue,
        uint offset = 0,
        bool pipe = false
    )
    {
        if (string.IsNullOrWhiteSpace(input))
        {
            goto END;
        }

        var token = Context.CancellationToken;
        var database = await IOUtility.MessagePackDeserializeAsync<DatabaseFile>(input, token).ConfigureAwait(false);
        if (database is null)
        {
            if (!pipe)
            {
                logger.LogInformation("null");
            }
            goto END;
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
        if (!pipe)
        {
            logger.LogInformation("[");
        }

        foreach (var item in users)
        {
            token.ThrowIfCancellationRequested();
            if (pipe)
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

        if (!pipe)
        {
            logger.LogInformation("]");
        }

    END:
        return 0;
    }
}
