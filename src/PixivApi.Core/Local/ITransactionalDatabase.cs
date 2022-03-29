namespace PixivApi.Core.Local;

public interface ITransactionalDatabase : IDatabase
{
    ValueTask BeginTransactionAsync(CancellationToken token);

    void RollbackTransaction();

    void EndTransaction();
}
