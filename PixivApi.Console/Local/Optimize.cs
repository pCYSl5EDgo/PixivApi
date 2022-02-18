using PixivApi.Core.Local;

namespace PixivApi.Console;

partial class LocalClient
{
    [Command("optimize")]
    public async ValueTask OptimizeAsync(
        [Option(0, IOUtility.ArtworkDatabaseDescription)] string path
    )
    {
        var token = Context.CancellationToken;
        var database = await IOUtility.MessagePackDeserializeAsync<DatabaseFile>(path, token).ConfigureAwait(false);
        if (database is null)
        {
            return;
        }

        var parallelOptions = new ParallelOptions()
        {
            CancellationToken = token,
            MaxDegreeOfParallelism = configSettings.MaxParallel,
        };
        await database.OptimizeAsync(parallelOptions).ConfigureAwait(false);
        await IOUtility.MessagePackSerializeAsync(path, database, FileMode.Create).ConfigureAwait(false);
    }
}
