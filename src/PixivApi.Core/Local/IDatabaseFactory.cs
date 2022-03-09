namespace PixivApi.Core.Local;

public interface IDatabaseFactory : IAsyncDisposable
{
    ValueTask<IDatabase> CreateAsync(CancellationToken token);
}
