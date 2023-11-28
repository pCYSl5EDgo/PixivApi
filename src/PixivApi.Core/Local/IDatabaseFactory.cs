namespace PixivApi.Core.Local;

public interface IDatabaseFactory : IAsyncDisposable
{
  ValueTask<IDatabase> RentAsync(CancellationToken token);

  void Return([MaybeNull] ref IDatabase database) => database = null;
}
