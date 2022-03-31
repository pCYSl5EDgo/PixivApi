namespace PixivApi.Core.Local;

public interface ITagDatabase
{
    IAsyncEnumerable<(string, uint)> EnumerateTagAsync(CancellationToken token);

    ValueTask<ulong> CountTagAsync(CancellationToken token);

    ValueTask<string?> GetTagAsync(uint id, CancellationToken token);

    ValueTask<uint?> FindTagAsync(string key, CancellationToken token);

    ValueTask<uint> RegisterTagAsync(string value, CancellationToken token);

    IAsyncEnumerable<uint> EnumeratePartialMatchTagAsync(string key, CancellationToken token);

    bool CanRegisterParallel => false;
}
