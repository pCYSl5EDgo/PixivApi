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
            : await FilterExtensions.CreateEnumerableAsync(configSettings, database, itemFilter, token).ConfigureAwait(false);

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
                logger.LogInformation($"[{IOUtility.JsonStringSerialize(item, true)}");
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
