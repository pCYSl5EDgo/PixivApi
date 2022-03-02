namespace PixivApi.Core;

public interface ISingleUpdater<T>
{
    ref Task<T>? GetTask { get; }

    Task<T> UpdateAsync(CancellationToken token);
}

public static class SingleUpdateUtility
{
    public static async Task<T> GetAsync<T>(ISingleUpdater<T> singleUpdater, CancellationToken token)
    {
        var current = singleUpdater.GetTask;
        if (current is not null && ReferenceEquals(Interlocked.CompareExchange(ref singleUpdater.GetTask, null, current), current))
        {
            singleUpdater.GetTask = singleUpdater.UpdateAsync(token);
        }
        else
        {
            while (singleUpdater.GetTask is null)
            {
                await Task.Delay(TimeSpan.FromSeconds(1d), token).ConfigureAwait(false);
            }
        }

        return await singleUpdater.GetTask.ConfigureAwait(false);
    }
}
