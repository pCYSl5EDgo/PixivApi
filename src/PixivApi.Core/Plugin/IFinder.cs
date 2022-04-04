using PixivApi.Core.Local;

namespace PixivApi.Core.Plugin;

public interface IFinder : IPlugin
{
    FileInfo Find(ulong id, FileExtensionKind extensionKind);
}

public interface IFinderWithIndex : IPlugin
{
    FileInfo Find(ulong id, FileExtensionKind extensionKind, uint index);
}
