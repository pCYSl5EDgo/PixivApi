namespace PixivApi.Core;

public interface IPlugin : IAsyncDisposable
{
    static abstract ValueTask<IPlugin?> CreateAsync(string dllPath, ConfigSettings configSettings, CancellationToken cancellationToken);

    static abstract bool SupportsMultithread();
}
