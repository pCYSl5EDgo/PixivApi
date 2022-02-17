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

        await database.OptimizeAsync(token).ConfigureAwait(false);
        await IOUtility.MessagePackSerializeAsync(path, database, FileMode.Create).ConfigureAwait(false);
    }
}
