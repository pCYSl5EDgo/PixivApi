namespace PixivApi.Core.Local;

public interface ITransactionalDatabase : IDatabase
{
    ValueTask BeginTransactionAsync(CancellationToken token);

    ValueTask RollbackTransactionAsync(CancellationToken token);

    ValueTask EndTransactionAsync(CancellationToken token);
}
