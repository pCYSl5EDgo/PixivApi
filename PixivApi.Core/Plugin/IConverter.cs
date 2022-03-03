using Microsoft.Extensions.Logging;
using PixivApi.Core.Local;

namespace PixivApi.Core.Plugin;

public interface IConverter : IPlugin
{
    public ValueTask<bool> TryConvertAsync(Artwork artwork, ILogger? logger, CancellationToken cancellationToken);

    public void DeleteUnneccessaryOriginal(Artwork artwork, ILogger? logger);
}
