using Microsoft.Extensions.Logging;

namespace PixivApi.Core.Plugin;

public interface IConverter : IPlugin
{
    public ValueTask<bool> TryConvertAsync(FileInfo file, ILogger? logger, CancellationToken cancellationToken);
}
