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
            : await ArtworkEnumerable.CreateAsync(configSettings, database, itemFilter, new() { CancellationToken = token, MaxDegreeOfParallelism = configSettings.MaxParallel, }).ConfigureAwait(false);

        if (toString)
        {
            if (artworks is not Artwork[])
            {
                artworks = artworks.ToArray();
            }

            await Parallel.ForEachAsync(artworks, token, (artwork, token) =>
            {
                token.ThrowIfCancellationRequested();
                artwork.Stringify(database.UserDictionary, database.TagSet, database.ToolSet);
                return ValueTask.CompletedTask;
            }).ConfigureAwait(false);
        }

        if (pipe)
        {
            foreach (var item in artworks)
            {
                logger.LogInformation(IOUtility.JsonStringSerialize(item, false));
            }
        }
        else
        {
            logger.LogInformation(IOUtility.JsonStringSerialize(artworks, true));
        }

    END:
        return 0;
    }
}
