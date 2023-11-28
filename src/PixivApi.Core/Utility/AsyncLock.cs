namespace PixivApi.Core;

public sealed class AsyncLock : IDisposable
{
  private readonly SemaphoreSlim semaphore = new(1, 1);
  private readonly Task<Releaser> releaser;

  public AsyncLock()
  {
    releaser = Task.FromResult<Releaser>(new(this));
  }

  public void Dispose() => semaphore.Dispose();

  public Task<Releaser> LockAsync(CancellationToken token)
  {
    var wait = semaphore.WaitAsync(token);
    return wait.IsCompleted ?
            releaser :
            wait.ContinueWith(
              continuationFunction: (_, state) => (Releaser)state!,
              releaser.Result,
              token,
              TaskContinuationOptions.ExecuteSynchronously,
              TaskScheduler.Default)!;
  }

  public sealed class Releaser : IDisposable
  {
    private readonly AsyncLock toRelease;

    internal Releaser(AsyncLock toRelease) => this.toRelease = toRelease;

    public void Dispose() => _ = toRelease.semaphore.Release();
  }
}