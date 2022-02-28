using PixivApi.Core.Local;

namespace PixivApi.Core;

public interface IFinder : IPlugin
{
    bool Find(Artwork artwork);
}

public interface IFinderWithIndex : IPlugin
{
    bool Find(Artwork artwork, uint index);
}
