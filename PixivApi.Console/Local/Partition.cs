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

        var parallelOptions = new ParallelOptions()
        {
            CancellationToken = token,
            MaxDegreeOfParallelism = configSettings.MaxParallel,
        };
        await artworkFilter.InitializeAsync(configSettings, database.UserDictionary, database.TagSet, parallelOptions).ConfigureAwait(false);

        ConcurrentBag<int> tBag = new();
        ConcurrentBag<int> fBag = new();
        await Parallel.ForEachAsync(Enumerable.Range(0, database.Artworks.Length), parallelOptions, (index, token) =>
        {
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
            token.ThrowIfCancellationRequested();
            trues[index++] = database.Artworks[item];
        }

        var falses = new Artwork[fBag.Count];
        index = 0;
        foreach (var item in fBag)
        {
            token.ThrowIfCancellationRequested();
            falses[index++] = database.Artworks[item];
        }

        var trueDatabase = new DatabaseFile(0, 0, trues, database.UserDictionary, database.TagSet, database.ToolSet);
        token.ThrowIfCancellationRequested();
        await IOUtility.MessagePackSerializeAsync(path + ".true", trueDatabase, FileMode.CreateNew).ConfigureAwait(false);
        var falseDatabase = new DatabaseFile(0, 0, falses, database.UserDictionary, database.TagSet, database.ToolSet);
        token.ThrowIfCancellationRequested();
        await IOUtility.MessagePackSerializeAsync(path + ".false", falseDatabase, FileMode.CreateNew).ConfigureAwait(false);

        logger.LogInformation($"True: {trues.Length} False: {falses.Length}");
    }
}
