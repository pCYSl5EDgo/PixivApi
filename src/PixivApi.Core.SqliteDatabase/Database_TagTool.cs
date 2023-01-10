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
            getTagStatement = Prepare("SELECT \"Value\" FROM \"TagTable\" WHERE \"Id\" = ?"u8, true, out _);
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
            getToolStatement = Prepare("SELECT \"Value\" FROM \"ToolTable\" WHERE \"Id\" = ?"u8, true, out _);
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

    public async ValueTask<uint> RegisterTagAsync(string value, CancellationToken token)
    {
        var id = await FindTagAsync(value, token).ConfigureAwait(false);
        if (id.HasValue)
        {
            return id.Value;
        }

        if (registerTagStatement is null)
        {
            var statement = Prepare("INSERT INTO \"TagTable\" (\"Value\") VALUES (?) RETURNING \"Id\""u8, true, out _);
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

    public async ValueTask<uint> RegisterToolAsync(string value, CancellationToken token)
    {
        var id = await FindToolAsync(value, token).ConfigureAwait(false);
        if (id.HasValue)
        {
            return id.Value;
        }

        if (registerToolStatement is null)
        {
            var statement = Prepare("INSERT INTO \"ToolTable\" (\"Value\") VALUES (?) RETURNING \"Id\""u8, true, out _);
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
            enumerateTagStatement = Prepare("SELECT \"Value\", \"Id\" FROM \"TagTable\""u8, true, out _);
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
            enumerateToolStatement = Prepare("SELECT \"Value\", \"Id\" FROM \"ToolTable\""u8, true, out _);
        }
        else
        {
            Reset(enumerateToolStatement);
        }

        return EnumerateTagToolAsync(enumerateToolStatement, token);
    }

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
                selectIdFromTagTextTableMatch = Prepare("SELECT \"rowid\" FROM \"TagTextTable\" (?)"u8, true, out _);
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
                selectIdFromTagTextTableLike = Prepare("SELECT \"Id\" FROM \"TagTable\" WHERE \"Value\" LIKE ('%' || ? || '%')"u8, true, out _);
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
            findTagStatement = Prepare("SELECT \"Id\" FROM \"TagTable\" WHERE \"Value\" = ?"u8, true, out _);
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
            findToolStatement = Prepare("SELECT \"Id\" FROM \"ToolTable\" WHERE \"Value\" = ?"u8, true, out _);
        }
        else
        {
            Reset(findToolStatement);
        }

        return FindTagOrToolAsync(findToolStatement, key, token);
    }

    private ValueTask DeleteTagsOfArtworkStatementAsync(ulong id, CancellationToken token)
    {
        if (deleteTagsOfArtworkStatement is null)
        {
            deleteTagsOfArtworkStatement = Prepare("DELETE FROM \"ArtworkTagCrossTable\" WHERE \"Id\" = ?"u8, true, out _);
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
            deleteTagsOfArtworkStatement = Prepare("DELETE FROM \"ArtworkTagCrossTable\" WHERE \"Id\" = ? AND \"ValueKind\" = 1"u8, true, out _);
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
            deleteTagsOfUserStatement = Prepare("DELETE FROM \"UserTagCrossTable\" WHERE \"Id\" = ?"u8, true, out _);
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
            deleteToolsOfArtworkStatement = Prepare("DELETE FROM \"ArtworkToolCrossTable\" WHERE \"Id\" = ?"u8, true, out _);
        }
        else
        {
            Reset(deleteToolsOfArtworkStatement);
        }

        var statement = deleteToolsOfArtworkStatement;
        Bind(statement, 0x01, id);
        return ExecuteAsync(statement, token);
    }

    public ValueTask AddTagToUser(ulong id, uint tagId, CancellationToken token)
    {
        if (id == 0 || tagId == 0)
        {
            return ValueTask.CompletedTask;
        }

        if (insertOrIgnoreIntoUserTagCrossTableStatement is null)
        {
            insertOrIgnoreIntoUserTagCrossTableStatement = Prepare("INSERT OR IGNORE INTO \"UserTagCrossTable\" (\"Id\", \"TagId\") VALUES (?1, ?2)"u8, true, out _);
        }
        else
        {
            Reset(insertOrIgnoreIntoUserTagCrossTableStatement);
        }

        var statement = insertOrIgnoreIntoUserTagCrossTableStatement;
        Bind(statement, 1, id);
        Bind(statement, 2, tagId);

        return ExecuteAsync(insertOrIgnoreIntoUserTagCrossTableStatement, token);
    }
}
