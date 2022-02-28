namespace PixivApi.Core;

public interface IPlugin : IAsyncDisposable
{
    static abstract Task<IPlugin?> CreateAsync(string dllPath, ConfigSettings configSettings, CancellationToken cancellationToken);
}
