using PixivApi.Core;
using PixivApi.Core.Local;

namespace PixivApi.Console;

public partial class LocalClient
{
    [Command("count", "")]
    public async ValueTask<int> CountAsync(
        [Option(0, $"input {ArgumentDescriptions.DatabaseDescription}")] string input,
        [Option(1, ArgumentDescriptions.FilterDescription)] string? filter = null
    )
    {
        var token = Context.CancellationToken;
        var artworkItemFilter = string.IsNullOrWhiteSpace(filter) ? null : await IOUtility.JsonDeserializeAsync<ArtworkFilter>(filter, token).ConfigureAwait(false);
        if (string.IsNullOrWhiteSpace(input))
        {
            return -1;
        }

        var count = 0;
        var database = await IOUtility.MessagePackDeserializeAsync<DatabaseFile>(input, token).ConfigureAwait(false);
        if (database is not { Artworks.Length: > 0 })
        {
            goto END;
        }

        if (artworkItemFilter is null)
        {
            count = database.Artworks.Length;
        }
        else
        {
            var parallelOptions = new ParallelOptions()
            {
                CancellationToken = token,
                MaxDegreeOfParallelism = configSettings.MaxParallel,
            };
            count = await ArtworkEnumerable.CountAsync(configSettings, database, artworkItemFilter, parallelOptions).ConfigureAwait(false);
        }

    END:
        logger.LogInformation($"{count}");
        return 0;
    }
}
