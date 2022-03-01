using PixivApi.Core.Local;

namespace PixivApi.Core;

public interface IFinder : IPlugin
{
    FileInfo Find(Artwork artwork);
}

public interface IFinderWithIndex : IPlugin
{
    FileInfo Find(Artwork artwork, uint index);
}
