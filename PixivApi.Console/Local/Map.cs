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
            ? database.Artworks
            : await FilterExtensions.CreateEnumerableAsync(configSettings, database, itemFilter, token).ConfigureAwait(false);

        if (toString)
        {
            if (artworks is not Artwork[])
            {
                artworks = artworks.ToArray();
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
        var enumerator = artworks.GetEnumerator();
        if (!enumerator.MoveNext())
        {
            if (!pipe)
            {
                logger.LogInformation("[]");
            }

            goto END;
        }

        if (!pipe)
        {
            logger.LogInformation("[");
        }

        logger.LogInformation(IOUtility.JsonStringSerialize(enumerator.Current, !pipe));
        while (enumerator.MoveNext())
        {
            token.ThrowIfCancellationRequested();
            if (pipe)
            {
                logger.LogInformation(IOUtility.JsonStringSerialize(enumerator.Current, !pipe));
            }
            else
            {
                logger.LogInformation($", {IOUtility.JsonStringSerialize(enumerator.Current, !pipe)}");
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
