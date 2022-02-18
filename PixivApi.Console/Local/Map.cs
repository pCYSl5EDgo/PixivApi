using PixivApi.Core.Local.Filter;
using PixivApi.Core.Local;

namespace PixivApi.Console;

partial class LocalClient
{
    [Command("map")]
    public async ValueTask<int> MapAsync(
        [Option(0, $"input {IOUtility.DatabaseDescription}")] string input,
        [Option(1, IOUtility.FilterDescription)] string filter,
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
            : await ArtworkEnumerable.CreateAsync(database, itemFilter, new() { CancellationToken = token, MaxDegreeOfParallelism = configSettings.MaxParallel, }).ConfigureAwait(false);

        if (pipe)
        {
            foreach (var item in artworks)
            {
                logger.LogInformation(IOUtility.JsonStringSerialize(item));
            }
        }
        else
        {
            logger.LogInformation(IOUtility.JsonStringSerialize(artworks));
        }

    END:
        return 0;
    }
}
