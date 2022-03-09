namespace PixivApi.Core.Local;

public interface IToolDatabase
{
    ValueTask<ulong> CountToolAsync(CancellationToken token);

    ValueTask<string?> GetToolAsync(uint id, CancellationToken token);

    ValueTask<uint> RegisterToolAsync(string value, CancellationToken token);
}
