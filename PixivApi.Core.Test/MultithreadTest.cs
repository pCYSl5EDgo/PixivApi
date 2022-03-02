using Xunit;

namespace PixivApi.Core.Test;

public class MultithreadTest
{
    private readonly UpdateMachine machine = new();

    [Fact]
    public async ValueTask MultithreadTestAsync()
    {
        var texts = new string[1000];
        var tasks = new Task[texts.Length];
        for (var i = 0; i < texts.Length; i++)
        {
            var id = i;
            tasks[id] = Task.Run(async () =>
            {
                await Task.Delay(TimeSpan.FromSeconds(1d), CancellationToken.None).ConfigureAwait(false);
                texts[id] = await SingleUpdateUtility.GetAsync(machine, CancellationToken.None).ConfigureAwait(false);
            }, CancellationToken.None);
        }

        await Task.WhenAll(tasks).ConfigureAwait(false);
        for (var i = 0; i < texts.Length; i++)
        {
            Assert.Equal("1", texts[i]);
        }
    }
}

internal sealed class UpdateMachine : ISingleUpdater<string>
{
    private static uint count = 0;

    private Task<string>? task = Task.FromResult("0");

    public ref Task<string>? GetTask => ref task;

    public Task<string> UpdateAsync(CancellationToken token) => Task.Delay(TimeSpan.FromSeconds(10), token).ContinueWith(_ => Interlocked.Increment(ref count).ToString());

    public TimeSpan WaitInterval => TimeSpan.FromSeconds(1);
}