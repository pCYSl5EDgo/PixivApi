namespace PixivApi.Core;

public interface ISingleUpdater<T>
{
    ref Task<T>? GetTask { get; }

    Task<T> UpdateAsync(CancellationToken token);

    TimeSpan WaitInterval { get; }

    TimeSpan LoopInterval { get; }
}

public static class SingleUpdateUtility
{
    public static async Task<T> GetAsync<T>(ISingleUpdater<T> singleUpdater, CancellationToken token)
    {
        var current = singleUpdater.GetTask;
        if (current is not null && ReferenceEquals(Interlocked.CompareExchange(ref singleUpdater.GetTask, null, current), current))
        {
            await Task.Delay(singleUpdater.WaitInterval, token).ConfigureAwait(false);
            singleUpdater.GetTask = singleUpdater.UpdateAsync(token);
        }
        else
        {
            var interval = singleUpdater.LoopInterval;
            while (singleUpdater.GetTask is null)
            {
                await Task.Delay(interval, token).ConfigureAwait(false);
            }
        }

        return await singleUpdater.GetTask.ConfigureAwait(false);
    }
}
