using PixivApi.Core.Local;
using PixivApi.Core.Network;
using SQLitePCL;
using System.Diagnostics.CodeAnalysis;
using static SQLitePCL.raw;

namespace PixivApi.Core.SqliteDatabase;

internal sealed class Database : IExtenededDatabase, IDisposable
{
    private readonly sqlite3 database;

    private sqlite3_stmt? countArtworkStatement;
    private sqlite3_stmt? countArtworkFilterStatement;
    private sqlite3_stmt? countUserStatement;
    private sqlite3_stmt? countTagStatement;
    private sqlite3_stmt? countToolStatement;
    private sqlite3_stmt? countRankingStatement;

    public Database(string path)
    {
        var code = sqlite3_open_v2(path, out database, SQLITE_OPEN_READWRITE | SQLITE_OPEN_CREATE | SQLITE_OPEN_NOMUTEX, "");
        if (code != SQLITE_OK)
        {
            throw new Exception($"Error Code: {code}");
        }
    }

    private Version? version;
    public Version Version
    {
        get
        {
            int major = 0, minor = 0;
            if (version is null)
            {
                sqlite3_stmt? statement = null;
                try
                {
                    var code = sqlite3_prepare_v2(database, "SELECT (`Major`, `Minor`) FROM `InfoTable` ORDER BY `Major` DESC, `Minor` DESC LIMIT 1;", out statement);
                    code = sqlite3_step(statement);
                    if (code == SQLITE_OK)
                    {
                        major = sqlite3_column_int(statement, 0);
                        minor = sqlite3_column_int(statement, 1);
                    }
                }
                finally
                {
                    statement?.manual_close();
                    version = new(major, minor);
                }
            }

            return version;
        }
    }

    public ValueTask<bool> AddOrUpdateAsync(ulong id, Func<CancellationToken, ValueTask<Artwork>> add, Func<Artwork, CancellationToken, ValueTask> update, CancellationToken token)
    {
        throw new NotImplementedException();
    }

    public ValueTask<bool> AddOrUpdateAsync(ulong id, Func<CancellationToken, ValueTask<User>> add, Func<User, CancellationToken, ValueTask> update, CancellationToken token)
    {
        throw new NotImplementedException();
    }

    public ValueTask AddOrUpdateRankingAsync(DateOnly date, RankingKind kind, ulong[] values, CancellationToken token)
    {
        throw new NotImplementedException();
    }

    public ValueTask<bool> ArtworkAddOrUpdateAsync(ArtworkResponseContent source, CancellationToken token)
    {
        throw new NotImplementedException();
    }

    private ulong SimpleCount([NotNull] ref sqlite3_stmt? stmt, string table)
    {
        int code;
        if (stmt is null)
        {
            code = sqlite3_prepare_v3(database, $"SELECT COUNT(*) FROM `{table}Table`", SQLITE_PREPARE_PERSISTENT, out stmt);
        }
        else
        {
            code = sqlite3_reset(stmt);
        }

        code = sqlite3_step(stmt);
        return (ulong)sqlite3_column_int64(stmt, 0);
    }

    public ValueTask<ulong> CountArtworkAsync(CancellationToken token) => ValueTask.FromResult(SimpleCount(ref countArtworkStatement, "ArtworkConcrete"));

    public ValueTask<ulong> CountArtworkAsync(ArtworkFilter filter, CancellationToken token)
    {
        throw new NotImplementedException();
    }

    public ValueTask<ulong> CountRankingAsync(CancellationToken token) => ValueTask.FromResult(SimpleCount(ref countRankingStatement, "Ranking"));

    public ValueTask<ulong> CountTagAsync(CancellationToken token) => ValueTask.FromResult(SimpleCount(ref countTagStatement, "Tag"));

    public ValueTask<ulong> CountToolAsync(CancellationToken token) => ValueTask.FromResult(SimpleCount(ref countToolStatement, "Tool"));

    public ValueTask<ulong> CountUserAsync(CancellationToken token) => ValueTask.FromResult(SimpleCount(ref countUserStatement, "User"));

    public void Dispose()
    {
        _ = sqlite3_close_v2(database);
        countArtworkStatement?.manual_close();
        countArtworkFilterStatement?.manual_close();
        countUserStatement?.manual_close();
        countTagStatement?.manual_close();
        countToolStatement?.manual_close();
        countRankingStatement?.manual_close();
    }

    public IAsyncEnumerable<Artwork> EnumerableArtworkAsync(CancellationToken token)
    {
        throw new NotImplementedException();
    }

    public IAsyncEnumerable<User> EnumerableUserAsync(CancellationToken token)
    {
        throw new NotImplementedException();
    }

    public IAsyncEnumerable<uint> EnumeratePartialMatchAsync(string key, CancellationToken token)
    {
        throw new NotImplementedException();
    }

    public IAsyncEnumerable<Artwork> FilterAsync(ArtworkFilter filter, CancellationToken token)
    {
        throw new NotImplementedException();
    }

    public IAsyncEnumerable<User> FilterAsync(IFilter<User> filter, CancellationToken token)
    {
        throw new NotImplementedException();
    }

    public ValueTask<uint?> FindTagAsync(string key, CancellationToken token)
    {
        throw new NotImplementedException();
    }

    public ValueTask<Artwork?> GetArtworkAsync(ulong id, CancellationToken token)
    {
        throw new NotImplementedException();
    }

    public ValueTask<ulong[]?> GetRankingAsync(DateOnly date, RankingKind kind, CancellationToken token)
    {
        throw new NotImplementedException();
    }

    public ValueTask<string?> GetTagAsync(uint id, CancellationToken token)
    {
        throw new NotImplementedException();
    }

    public ValueTask<string?> GetToolAsync(uint id, CancellationToken token)
    {
        throw new NotImplementedException();
    }

    public ValueTask<User?> GetUserAsync(ulong id, CancellationToken token)
    {
        throw new NotImplementedException();
    }

    public ValueTask<(ulong Add, ulong Update)> RankingAddOrUpdateAsync(DateOnly date, RankingKind rankingKind, IEnumerable<ArtworkResponseContent> sources, CancellationToken token)
    {
        throw new NotImplementedException();
    }

    public ValueTask<uint> RegisterTagAsync(string value, CancellationToken token)
    {
        throw new NotImplementedException();
    }

    public ValueTask<uint> RegisterToolAsync(string value, CancellationToken token)
    {
        throw new NotImplementedException();
    }

    public ValueTask<bool> UserAddOrUpdateAsync(UserDetailResponseData source, CancellationToken token)
    {
        throw new NotImplementedException();
    }

    public ValueTask<bool> UserAddOrUpdateAsync(UserPreviewResponseContent source, CancellationToken token)
    {
        throw new NotImplementedException();
    }
}
