namespace PixivApi.Core.Local;

public interface IToolDatabase
{
    IAsyncEnumerable<(string, uint)> EnumerateToolAsync(CancellationToken token);

    ValueTask<ulong> CountToolAsync(CancellationToken token);

    ValueTask<string?> GetToolAsync(uint id, CancellationToken token);

    ValueTask<uint?> FindToolAsync(string key, CancellationToken token);

    ValueTask<uint> RegisterToolAsync(string value, CancellationToken token);

    bool CanRegisterParallel => false;
}
