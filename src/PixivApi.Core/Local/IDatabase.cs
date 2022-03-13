global using DatabaseAddArtworkFunc = System.Func<System.Threading.CancellationToken, System.Threading.Tasks.ValueTask<PixivApi.Core.Local.Artwork>>;
global using DatabaseAddUserFunc = System.Func<System.Threading.CancellationToken, System.Threading.Tasks.ValueTask<PixivApi.Core.Local.User>>;
global using DatabaseUpdateArtworkFunc = System.Func<PixivApi.Core.Local.Artwork, System.Threading.CancellationToken, System.Threading.Tasks.ValueTask>;
global using DatabaseUpdateUserFunc = System.Func<PixivApi.Core.Local.User, System.Threading.CancellationToken, System.Threading.Tasks.ValueTask>;

namespace PixivApi.Core.Local;

public interface IDatabase : IArtworkDatabase, IUserDatabase, ITagDatabase, IToolDatabase, IRankingDatabase
{
    Version Version { get; }
}
