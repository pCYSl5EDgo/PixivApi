namespace PixivApi.Core.Plugin;

public interface IPlugin : IAsyncDisposable
{
    static abstract Task<IPlugin?> CreateAsync(string dllPath, ConfigSettings configSettings, CancellationToken cancellationToken);
}
