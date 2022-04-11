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
    private sqlite3_stmt? insertOrIgnoreIntoUserTagCrossTableStatement;

    [StringLiteral.Utf8("SELECT \"Value\" FROM \"TagTable\" WHERE \"Id\" = ?")] private static partial ReadOnlySpan<byte> Literal_SelectValueFromTagTableWhereId();
    [StringLiteral.Utf8("SELECT \"Value\" FROM \"ToolTable\" WHERE \"Id\" = ?")] private static partial ReadOnlySpan<byte> Literal_SelectValueFromToolTableWhereId();

    private async ValueTask<string?> GetTagOrToolAsync(sqlite3_stmt statement, uint id, CancellationToken token)
    {
        Bind(statement, 1, id);
        do
        {
            token.ThrowIfCancellationRequested();
            var code = Step(statement);
            if (code == SQLITE_BUSY)
            {
                await Task.Delay(TimeSpan.FromSeconds(1d), token).ConfigureAwait(false);
                continue;
            }

            if (code == SQLITE_ROW)
            {
                return CStr(statement, 0);
            }

            return null;
        } while (true);
    }

    public ValueTask<string?> GetTagAsync(uint id, CancellationToken token)
    {
        if (getTagStatement is null)
        {
            getTagStatement = Prepare(Literal_SelectValueFromTagTableWhereId(), true, out _);
        }
        else
        {
            Reset(getTagStatement);
        }

        return GetTagOrToolAsync(getTagStatement, id, token);
    }

    public ValueTask<string?> GetToolAsync(uint id, CancellationToken token)
    {
        if (getToolStatement is null)
        {
            getToolStatement = Prepare(Literal_SelectValueFromToolTableWhereId(), true, out _);
        }
        else
        {
            Reset(getToolStatement);
        }

        return GetTagOrToolAsync(getToolStatement, id, token);
    }

    private async ValueTask<uint> RegisterTagOrToolAsync(sqlite3_stmt statement, string value, CancellationToken token)
    {
        Bind(statement, 1, value);
        uint answer = 0;
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
                return answer;
            }

            if (code == SQLITE_ROW)
            {
                answer = CU32(statement, 0);
                continue;
            }

            throw new Exception($"Error Code: {code} Message: {sqlite3_errmsg(database).utf8_to_string()}");
        } while (true);
    }

    [StringLiteral.Utf8("INSERT INTO \"TagTable\" (\"Value\") VALUES (?) RETURNING \"Id\"")] private static partial ReadOnlySpan<byte> Literal_InsertIntoTagTableReturningId();

    public async ValueTask<uint> RegisterTagAsync(string value, CancellationToken token)
    {
        var id = await FindTagAsync(value, token).ConfigureAwait(false);
        if (id.HasValue)
        {
            return id.Value;
        }

        if (registerTagStatement is null)
        {
            var statement = Prepare(Literal_InsertIntoTagTableReturningId(), true, out _);
            if (Interlocked.CompareExchange(ref registerTagStatement, statement, null) != null)
            {
                statement.manual_close();
            }
        }
        else
        {
            Reset(registerTagStatement);
        }

        return await RegisterTagOrToolAsync(registerTagStatement, value, token).ConfigureAwait(false);
    }

    [StringLiteral.Utf8("INSERT INTO \"ToolTable\" (\"Value\") VALUES (?) RETURNING \"Id\"")] private static partial ReadOnlySpan<byte> Literal_InsertIntoToolTableReturningId();

    public async ValueTask<uint> RegisterToolAsync(string value, CancellationToken token)
    {
        var id = await FindToolAsync(value, token).ConfigureAwait(false);
        if (id.HasValue)
        {
            return id.Value;
        }

        if (registerToolStatement is null)
        {
            var statement = Prepare(Literal_InsertIntoToolTableReturningId(), true, out _);
            if (Interlocked.CompareExchange(ref registerToolStatement, statement, null) != null)
            {
                statement.manual_close();
            }
        }
        else
        {
            Reset(registerToolStatement);
        }

        return await RegisterTagOrToolAsync(registerToolStatement, value, token).ConfigureAwait(false);
    }

    [StringLiteral.Utf8("SELECT \"Value\", \"Id\" FROM \"TagTable\"")]
    private static partial ReadOnlySpan<byte> Literal_SelectValueId_FromTagTable();

    [StringLiteral.Utf8("SELECT \"Value\", \"Id\" FROM \"ToolTable\"")]
    private static partial ReadOnlySpan<byte> Literal_SelectValueId_FromToolTable();

    private async IAsyncEnumerable<(string, uint)> EnumerateTagToolAsync(sqlite3_stmt statement, [EnumeratorCancellation] CancellationToken token)
    {
        do
        {
            var code = Step(statement);
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

    public IAsyncEnumerable<(string, uint)> EnumerateTagAsync(CancellationToken token)
    {
        if (enumerateTagStatement is null)
        {
            enumerateTagStatement = Prepare(Literal_SelectValueId_FromTagTable(), true, out _);
        }
        else
        {
            Reset(enumerateTagStatement);
        }

        return EnumerateTagToolAsync(enumerateTagStatement, token);
    }

    public IAsyncEnumerable<(string, uint)> EnumerateToolAsync(CancellationToken token)
    {
        if (enumerateToolStatement is null)
        {
            enumerateToolStatement = Prepare(Literal_SelectValueId_FromToolTable(), true, out _);
        }
        else
        {
            Reset(enumerateToolStatement);
        }

        return EnumerateTagToolAsync(enumerateToolStatement, token);
    }

    [StringLiteral.Utf8("SELECT \"rowid\" FROM \"TagTextTable\" (?)")]
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
            if (selectIdFromTagTextTableMatch is null)
            {
                selectIdFromTagTextTableMatch = Prepare(Literal_SelectId_FromTagTextTable_Match(), true, out _);
            }
            else
            {
                Reset(selectIdFromTagTextTableMatch);
            }

            statement = selectIdFromTagTextTableMatch;
        }
        else
        {
            if (selectIdFromTagTextTableLike is null)
            {
                selectIdFromTagTextTableLike = Prepare(Literal_SelectId_FromTagTable_Like(), true, out _);
            }
            else
            {
                Reset(selectIdFromTagTextTableLike);
            }

            statement = selectIdFromTagTextTableLike;
        }

        Bind(statement, 1, key);
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

    [StringLiteral.Utf8("SELECT \"Id\" FROM \"TagTable\" WHERE \"Value\" = ?")] private static partial ReadOnlySpan<byte> Literal_FindTag();

    [StringLiteral.Utf8("SELECT \"Id\" FROM \"ToolTable\" WHERE \"Value\" = ?")] private static partial ReadOnlySpan<byte> Literal_FindTool();

    private async ValueTask<uint?> FindTagOrToolAsync(sqlite3_stmt statement, string key, CancellationToken token)
    {
        Bind(statement, 1, key);
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

    public ValueTask<uint?> FindTagAsync(string key, CancellationToken token)
    {
        if (findTagStatement is null)
        {
            findTagStatement = Prepare(Literal_FindTag(), true, out _);
        }
        else
        {
            Reset(findTagStatement);
        }

        return FindTagOrToolAsync(findTagStatement, key, token);
    }

    public ValueTask<uint?> FindToolAsync(string key, CancellationToken token)
    {
        if (findToolStatement is null)
        {
            findToolStatement = Prepare(Literal_FindTool(), true, out _);
        }
        else
        {
            Reset(findToolStatement);
        }

        return FindTagOrToolAsync(findToolStatement, key, token);
    }

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
        if (deleteTagsOfArtworkStatement is null)
        {
            deleteTagsOfArtworkStatement = Prepare(Literal_Delete_From_ArtworkTagCrossTable(), true, out _);
        }
        else
        {
            Reset(deleteTagsOfArtworkStatement);
        }

        var statement = deleteTagsOfArtworkStatement;
        Bind(statement, 0x01, id);
        return ExecuteAsync(statement, token);
    }

    private ValueTask DeleteTagsOfArtworkWhereValueKindEquals1StatementAsync(ulong id, CancellationToken token)
    {
        logger.LogTrace("Delete Tags");
        if (deleteTagsOfArtworkStatement is null)
        {
            deleteTagsOfArtworkStatement = Prepare(Literal_Delete_From_ArtworkTagCrossTable_Where_ValueKind_Equals_1(), true, out _);
        }
        else
        {
            Reset(deleteTagsOfArtworkStatement);
        }

        var statement = deleteTagsOfArtworkStatement;
        Bind(statement, 0x01, id);
        return ExecuteAsync(statement, token);
    }

    private ValueTask DeleteTagsOfUserStatementAsync(ulong id, CancellationToken token)
    {
        if (deleteTagsOfUserStatement is null)
        {
            deleteTagsOfUserStatement = Prepare(Literal_Delete_From_UserTagCrossTable(), true, out _);
        }
        else
        {
            Reset(deleteTagsOfUserStatement);
        }

        var statement = deleteTagsOfUserStatement;
        Bind(statement, 0x01, id);
        return ExecuteAsync(statement, token);
    }

    private ValueTask DeleteToolsOfArtworkStatementAsync(ulong id, CancellationToken token)
    {
        logger.LogTrace("Delete Tools");
        if (deleteToolsOfArtworkStatement is null)
        {
            deleteToolsOfArtworkStatement = Prepare(Literal_Delete_From_ArtworkToolCrossTable(), true, out _);
        }
        else
        {
            Reset(deleteToolsOfArtworkStatement);
        }

        var statement = deleteToolsOfArtworkStatement;
        Bind(statement, 0x01, id);
        return ExecuteAsync(statement, token);
    }

    [StringLiteral.Utf8("INSERT OR IGNORE INTO \"UserTagCrossTable\" (\"Id\", \"TagId\") VALUES (?1, ?2)")]
    private static partial ReadOnlySpan<byte> Literal_InsertOrIgnoreIntoUserTagCrossTable();

    public ValueTask AddTagToUser(ulong id, uint tagId, CancellationToken token)
    {
        if (id == 0 || tagId == 0)
        {
            return ValueTask.CompletedTask;
        }

        if (insertOrIgnoreIntoUserTagCrossTableStatement is null)
        {
            insertOrIgnoreIntoUserTagCrossTableStatement = Prepare(Literal_InsertOrIgnoreIntoUserTagCrossTable(), true, out _);
        }
        else
        {
            Reset(insertOrIgnoreIntoUserTagCrossTableStatement);
        }

        return ExecuteAsync(insertOrIgnoreIntoUserTagCrossTableStatement, token);
    }
}
