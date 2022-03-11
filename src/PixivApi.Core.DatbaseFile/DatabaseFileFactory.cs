namespace PixivApi.Core.Local;

public sealed class DatabaseFileFactory : IDatabaseFactory
{
    private readonly string path;
    private DatabaseFile? databaseFile;

    public DatabaseFileFactory(ConfigSettings configSettings)
    {
        path = configSettings.DatabaseFilePath ?? throw new NullReferenceException();
    }

    public async ValueTask<IDatabase> RentAsync(CancellationToken token)
    {
        if (databaseFile is not null)
        {
            return databaseFile;
        }

        var value = await IOUtility.MessagePackDeserializeAsync<DatabaseFile>(path, token).ConfigureAwait(false) ?? throw new NullReferenceException();
        var answer = Interlocked.CompareExchange(ref databaseFile, value, null);
        return answer ?? value;
    }

    public async ValueTask DisposeAsync()
    {
        if (!(databaseFile is { IsChanged: not 0 } local))
        {
            return;
        }

        if (!Console.IsErrorRedirected)
        {
            Console.Error.WriteLine("Start saving to the database file.");
        }

        await IOUtility.MessagePackSerializeAsync(path, local, FileMode.Create).ConfigureAwait(false);
    }
}
