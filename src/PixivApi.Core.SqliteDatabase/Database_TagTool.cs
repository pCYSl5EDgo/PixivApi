namespace PixivApi.Core.SqliteDatabase;

internal sealed partial class Database
{
    private sqlite3_stmt? getTagStatement;
    private sqlite3_stmt? getToolStatement;
    private sqlite3_stmt? registerTagStatement;
    private sqlite3_stmt? registerToolStatement;
    private sqlite3_stmt? enumerateTagStatement;
    private sqlite3_stmt? enumerateToolStatement;
    private sqlite3_stmt? selectIdFromTagTextTableMatch;
    private sqlite3_stmt? selectIdFromTagTextTableLike;
    private sqlite3_stmt? findTagStatement;
    private sqlite3_stmt? findToolStatement;
    private sqlite3_stmt? deleteTagsOfArtworkStatement;
    private sqlite3_stmt? deleteTagsOfUserStatement;
    private sqlite3_stmt? deleteToolsOfArtworkStatement;

    [StringLiteral.Utf8("SELECT \"Value\" FROM \"TagTable\" WHERE \"Id\" = ?")] private static partial ReadOnlySpan<byte> Literal_SelectValueFromTagTableWhereId();
    [StringLiteral.Utf8("SELECT \"Value\" FROM \"ToolTable\" WHERE \"Id\" = ?")] private static partial ReadOnlySpan<byte> Literal_SelectValueFromToolTableWhereId();

    private async ValueTask<string?> GetTagOrToolAsync(sqlite3_stmt statement, uint id, CancellationToken token)
    {
        Bind(statement, 1, id);
        try
        {
            int code;
            while ((code = Step(statement)) == SQLITE_BUSY)
            {
                await Task.Delay(TimeSpan.FromSeconds(1d), token).ConfigureAwait(false);
            }

            if (code == SQLITE_ROW)
            {
                return CStr(statement, 0);
            }
            else
            {
                return null;
            }
        }
        finally
        {
            Reset(statement);
        }
    }

    public ValueTask<string?> GetTagAsync(uint id, CancellationToken token) => GetTagOrToolAsync(getTagStatement ??= Prepare(Literal_SelectValueFromTagTableWhereId(), true, out _), id, token);

    public ValueTask<string?> GetToolAsync(uint id, CancellationToken token) => GetTagOrToolAsync(getToolStatement ??= Prepare(Literal_SelectValueFromToolTableWhereId(), true, out _), id, token);

    [StringLiteral.Utf8("INSERT INTO \"TagTable\" (\"Value\") VALUES (?) ON CONFLICT (\"Value\") DO NOTHING RETURNING \"Id\"")] private static partial ReadOnlySpan<byte> Literal_InsertIntoTagTableReturningId();
    [StringLiteral.Utf8("INSERT INTO \"ToolTable\" (\"Value\") VALUES (?) ON CONFLICT (\"Value\") DO NOTHING RETURNING \"Id\"")] private static partial ReadOnlySpan<byte> Literal_InsertIntoToolTableReturningId();

    private async ValueTask<uint> RegisterTagOrToolAsync(sqlite3_stmt statement, string value, CancellationToken token)
    {
        Bind(statement, 1, value);
        try
        {
            int code;
            while ((code = Step(statement)) == SQLITE_BUSY)
            {
                await Task.Delay(TimeSpan.FromSeconds(1d), token).ConfigureAwait(false);
            }

            return code == SQLITE_ROW ? CU32(statement, 0) : 0;
        }
        finally
        {
            Reset(statement);
        }
    }

    public ValueTask<uint> RegisterTagAsync(string value, CancellationToken token) => RegisterTagOrToolAsync(registerTagStatement ??= Prepare(Literal_InsertIntoTagTableReturningId(), true, out _), value, token);

    public ValueTask<uint> RegisterToolAsync(string value, CancellationToken token) => RegisterTagOrToolAsync(registerToolStatement ??= Prepare(Literal_InsertIntoToolTableReturningId(), true, out _), value, token);

    [StringLiteral.Utf8("SELECT \"Value\", \"Id\" FROM \"TagTable\"")]
    private static partial ReadOnlySpan<byte> Literal_SelectValueId_FromTagTable();

    [StringLiteral.Utf8("SELECT \"Value\", \"Id\" FROM \"ToolTable\"")]
    private static partial ReadOnlySpan<byte> Literal_SelectValueId_FromToolTable();

    private async IAsyncEnumerable<(string, uint)> EnumerateTagToolAsync(sqlite3_stmt statement, [EnumeratorCancellation] CancellationToken token)
    {
        try
        {
            int code;
            do
            {
                code = Step(statement);
                if (code == SQLITE_BUSY)
                {
                    await Task.Delay(TimeSpan.FromSeconds(1d), token).ConfigureAwait(false);
                    continue;
                }

                if (code == SQLITE_ROW)
                {
                    yield return (CStr(statement, 0)!, CU32(statement, 1));
                    continue;
                }

                if (code == SQLITE_DONE)
                {
                    break;
                }

                throw new Exception(code.ToString());
            } while (true);
        }
        finally
        {
            Reset(statement);
        }
    }

    public IAsyncEnumerable<(string, uint)> EnumerateTagAsync(CancellationToken token) => EnumerateTagToolAsync(enumerateTagStatement ??= Prepare(Literal_SelectValueId_FromTagTable(), true, out _), token);

    public IAsyncEnumerable<(string, uint)> EnumerateToolAsync(CancellationToken token) => EnumerateTagToolAsync(enumerateToolStatement ??= Prepare(Literal_SelectValueId_FromToolTable(), true, out _), token);

    [StringLiteral.Utf8("SELECT \"Id\" FROM \"TagTextTable\" (?)")]
    private static partial ReadOnlySpan<byte> Literal_SelectId_FromTagTextTable_Match();

    [StringLiteral.Utf8("SELECT \"Id\" FROM \"TagTable\" WHERE \"Value\" LIKE ('%' || ? || '%')")]
    private static partial ReadOnlySpan<byte> Literal_SelectId_FromTagTable_Like();

    public async IAsyncEnumerable<uint> EnumeratePartialMatchTagAsync(string key, [EnumeratorCancellation] CancellationToken token)
    {
        if (key.Length == 0)
        {
            yield break;
        }

        sqlite3_stmt statement;
        if (key.Length >= 3)
        {
            statement = selectIdFromTagTextTableMatch ??= Prepare(Literal_SelectId_FromTagTextTable_Match(), true, out _);
        }
        else
        {
            statement = selectIdFromTagTextTableLike ??= Prepare(Literal_SelectId_FromTagTable_Like(), true, out _);
        }

        Bind(statement, 1, key);
        try
        {
            do
            {
                if (token.IsCancellationRequested)
                {
                    yield break;
                }

                var code = Step(statement);
                if (code == SQLITE_BUSY)
                {
                    await Task.Delay(TimeSpan.FromSeconds(1d), token).ConfigureAwait(false);
                    continue;
                }

                if (code == SQLITE_DONE)
                {
                    yield break;
                }

                yield return CU32(statement, 0);
            } while (true);
        }
        finally
        {
            Reset(statement);
        }
    }

    [StringLiteral.Utf8("SELECT \"Id\" FROM \"TagTable\" WHERE \"Value\" = ?")] private static partial ReadOnlySpan<byte> Literal_FindTag();
    
    [StringLiteral.Utf8("SELECT \"Id\" FROM \"ToolTable\" WHERE \"Value\" = ?")] private static partial ReadOnlySpan<byte> Literal_FindTool();

    private async ValueTask<uint?> FindTagOrToolAsync(sqlite3_stmt statement, string key, CancellationToken token)
    {
        Bind(statement, 1, key);
        try
        {
            do
            {
                var code = Step(statement);
                if (code == SQLITE_BUSY)
                {
                    await Task.Delay(TimeSpan.FromSeconds(1d), token).ConfigureAwait(false);
                    continue;
                }

                if (code == SQLITE_DONE)
                {
                    return null;
                }

                return CU32(statement, 0);
            } while (true);
        }
        finally
        {
            Reset(statement);
        }
    }

    public ValueTask<uint?> FindTagAsync(string key, CancellationToken token) => FindTagOrToolAsync(findTagStatement ??= Prepare(Literal_FindTag(), true, out _), key, token);

    public ValueTask<uint?> FindToolAsync(string key, CancellationToken token) => FindTagOrToolAsync(findToolStatement ??= Prepare(Literal_FindTool(), true, out _), key, token);

    [StringLiteral.Utf8("DELETE FROM \"ArtworkTagCrossTable\" WHERE \"Id\" = ?")]
    private static partial ReadOnlySpan<byte> Literal_Delete_From_ArtworkTagCrossTable();

    [StringLiteral.Utf8("DELETE FROM \"ArtworkTagCrossTable\" WHERE \"Id\" = ? AND \"ValueKind\" = 1")]
    private static partial ReadOnlySpan<byte> Literal_Delete_From_ArtworkTagCrossTable_Where_ValueKind_Equals_1();

    [StringLiteral.Utf8("DELETE FROM \"UserTagCrossTable\" WHERE \"Id\" = ?")]
    private static partial ReadOnlySpan<byte> Literal_Delete_From_UserTagCrossTable();

    [StringLiteral.Utf8("DELETE FROM \"ArtworkToolCrossTable\" WHERE \"Id\" = ?")]
    private static partial ReadOnlySpan<byte> Literal_Delete_From_ArtworkToolCrossTable();

    private ValueTask DeleteTagsOfArtworkStatementAsync(ulong id, CancellationToken token)
    {
        var statement = deleteTagsOfArtworkStatement ??= Prepare(Literal_Delete_From_ArtworkTagCrossTable(), true, out _);
        Bind(statement, 0x01, id);
        return ExecuteAsync(statement, token);
    }

    private ValueTask DeleteTagsOfArtworkWhereValueKindEquals1StatementAsync(ulong id, CancellationToken token)
    {
        var statement = deleteTagsOfArtworkStatement ??= Prepare(Literal_Delete_From_ArtworkTagCrossTable_Where_ValueKind_Equals_1(), true, out _);
        Bind(statement, 0x01, id);
        return ExecuteAsync(statement, token);
    }

    private ValueTask DeleteTagsOfUserStatementAsync(ulong id, CancellationToken token)
    {
        var statement = deleteTagsOfUserStatement ??= Prepare(Literal_Delete_From_UserTagCrossTable(), true, out _);
        Bind(statement, 0x01, id);
        return ExecuteAsync(statement, token);
    }

    private ValueTask DeleteToolsOfArtworkStatementAsync(ulong id, CancellationToken token)
    {
        var statement = deleteToolsOfArtworkStatement ??= Prepare(Literal_Delete_From_ArtworkToolCrossTable(), true, out _);
        Bind(statement, 0x01, id);
        return ExecuteAsync(statement, token);
    }
}
