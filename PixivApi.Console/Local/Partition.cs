using PixivApi.Core;
using PixivApi.Core.Local;

namespace PixivApi.Console;

public partial class LocalClient
{
    [Command("partition")]
    public async ValueTask PartitionAsync(
        [Option(0, ArgumentDescriptions.DatabaseDescription)] string path,
        [Option(1, ArgumentDescriptions.FilterDescription)] string filter
    )
    {
        var token = Context.CancellationToken;
        var database = await IOUtility.MessagePackDeserializeAsync<DatabaseFile>(path, token).ConfigureAwait(false);
        if (database is null)
        {
            return;
        }

        var artworkFilter = await IOUtility.JsonDeserializeAsync<ArtworkFilter>(filter, token).ConfigureAwait(false);
        if (artworkFilter is null)
        {
            return;
        }

        await artworkFilter.InitializeAsync(configSettings, database.UserDictionary, database.TagSet, token).ConfigureAwait(false);

        ConcurrentBag<int> tBag = new();
        ConcurrentBag<int> fBag = new();
        await Parallel.ForEachAsync(Enumerable.Range(0, database.Artworks.Length), token, (index, token) =>
        {
            if (token.IsCancellationRequested)
            {
                return ValueTask.FromCanceled(token);
            }

            var artwork = database.Artworks[index];
            (artworkFilter.Filter(artwork) ? tBag : fBag).Add(index);
            return ValueTask.CompletedTask;
        }).ConfigureAwait(false);

        if (tBag.IsEmpty || fBag.IsEmpty)
        {
            return;
        }

        var trues = new Artwork[tBag.Count];
        var index = 0;
        foreach (var item in tBag)
        {
            if (token.IsCancellationRequested)
            {
                return;
            }

            trues[index++] = database.Artworks[item];
        }

        var falses = new Artwork[fBag.Count];
        index = 0;
        foreach (var item in fBag)
        {
            if (token.IsCancellationRequested)
            {
                return;
            }

            falses[index++] = database.Artworks[item];
        }

        var trueDatabase = new DatabaseFile(0, 0, trues, database.UserDictionary, database.TagSet, database.ToolSet);
        if (token.IsCancellationRequested)
        {
            return;
        }

        await IOUtility.MessagePackSerializeAsync(path + ".true", trueDatabase, FileMode.CreateNew).ConfigureAwait(false);
        var falseDatabase = new DatabaseFile(0, 0, falses, database.UserDictionary, database.TagSet, database.ToolSet);
        if (token.IsCancellationRequested)
        {
            return;
        }

        await IOUtility.MessagePackSerializeAsync(path + ".false", falseDatabase, FileMode.CreateNew).ConfigureAwait(false);

        logger.LogInformation($"True: {trues.Length} False: {falses.Length}");
    }
}
