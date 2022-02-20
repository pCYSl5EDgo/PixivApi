using PixivApi.Core.Local;

namespace PixivApi.Console;

public partial class LocalClient
{
    [Command("database-version-update")]
    public async ValueTask DatabaseVersionUpdateAsync(
        [Option(0, ArgumentDescriptions.DatabaseDescription)] string path
    )
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return;
        }

        var database = await IOUtility.MessagePackDeserializeAsync<DatabaseFile>(path, Context.CancellationToken).ConfigureAwait(false);
        if (database == null)
        {
            return;
        }

        await IOUtility.MessagePackSerializeAsync(path, database, FileMode.Create).ConfigureAwait(false);
    }

    public async ValueTask DatabseVersion([Option(0, ArgumentDescriptions.DatabaseDescription)] string? path)
    {
        if (!string.IsNullOrWhiteSpace(path))
        {
            var database = await IOUtility.MessagePackDeserializeAsync<DatabaseFile>(path, Context.CancellationToken).ConfigureAwait(false);
            if (database is not null)
            {
                logger.LogInformation($"{database.MajorVersion}.{database.MinorVersion}");
                return;
            }
        }
        
        logger.LogInformation("0.0");
    }
}
