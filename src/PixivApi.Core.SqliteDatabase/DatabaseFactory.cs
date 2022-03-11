using PixivApi.Core.Local;
using System.Diagnostics.CodeAnalysis;

namespace PixivApi.Core.SqliteDatabase;

public sealed class DatabaseFactory : IDatabaseFactory
{
    public ValueTask<IDatabase> RentAsync(CancellationToken token)
    {
        throw new NotImplementedException();
    }

    public ValueTask DisposeAsync()
    {
        throw new NotImplementedException();
    }

    public void Return([MaybeNull] ref IDatabase? database)
    {
        throw new NotImplementedException();
    }
}
