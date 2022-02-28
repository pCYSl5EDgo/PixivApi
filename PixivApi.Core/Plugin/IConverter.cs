using Microsoft.Extensions.Logging;
using PixivApi.Core.Local;

namespace PixivApi.Core;

public interface IConverter : IPlugin
{
    public ValueTask<bool> TryConvertAsync(Artwork artwork, ILogger? logger, CancellationToken cancellationToken);
}
