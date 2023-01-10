namespace PixivApi.Core.Plugin;

public interface IPlugin : IAsyncDisposable
{
    static virtual Task<IPlugin?> CreateAsync(string dllPath, ConfigSettings configSettings, IServiceProvider provider, CancellationToken cancellationToken) => throw new NotImplementedException(nameof(IPlugin.CreateAsync));
}
