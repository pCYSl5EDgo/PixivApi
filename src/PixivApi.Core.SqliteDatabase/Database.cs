namespace PixivApi.Core.SqliteDatabase;

internal sealed partial class Database : IExtenededDatabase, ITransactionalDatabase, IDisposable
{
    private readonly ILogger logger;
    private readonly bool logTrace;
    private readonly bool logDebug;
    private readonly bool logError;

    internal readonly sqlite3 database;

    public void Dispose()
    {
        CloseStatement(ref beginTransactionStatement);
        CloseStatement(ref beginExclusiveTransactionStatement);
        CloseStatement(ref endTransactionStatement);
        CloseStatement(ref rollbackTransactionStatement);
        CloseStatement(ref getRankingStatement);
        CloseStatement(ref addOrUpdateRankingStatementArray);
        CloseStatement(ref getArtworkStatement);
        CloseStatement(ref getTagsOfArtworkStatement);
        CloseStatement(ref getToolsOfArtworkStatement);
        CloseStatement(ref getUgoiraFramesStatement);
        CloseStatement(ref getHideReasonsStatement);
        CloseStatement(ref enumerateArtworkStatement);
        CloseStatement(ref officiallyRemoveArtworkStatement);
        CloseStatement(ref selectHidePagesStatement);
        CloseStatement(ref insertUserStatement);
        CloseStatement(ref insertUser_UserResponse_Statement);
        CloseStatement(ref insertUser_UserPreviewResponse_Statement);
        CloseStatement(ref insertUserDetailStatement);
        CloseStatement(ref insertTagsOfUserStatementArray);
        CloseStatement(ref getUserStatement);
        CloseStatement(ref getUserDetailStatement);
        CloseStatement(ref getTagsOfUserStatement);
        CloseStatement(ref enumerateUserStatement);
        CloseStatement(ref officiallyRemoveUserStatement);
        CloseStatement(ref countArtworkStatement);
        CloseStatement(ref countUserStatement);
        CloseStatement(ref countTagStatement);
        CloseStatement(ref countToolStatement);
        CloseStatement(ref countRankingStatement);
        CloseStatement(ref existsArtworkStatement);
        CloseStatement(ref deleteHidesStatement);
        CloseStatement(ref updateArtworkStatement);
        CloseStatement(ref insertArtworkStatement);
        CloseStatement(ref insertOrUpdateArtwork_ArtworkResponseContent_Statement);
        CloseStatement(ref insertUgoiraFramesStatementArray);
        CloseStatement(ref insertHidesStatementArray);
        CloseStatement(ref insertTagsOfArtworkStatementArray);
        CloseStatement(ref insertArtworkToolCrossTableStatementArray);
        CloseStatement(ref insertArtworkTagCrossTableStatementArray);
        CloseStatement(ref getTagStatement);
        CloseStatement(ref getToolStatement);
        CloseStatement(ref registerTagStatement);
        CloseStatement(ref registerToolStatement);
        CloseStatement(ref enumerateTagStatement);
        CloseStatement(ref enumerateToolStatement);
        CloseStatement(ref selectIdFromTagTextTableMatch);
        CloseStatement(ref selectIdFromTagTextTableLike);
        CloseStatement(ref findTagStatement);
        CloseStatement(ref findToolStatement);
        CloseStatement(ref deleteTagsOfArtworkStatement);
        CloseStatement(ref deleteTagsOfUserStatement);
        CloseStatement(ref deleteToolsOfArtworkStatement);
        CloseStatement(ref insertOrIgnoreIntoUserTagCrossTableStatement);

        database.manual_close_v2();
    }

    public Database(ILogger logger, string path)
    {
        var code = sqlite3_open_v2(path, out database, SQLITE_OPEN_READWRITE | SQLITE_OPEN_CREATE | SQLITE_OPEN_NOMUTEX, null);
        if (code != SQLITE_OK)
        {
            throw new Exception($"Error Code: {code} Text: {sqlite3_errmsg(database).utf8_to_string()}");
        }

        this.logger = logger;
        logTrace = logger.IsEnabled(LogLevel.Trace);
        logDebug = logger.IsEnabled(LogLevel.Debug);
        logError = logger.IsEnabled(LogLevel.Error);
    }

    #region Prepare
#pragma warning disable CA2254
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private sqlite3_stmt Prepare(ReadOnlySpan<byte> query, bool persistent, out int code)
    {
        sqlite3_stmt statement;
        lock (database)
        {
            code = sqlite3_prepare_v3(database, query, persistent ? SQLITE_PREPARE_PERSISTENT : 0U, out statement);
        }

        if (logDebug)
        {
            logger.LogDebug($"Query: {System.Text.Encoding.UTF8.GetString(query)}\nCode: {code}");
        }

        if (logError && code == SQLITE_ERROR)
        {
            logger.LogError($"Error: {sqlite3_errmsg(database).utf8_to_string()}");
        }

        return statement;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private sqlite3_stmt Prepare(ref Utf8ValueStringBuilder query, bool persistent, out int code)
    {
        sqlite3_stmt statement;
        lock (database)
        {
            code = sqlite3_prepare_v3(database, query.AsSpan(), persistent ? SQLITE_PREPARE_PERSISTENT : 0U, out statement);
        }

        if (logDebug)
        {
            logger.LogDebug($"Query: {query}\nCode: {code}");
        }

        if (logError && code == SQLITE_ERROR)
        {
            logger.LogError($"Error: {sqlite3_errmsg(database).utf8_to_string()}");
        }

        return statement;
    }
    #endregion

    #region Close
    private static void CloseStatement(ref sqlite3_stmt? statement)
    {
        if (statement is not null)
        {
            statement.manual_close();
            statement = null;
        }
    }

    private static void CloseStatement(ref sqlite3_stmt?[]? array)
    {
        if (array is null)
        {
            return;
        }

        foreach (ref var statement in array.AsSpan())
        {
            statement?.manual_close();
            statement = null;
        }

        ArrayPool<sqlite3_stmt?>.Shared.Return(array);
    }
    #endregion

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private int Step(sqlite3_stmt statement)
    {
        var code = sqlite3_step(statement);
        if (logTrace)
        {
            logger.LogTrace($"Step\n{code}");
        }

        if (logError)
        {
            switch (code)
            {
                case SQLITE_DONE:
                case SQLITE_BUSY:
                case SQLITE_ROW:
                    break;
                default:
                    logger.LogError($"Error: {sqlite3_errmsg(database).utf8_to_string()} - {sqlite3_sql(statement).utf8_to_string()}");
                    if (logTrace)
                    {
                        logger.LogTrace(Environment.StackTrace);
                    }

                    break;
            }
        }

        return code;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private int Reset(sqlite3_stmt statement)
    {
        var code = sqlite3_reset(statement);
        if (logTrace)
        {
            logger.LogTrace($"Reset: {code}");
        }

        code = sqlite3_clear_bindings(statement);
        return code;
    }

    private async ValueTask ExecuteAsync(sqlite3_stmt statement, CancellationToken token)
    {
        do
        {
            var code = Step(statement);
            if (code == SQLITE_BUSY)
            {
                if (logTrace)
                {
                    logger.LogTrace($"Busy Extended Error Code: {sqlite3_extended_errcode(database)}");
                }

                await Task.Delay(TimeSpan.FromSeconds(1d), token).ConfigureAwait(false);
                continue;
            }

            if (code == SQLITE_DONE)
            {
                break;
            }

            if (code == SQLITE_ROW)
            {
                continue;
            }

            throw new InvalidOperationException($"Error: {code} - {sqlite3_errmsg(database).utf8_to_string()}");
        } while (!token.IsCancellationRequested);
    }

    #region Bind
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private int Bind(sqlite3_stmt statement, int index, ReadOnlySpan<char> value)
    {
        var code = sqlite3_bind_text16(statement, index, value);
        if (code == SQLITE_MISUSE)
        {
            throw new InvalidOperationException($"MISUSE - {sqlite3_sql(statement).utf8_to_string()}");
        }

        if (logTrace)
        {
            logger.LogTrace($"Bind {index} Code: {code} UTF16: {value}");
        }

        return code;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private int Bind(sqlite3_stmt statement, int index, bool value)
    {
        var code = sqlite3_bind_int(statement, index, value ? 1 : 0);
        if (code == SQLITE_MISUSE)
        {
            throw new InvalidOperationException($"MISUSE - {sqlite3_sql(statement).utf8_to_string()}");
        }

        if (logTrace)
        {
            logger.LogTrace($"Bind {index} Code: {code} Bool: {value}");
        }

        return code;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private int Bind(sqlite3_stmt statement, int index, ArtworkType value)
    {
        var code = sqlite3_bind_int(statement, index, (byte)value);
        if (code == SQLITE_MISUSE)
        {
            throw new InvalidOperationException($"MISUSE - {sqlite3_sql(statement).utf8_to_string()}");
        }

        if (logTrace)
        {
            logger.LogTrace($"Bind {index} Code: {code} ArtworkType: {value}");
        }

        return code;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private int Bind(sqlite3_stmt statement, int index, HideReason value)
    {
        var code = sqlite3_bind_int(statement, index, (byte)value);
        if (code == SQLITE_MISUSE)
        {
            throw new InvalidOperationException($"MISUSE - {sqlite3_sql(statement).utf8_to_string()}");
        }

        if (logTrace)
        {
            logger.LogTrace($"Bind {index} Code: {code} HideReason: {value}");
        }

        return code;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private int Bind(sqlite3_stmt statement, int index, FileExtensionKind value)
    {
        var code = sqlite3_bind_int(statement, index, (byte)value);
        if (code == SQLITE_MISUSE)
        {
            throw new InvalidOperationException($"MISUSE - {sqlite3_sql(statement).utf8_to_string()}");
        }

        if (logTrace)
        {
            logger.LogTrace($"Bind {index} Code: {code} FileExtensionKind: {value}");
        }

        return code;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private int Bind(sqlite3_stmt statement, int index, RankingKind value)
    {
        var code = sqlite3_bind_int(statement, index, (byte)value);
        if (code == SQLITE_MISUSE)
        {
            throw new InvalidOperationException($"MISUSE - {sqlite3_sql(statement).utf8_to_string()}");
        }

        if (logTrace)
        {
            logger.LogTrace($"Bind {index} Code: {code} RankingKind: {value}");
        }

        return code;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private int Bind(sqlite3_stmt statement, int index, long value)
    {
        var code = sqlite3_bind_int64(statement, index, value);
        if (code == SQLITE_MISUSE)
        {
            throw new InvalidOperationException($"MISUSE - {sqlite3_sql(statement).utf8_to_string()}");
        }

        if (logTrace)
        {
            logger.LogTrace($"Bind {index} Code: {code} Int64: {value}");
        }

        return code;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private int Bind(sqlite3_stmt statement, int index, int value)
    {
        var code = sqlite3_bind_int(statement, index, value);
        if (code == SQLITE_MISUSE)
        {
            throw new InvalidOperationException($"MISUSE - {sqlite3_sql(statement).utf8_to_string()}");
        }

        if (logTrace)
        {
            logger.LogTrace($"Bind {index} Code: {code} Int32: {value}");
        }

        return code;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private int Bind(sqlite3_stmt statement, int index, ulong value)
    {
        var code = sqlite3_bind_int64(statement, index, (long)value);
        if (code == SQLITE_MISUSE)
        {
            throw new InvalidOperationException($"MISUSE - {sqlite3_sql(statement).utf8_to_string()}");
        }

        if (logTrace)
        {
            logger.LogTrace($"Bind {index} Code: {code} UInt64: {value}");
        }

        return code;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private int Bind(sqlite3_stmt statement, int index, DateOnly value)
    {
        var builder = ZString.CreateUtf8StringBuilder();
        builder.Append(value);
        var code = sqlite3_bind_text(statement, index, builder.AsSpan());
        builder.Dispose();
        if (code == SQLITE_MISUSE)
        {
            throw new InvalidOperationException($"MISUSE - {sqlite3_sql(statement).utf8_to_string()}");
        }

        if (logTrace)
        {
            logger.LogTrace($"Bind {index} Code: {code} Date: {value}");
        }

        return code;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private int Bind(sqlite3_stmt statement, int index, DateTime value)
    {
        var builder = ZString.CreateUtf8StringBuilder();
        builder.Append(value);
        var code = sqlite3_bind_text(statement, index, builder.AsSpan());
        builder.Dispose();
        if (code == SQLITE_MISUSE)
        {
            throw new InvalidOperationException($"MISUSE - {sqlite3_sql(statement).utf8_to_string()}");
        }

        if (logTrace)
        {
            logger.LogTrace($"Bind {index} Code: {code} DateTime: {value}");
        }

        return code;
    }
    #endregion

    #region Column
#pragma warning restore CA2254
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static ulong CU64(sqlite3_stmt statement, int index) => (ulong)sqlite3_column_int64(statement, index);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static long CI64(sqlite3_stmt statement, int index) => sqlite3_column_int64(statement, index);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static int CI32(sqlite3_stmt statement, int index) => sqlite3_column_int(statement, index);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static uint CU32(sqlite3_stmt statement, int index) => (uint)sqlite3_column_int(statement, index);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static ushort CU16(sqlite3_stmt statement, int index) => (ushort)sqlite3_column_int(statement, index);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool CBool(sqlite3_stmt statement, int index) => sqlite3_column_int(statement, index) != 0;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static string? CStr(sqlite3_stmt statement, int index) => sqlite3_column_text(statement, index).utf8_to_string();

    private async ValueTask<ulong[]> CU64ArrayAsync(sqlite3_stmt statement, CancellationToken token)
    {
        var rental = ArrayPool<ulong>.Shared.Rent(16);
        var count = 0;
        try
        {
            int code;
            do
            {
                while ((code = Step(statement)) == SQLITE_BUSY)
                {
                    await Task.Delay(TimeSpan.FromSeconds(1d), token).ConfigureAwait(false);
                }

                if (code == SQLITE_DONE)
                {
                    if (count == 0)
                    {
                        return Array.Empty<ulong>();
                    }

                    var answer = new ulong[count];
                    rental.AsSpan(0, count).CopyTo(answer);
                    return answer;
                }

                if (code == SQLITE_ROW)
                {
                    if (++count >= rental.Length)
                    {
                        var tmp = ArrayPool<ulong>.Shared.Rent(count);
                        rental.AsSpan(0, count - 1).CopyTo(tmp);
                        ArrayPool<ulong>.Shared.Return(rental);
                        rental = tmp;
                    }

                    rental[count - 1] = CU64(statement, 0);
                }
            } while (true);
        }
        finally
        {
            ArrayPool<ulong>.Shared.Return(rental);
        }
    }

    private async ValueTask<uint[]> CU32ArrayAsync(sqlite3_stmt statement, CancellationToken token)
    {
        var rental = ArrayPool<uint>.Shared.Rent(16);
        var count = 0;
        try
        {
            int code;
            do
            {
                while ((code = Step(statement)) == SQLITE_BUSY)
                {
                    await Task.Delay(TimeSpan.FromSeconds(1d), token).ConfigureAwait(false);
                }

                if (code == SQLITE_DONE)
                {
                    if (count == 0)
                    {
                        return Array.Empty<uint>();
                    }

                    var answer = new uint[count];
                    rental.AsSpan(0, count).CopyTo(answer);
                    return answer;
                }

                if (code == SQLITE_ROW)
                {
                    if (++count >= rental.Length)
                    {
                        var tmp = ArrayPool<uint>.Shared.Rent(count);
                        rental.AsSpan(0, count - 1).CopyTo(tmp);
                        ArrayPool<uint>.Shared.Return(rental);
                        rental = tmp;
                    }

                    rental[count - 1] = CU32(statement, 0);
                }
            } while (true);
        }
        finally
        {
            ArrayPool<uint>.Shared.Return(rental);
        }
    }

    private async ValueTask<ushort[]> CU16ArrayAsync(sqlite3_stmt statement, CancellationToken token)
    {
        var rental = ArrayPool<ushort>.Shared.Rent(16);
        var count = 0;
        try
        {
            do
            {
                token.ThrowIfCancellationRequested();
                var code = Step(statement);
                if (code == SQLITE_BUSY)
                {
                    await Task.Delay(TimeSpan.FromSeconds(1d), token).ConfigureAwait(false);
                    continue;
                }

                if (code == SQLITE_DONE)
                {
                    if (count == 0)
                    {
                        return Array.Empty<ushort>();
                    }

                    var answer = new ushort[count];
                    rental.AsSpan(0, count).CopyTo(answer);
                    return answer;
                }

                if (code == SQLITE_ROW)
                {
                    if (++count >= rental.Length)
                    {
                        var tmp = ArrayPool<ushort>.Shared.Rent(count);
                        rental.AsSpan(0, count - 1).CopyTo(tmp);
                        ArrayPool<ushort>.Shared.Return(rental);
                        rental = tmp;
                    }

                    rental[count - 1] = CU16(statement, 0);
                }
            } while (true);
        }
        finally
        {
            ArrayPool<ushort>.Shared.Return(rental);
        }
    }
    #endregion

    private static ref sqlite3_stmt? At([NotNull] ref sqlite3_stmt?[]? array, int index)
    {
        if (array is null)
        {
            array = ArrayPool<sqlite3_stmt>.Shared.Rent(index + 1);
        }
        else if (index >= array.Length)
        {
            var tmp = ArrayPool<sqlite3_stmt?>.Shared.Rent(index + 1);
            array.CopyTo(tmp, 0);
            tmp.AsSpan(array.Length).Clear();
            Array.Clear(array);
            ArrayPool<sqlite3_stmt?>.Shared.Return(array);
            array = tmp;
        }

        return ref array[index];
    }

    #region Version
    [StringLiteral.Utf8("SELECT \"Major\", \"Minor\" FROM \"InfoTable\" ORDER BY \"Major\" DESC, \"Minor\" DESC LIMIT 1")]
    private static partial ReadOnlySpan<byte> Literal_Select_Version();

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
                    statement = Prepare(Literal_Select_Version(), false, out _);
                    var code = Step(statement);
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
    #endregion

    private ulong GetLastInsertRowId() => (ulong)sqlite3_last_insert_rowid(database);

    public async ValueTask<bool> AddOrUpdateAsync(ulong id, Func<CancellationToken, ValueTask<Artwork>> add, Func<Artwork, CancellationToken, ValueTask> update, CancellationToken token)
    {
        if (id == 0)
        {
            return false;
        }

        var answer = await GetArtworkAsync(id, token).ConfigureAwait(false);
        if (answer is null)
        {
            answer = await add(token).ConfigureAwait(false);
            token.ThrowIfCancellationRequested();
            await InsertAsync(answer, token).ConfigureAwait(false);
            return true;
        }
        else
        {
            await update(answer, token).ConfigureAwait(false);
            token.ThrowIfCancellationRequested();
            await UpdateAsync(answer, token).ConfigureAwait(false);
            return false;
        }
    }

    public async ValueTask<bool> AddOrUpdateAsync(ulong id, Func<CancellationToken, ValueTask<User>> add, Func<User, CancellationToken, ValueTask> update, CancellationToken token)
    {
        if (id == 0)
        {
            return false;
        }

        var answer = await GetUserAsync(id, token).ConfigureAwait(false);
        bool returnValue;
        if (answer is null)
        {
            answer = await add(token).ConfigureAwait(false);
            returnValue = true;
        }
        else
        {
            await update(answer, token).ConfigureAwait(false);
            returnValue = false;
        }

        token.ThrowIfCancellationRequested();
        await InsertOrUpdateAsync(answer, token).ConfigureAwait(false);
        return returnValue;
    }
}
