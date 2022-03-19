using Cysharp.Text;
using Microsoft.Extensions.Logging;
using PixivApi.Core.Local;
using PixivApi.Core.Network;
using SQLitePCL;
using System.Buffers;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using static SQLitePCL.raw;

namespace PixivApi.Core.SqliteDatabase;

internal sealed partial class Database : IExtenededDatabase, IDisposable
{
    private readonly ILogger logger;
    private readonly bool logTrace;

    private readonly sqlite3 database;
    private sqlite3_stmt? countArtworkStatement;
    private sqlite3_stmt? countUserStatement;
    private sqlite3_stmt? countTagStatement;
    private sqlite3_stmt? countToolStatement;
    private sqlite3_stmt? countRankingStatement;
    private sqlite3_stmt? findTagStatement;

    private sqlite3_stmt? getArtworkStatement;
    private sqlite3_stmt? getArtworkTagStatement;
    private sqlite3_stmt? getArtworkToolStatement;
    private sqlite3_stmt? getArtworkUgoiraFramesStatement;
    private sqlite3_stmt? getArtworkHidePageStatement;

    private sqlite3_stmt? enumerableArtworkStatement;
    private sqlite3_stmt? enumerableUserStatement;

    private sqlite3_stmt? getTagStatement;
    private sqlite3_stmt? getToolStatement;
    private sqlite3_stmt? getRankingStatement;
    private sqlite3_stmt? getUserStatement;
    private sqlite3_stmt? getUserDetailStatement;

    private sqlite3_stmt? registerTagStatement;
    private sqlite3_stmt? registerToolStatement;

    private sqlite3_stmt?[]? insertArtworkStatementArray;
    private sqlite3_stmt?[]? insertUserStatementArray;
    private sqlite3_stmt?[]? insertUserDetailStatementArray;
    private sqlite3_stmt?[]? updateArtworkStatementArray;
    private sqlite3_stmt?[]? updateUserStatementArray;
    private sqlite3_stmt?[]? updateUserDetailStatementArray;

    private sqlite3_stmt?[]? addOrUpdateRankingStatementArray;

    private sqlite3_stmt? userDetailAddOrUpdateStatement;

    private Dictionary<(int Tag, int Tool), sqlite3_stmt>? artworkAddOrUpdateStatementDictionary;
    private Dictionary<ArtworkArrayKey, sqlite3_stmt>? userPreviewAddOrUpdateStatementDictionary;

    public void Dispose()
    {
        database.manual_close_v2();
        static void Close(ref sqlite3_stmt? statement)
        {
            if (statement is not null)
            {
                statement.manual_close();
                statement = null;
            }
        }

        static void CloseArray(ref sqlite3_stmt?[]? array)
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

        static void CloseTupleDictionary(ref Dictionary<(int, int), sqlite3_stmt>? dictionary)
        {
            if (dictionary is null)
            {
                return;
            }

            foreach (var statement in dictionary.Values)
            {
                statement.manual_close();
            }

            dictionary.Clear();
            dictionary = null;
        }

        static void CloseDictionary(ref Dictionary<ArtworkArrayKey, sqlite3_stmt>? dictionary)
        {
            if (dictionary is null)
            {
                return;
            }

            foreach (var (key, statement) in dictionary)
            {
                key.Dispose();
                statement.manual_close();
            }

            dictionary.Clear();
            dictionary = null;
        }

        Close(ref countArtworkStatement);
        Close(ref countUserStatement);
        Close(ref countTagStatement);
        Close(ref countToolStatement);
        Close(ref countRankingStatement);
        Close(ref findTagStatement);
        Close(ref getArtworkStatement);
        Close(ref getArtworkTagStatement);
        Close(ref getArtworkToolStatement);
        Close(ref getArtworkUgoiraFramesStatement);
        Close(ref getArtworkHidePageStatement);
        Close(ref enumerableArtworkStatement);
        Close(ref enumerableUserStatement);
        Close(ref getTagStatement);
        Close(ref getToolStatement);
        Close(ref getRankingStatement);
        Close(ref getUserStatement);
        Close(ref getUserDetailStatement);
        Close(ref registerTagStatement);
        Close(ref registerToolStatement);
        Close(ref userDetailAddOrUpdateStatement);
        CloseArray(ref insertArtworkStatementArray);
        CloseArray(ref insertUserStatementArray);
        CloseArray(ref insertUserDetailStatementArray);
        CloseArray(ref updateArtworkStatementArray);
        CloseArray(ref updateUserStatementArray);
        CloseArray(ref updateUserDetailStatementArray);
        CloseArray(ref addOrUpdateRankingStatementArray);
        CloseTupleDictionary(ref artworkAddOrUpdateStatementDictionary);
        CloseDictionary(ref userPreviewAddOrUpdateStatementDictionary);
    }

    public Database(ILogger logger, string path)
    {
        var code = sqlite3_open_v2(path, out database, SQLITE_OPEN_READWRITE | SQLITE_OPEN_CREATE | SQLITE_OPEN_NOMUTEX, "");
        if (code != SQLITE_OK)
        {
            throw new Exception($"Error Code: {code}");
        }

        this.logger = logger;
        logTrace = logger.IsEnabled(LogLevel.Trace);
    }

#pragma warning disable CA2254
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private sqlite3_stmt Prepare(string query, bool persistent, out int code)
    {
        code = sqlite3_prepare_v3(database, query, persistent ? SQLITE_PREPARE_PERSISTENT : 0U, out var statement);
        if (logTrace)
        {
            logger.LogTrace($"Query: {query}\nCode: {code}");
        }

        return statement;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private sqlite3_stmt Prepare(ref Utf8ValueStringBuilder query, bool persistent, out int code)
    {
        code = sqlite3_prepare_v3(database, query.AsSpan(), persistent ? SQLITE_PREPARE_PERSISTENT : 0U, out var statment);
        if (logTrace)
        {
            logger.LogTrace($"Query: {query}\nCode: {code}");
        }

        return statment;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private int Step(sqlite3_stmt statement)
    {
        var code = sqlite3_step(statement);
        if (logTrace)
        {
            logger.LogTrace($"Step\n{code}");
        }

        return code;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private int Reset(sqlite3_stmt statement)
    {
        var code = sqlite3_reset(statement);
        if (logTrace)
        {
            sqlite3_clear_bindings(statement);
            logger.LogTrace($"Reset\n{code}");
        }

        return code;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private int Bind(sqlite3_stmt statement, int index, ReadOnlySpan<char> value)
    {
        var code = sqlite3_bind_text16(statement, index, value);
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
        if (logTrace)
        {
            logger.LogTrace($"Bind {index} Code: {code} DateTime: {value}");
        }

        return code;
    }

#pragma warning restore CA2254

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static ulong CUI64(sqlite3_stmt statement, int index) => (ulong)sqlite3_column_int64(statement, index);

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

    private void ColumnUser(User user, sqlite3_stmt statement, int offset)
    {
        user.Name = CStr(statement, offset++);
        user.Account = CStr(statement, offset++);
        user.IsFollowed = CBool(statement, offset++);
        user.IsMuted = CBool(statement, offset++);
        user.IsOfficiallyRemoved = CBool(statement, offset++);
        user.ExtraHideReason = (HideReason)CI32(statement, offset++);
        user.ImageUrls = CStr(statement, offset++);
        user.Comment = CStr(statement, offset++);
        user.ExtraMemo = CStr(statement, offset++);
        if (CBool(statement, offset))
        {
            ColumnUserDetail(user);
        }
    }

    private void ColumnUserDetail(User user)
    {
        getUserDetailStatement ??= Prepare("SELECT \"Profile_Webpage\", \"Profile_Gender\", \"Profile_Birth\", \"Profile_BirthYear\", \"Profile_BirthDay\", \"Profile_Region\", \"Profile_AddressId\", \"Profile_CountryCode\", \"Profile_Job\", \"Profile_JobId\", \"Profile_TotalFollowUsers\", \"Profile_TotalIllusts\", \"Profile_TotalManga\", \"Profile_TotalNovels\", \"Profile_TotalIllustBookmarksPublic\", \"Profile_TotalIllustSeries\", \"Profile_TotalNovelSeries\", \"Profile_BackgroundImageUrl\", \"Profile_TwitterAccount\", \"Profile_TwitterUrl\", \"Profile_PawooUrl\", \"Profile_IsPremium\", \"Profile_IsUsingCustomProfileImage\", \"ProfilePublicity_Gender\", \"ProfilePublicity_Region\", \"ProfilePublicity_BirthDay\", \"ProfilePublicity_BirthYear\", \"ProfilePublicity_Job\", \"ProfilePublicity_Pawoo\", \"Workspace_Pc\", \"Workspace_Monitor\", \"Workspace_Tool\", \"Workspace_Scanner\", \"Workspace_Tablet\", \"Workspace_Mouse\", \"Workspace_Printer\", \"Workspace_Desktop\", \"Workspace_Music\", \"Workspace_Desk\", \"Workspace_Chair\", \"Workspace_Comment\", \"Workspace_WorkspaceImageUrl\" FROM \"UserDetailTable\" WHERE \"Id\" = ?", true, out _);
        Bind(getUserDetailStatement, 1, user.Id);
        if (Step(getUserDetailStatement) != SQLITE_ROW)
        {
            goto RETURN;
        }

        ref var profile = ref user.Profile;
        profile ??= new();
        profile.Webpage = CStr(getUserDetailStatement, 0);
        profile.Gender = CStr(getUserDetailStatement, 1);
        profile.Birth = CStr(getUserDetailStatement, 2);
        profile.BirthYear = CU32(getUserDetailStatement, 3);
        profile.BirthDay = CStr(getUserDetailStatement, 4);
        profile.Region = CStr(getUserDetailStatement, 5);
        profile.AddressId = CI64(getUserDetailStatement, 6);
        profile.CountryCode = CStr(getUserDetailStatement, 7);
        profile.Job = CStr(getUserDetailStatement, 8);
        profile.JobId = CI64(getUserDetailStatement, 9);
        profile.TotalFollowUsers = CUI64(getUserDetailStatement, 10);
        profile.TotalIllusts = CUI64(getUserDetailStatement, 11);
        profile.TotalManga = CUI64(getUserDetailStatement, 12);
        profile.TotalNovels = CUI64(getUserDetailStatement, 13);
        profile.TotalIllustBookmarksPublic = CUI64(getUserDetailStatement, 14);
        profile.TotalIllustSeries = CUI64(getUserDetailStatement, 15);
        profile.TotalNovelSeries = CUI64(getUserDetailStatement, 16);
        profile.BackgroundImageUrl = CStr(getUserDetailStatement, 17);
        profile.TwitterAccount = CStr(getUserDetailStatement, 18);
        profile.TwitterUrl = CStr(getUserDetailStatement, 19);
        profile.PawooUrl = CStr(getUserDetailStatement, 20);
        profile.IsPremium = CBool(getUserDetailStatement, 21);
        profile.IsUsingCustomProfileImage = CBool(getUserDetailStatement, 22);

        ref var publicity = ref user.ProfilePublicity;
        publicity ??= new();
        publicity.Gender = CStr(getUserDetailStatement, 23);
        publicity.Region = CStr(getUserDetailStatement, 24);
        publicity.BirthDay = CStr(getUserDetailStatement, 25);
        publicity.BirthYear = CStr(getUserDetailStatement, 26);
        publicity.Job = CStr(getUserDetailStatement, 27);
        publicity.Pawoo = CBool(getUserDetailStatement, 28);

        ref var workspace = ref user.Workspace;
        workspace ??= new();
        workspace.Pc = CStr(getUserDetailStatement, 29);
        workspace.Monitor = CStr(getUserDetailStatement, 30);
        workspace.Tool = CStr(getUserDetailStatement, 31);
        workspace.Scanner = CStr(getUserDetailStatement, 32);
        workspace.Tablet = CStr(getUserDetailStatement, 33);
        workspace.Mouse = CStr(getUserDetailStatement, 34);
        workspace.Printer = CStr(getUserDetailStatement, 35);
        workspace.Desktop = CStr(getUserDetailStatement, 36);
        workspace.Music = CStr(getUserDetailStatement, 37);
        workspace.Desk = CStr(getUserDetailStatement, 38);
        workspace.Chair = CStr(getUserDetailStatement, 39);
        workspace.Comment = CStr(getUserDetailStatement, 40);
        workspace.WorkspaceImageUrl = CStr(getUserDetailStatement, 41);

    RETURN:
        Reset(getUserDetailStatement);
    }

    private const string EnumerableArtworkQuery = "SELECT \"Origin\".\"Id\", \"Origin\".\"UserId\", \"Origin\".\"PageCount\", \"Origin\".\"Width\", \"Origin\".\"Height\", \"Origin\".\"Type\", \"Origin\".\"Extension\", \"Origin\".\"IsXRestricted\", \"Origin\".\"IsVisible\", \"Origin\".\"IsMuted\", \"Origin\".\"CreateDate\", \"Origin\".\"FileDate\", \"Origin\".\"TotalView\", \"Origin\".\"TotalBookmarks\", \"Origin\".\"HideReason\", \"Origin\".\"IsOfficiallyRemoved\", \"Origin\".\"IsBookmarked\", \"Origin\".\"Title\", \"Origin\".\"Caption\", \"Origin\".\"Memo\" FROM \"ArtworkTable\" AS \"Origin\"";
    private const string EnumerableUserQuery = "SELECT \"Origin\".\"Id\", \"Origin\".\"Name\", \"Origin\".\"Account\", \"Origin\".\"IsFollowed\", \"Origin\".\"IsMuted\", \"Origin\".\"IsOfficiallyRemoved\", \"Origin\".\"HideReason\", \"Origin\".\"ImageUrls\", \"Origin\".\"Comment\", \"Origin\".\"Memo\", \"Origin\".\"HasDetail\" FROM \"UserTable\" AS \"Origin\"";

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
                    statement = Prepare("SELECT (\"Major\", \"Minor\") FROM \"InfoTable\" ORDER BY \"Major\" DESC, \"Minor\" DESC LIMIT 1", false, out _);
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

    private ValueTask InsertAsync(Artwork answer, CancellationToken token)
    {
        sqlite3_stmt PrepareStatement(int count)
        {
            ref var statement = ref At(ref insertArtworkStatementArray, count);
            if (statement is null)
            {
                var builder = ZString.CreateUtf8StringBuilder();
                builder.Append("BEGIN IMMEDIATE TRANSACTION; INSERT INTO \"ArtworkTable\"" +
                    " VALUES (?1, ?2, ?3, ?4, ?5, ?6, ?7, ?8, ?9, ?10, ?11, ?12, ?13, ?14, ?15, ?16, ?17, ?18, ?19, ?20) " +
                    "ON CONFLICT (\"Id\") DO UPDATE SET \"IsVisible\" = \"excluded\".\"IsVisible\", \"IsMuted\" = \"exluded\".\"IsMuted\", " +
                        "\"CreateDate\" = \"exluded\".\"CreateDate\", \"FileDate\" = \"exluded\".\"FileDate\", \"TotalView\" = \"excluded\".\"TotalView\"," +
                        "\"TotalBookmarks\" = \"excluded\".\"TotalBookmarks\", \"HideReason\" = \"excluded\".\"HideReason\", " +
                        "\"IsOfficiallyRemoved\" = \"excluded\".\"IsOfficiallyRemoved\", \"IsBookmarked\" = \"excluded\".\"IsBookmarked\", \"Title\" = \"excluded\".\"Title\"," +
                        "\"Caption\" = \"excluded\".\"Caption\", \"Memo\" = \"excluded\".\"Memo\";");
                ProcessTag(ref builder, "ArtworkTagTable", 0x14, count);
                builder.Append(";END TRANSACTION;");
                builder.Dispose();
                statement = Prepare(ref builder, false, out _);
                builder.Dispose();
            }

            return statement;
        }

        var tagDictionary = answer.CalculateTags();
        var statement = PrepareStatement(tagDictionary.Count);
        var offset = 0x14;
        Bind(statement, 0x01, answer.Id);
        Bind(statement, 0x02, answer.UserId);
        Bind(statement, 0x03, answer.PageCount);
        Bind(statement, 0x04, answer.Width);
        Bind(statement, 0x05, answer.Height);
        Bind(statement, 0x06, answer.Type);
        Bind(statement, 0x07, answer.Extension);
        Bind(statement, 0x08, answer.IsXRestricted);
        Bind(statement, 0x09, answer.IsVisible);
        Bind(statement, 0x0a, answer.IsMuted);
        Bind(statement, 0x0b, answer.CreateDate);
        Bind(statement, 0x0c, answer.FileDate);
        Bind(statement, 0x0d, answer.TotalView);
        Bind(statement, 0x0e, answer.TotalBookmarks);
        Bind(statement, 0x0f, answer.ExtraHideReason);
        Bind(statement, 0x10, answer.IsOfficiallyRemoved);
        Bind(statement, 0x11, answer.IsBookmarked);
        Bind(statement, 0x12, answer.Title);
        Bind(statement, 0x13, answer.Caption);
        Bind(statement, 0x14, answer.ExtraMemo);
        foreach (var (tagId, tagKind) in tagDictionary)
        {
            Bind(statement, ++offset, tagId);
            Bind(statement, ++offset, tagKind);
        }

        Step(statement);
        statement.manual_close();
        return ValueTask.CompletedTask;
    }

    private async ValueTask UpdateAsync(Artwork answer, CancellationToken token)
    {
        sqlite3_stmt PrepareStatement(int count)
        {
            ref var statement = ref At(ref updateArtworkStatementArray, count);
            if (statement is not null)
            {
                return statement;
            }

            var builder = ZString.CreateUtf8StringBuilder();
            builder.Append("BEGIN IMMEDIATE TRANSACTION;DELETE FROM \"ArtworkTagCrossTable\" WHERE \"Id\" = ?1;UPDATE OR IGNORE \"ArtworkTable\" SET (\"IsVisible\", \"IsMuted\", \"CreateDate\", \"FileDate\", \"TotalView\", \"TotalBookmarks\", \"HideReason\", \"IsOfficiallyRemoved\", \"IsBookmarked\", \"Title\", \"Caption\", \"Memo\") = (?2, ?3, ?4, ?5, ?6, ?7, ?8, ?9, ?10, ?11, ?12, ?13) WHERE \"Id\" = ?1");
            ProcessTag(ref builder, "ArtworkTagCrossTable", 0xd, count);
            builder.Append(";END TRANSACTION;");
            statement = Prepare(ref builder, true, out _);
            builder.Dispose();
            return statement;
        }

        var tagDictionary = answer.CalculateTags();
        var offset = 0xd;
        var statement = PrepareStatement(tagDictionary.Count);
        Bind(statement, 0x1, answer.Id);
        Bind(statement, 0x2, answer.IsVisible);
        Bind(statement, 0x3, answer.IsVisible);
        Bind(statement, 0x4, answer.CreateDate);
        Bind(statement, 0x5, answer.FileDate);
        Bind(statement, 0x6, answer.TotalView);
        Bind(statement, 0x7, answer.TotalBookmarks);
        Bind(statement, 0x8, answer.ExtraHideReason);
        Bind(statement, 0x9, answer.IsOfficiallyRemoved);
        Bind(statement, 0xa, answer.IsBookmarked);
        Bind(statement, 0xb, answer.Title);
        Bind(statement, 0xc, answer.Caption);
        Bind(statement, 0xd, answer.ExtraMemo);
        foreach (var item in tagDictionary)
        {
            Bind(statement, ++offset, item.Key);
            Bind(statement, ++offset, item.Value);
        }

        try
        {
            while (Step(statement) == SQLITE_BUSY)
            {
                if (token.IsCancellationRequested)
                {
                    break;
                }

                await Task.Delay(TimeSpan.FromSeconds(1d), token).ConfigureAwait(false);
            }
        }
        finally
        {
            Reset(statement);
        }
    }

    /// <summary>
    /// ?1: Id
    /// </summary>
    /// <param name="builder"></param>
    /// <param name="table"></param>
    /// <param name="offset">VALUES ($1, ?++offset, ?++offset)</param>
    /// <param name="count"></param>
    private static void ProcessTag(ref Utf8ValueStringBuilder builder, string table, int offset, int count)
    {
        if (count == 0)
        {
            return;
        }

        builder.Append("INSERT INTO \"");
        builder.Append(table);
        builder.Append("\" VALUES (?1, ?");
        builder.Append(++offset);
        builder.Append(", ?");
        builder.Append(++offset);
        for (var i = 1; i < count; i++)
        {
            builder.Append("), (?1, ?");
            builder.Append(++offset);
            builder.Append(", ?");
            builder.Append(++offset);
        }

        builder.AppendAscii(')');
    }

    public async ValueTask<bool> AddOrUpdateAsync(ulong id, Func<CancellationToken, ValueTask<User>> add, Func<User, CancellationToken, ValueTask> update, CancellationToken token)
    {
        if (id == 0)
        {
            return false;
        }

        var answer = await GetUserAsync(id, token).ConfigureAwait(false);
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

    private async ValueTask UpdateAsync(User answer, CancellationToken token)
    {
        var tagCount = answer.ExtraTags?.Length ?? 0;
        sqlite3_stmt PrepareStatement(int tagCount)
        {
            ref var statement = ref Unsafe.NullRef<sqlite3_stmt>();
            if (answer is { ProfilePublicity: null, Profile: null, Workspace: null })
            {
                statement = ref At(ref updateUserStatementArray, tagCount);
                if (statement is null)
                {
                    var builder = ZString.CreateUtf8StringBuilder();
                    builder.Append("BEGIN IMMEDIATE TRANSACTION;" +
                        "UPDATE \"UserTable\" SET \"Name\" = ?2, \"Account\" = ?3, \"IsFollowed\" = ?4, \"IsMuted\" = ?5, " +
                            "\"IsOfficiallyRemoved\" = ?6, \"HideReason\" = ?7, \"ImageUrls\" = ?8, \"Comment\" = ?9, \"Memo\" = ?10, \"HasDetail\" = 0 WHERE \"Id\" = ?1;" +
                        "DELETE FROM \"UserDetailTable\" WHERE \"Id\" = ?1;" +
                        "DELETE FROM \"UserTagCrossTable\" WHERE \"Id\" = ?1;");
                    ProcessTag(ref builder, "UserTagCrossTable", 10, tagCount);
                    builder.Append("END TRANSACTION;");
                    statement = Prepare(ref builder, true, out _);
                    builder.Dispose();
                }
            }
            else
            {
                statement = ref At(ref updateUserDetailStatementArray, tagCount);
                if (statement is null)
                {
                    var builder = ZString.CreateUtf8StringBuilder();
                    builder.Append("BEGIN IMMEDIATE TRANSACTION;" +
                        "UPDATE \"UserTable\" SET \"Name\" = ?2, \"Account\" = ?3, \"IsFollowed\" = ?4, \"IsMuted\" = ?5, " +
                            "\"IsOfficiallyRemoved\" = ?6, \"HideReason\" = ?7, \"ImageUrls\" = ?8, \"Comment\" = ?9, \"Memo\" = ?10, \"HasDetail\" = 1 WHERE \"Id\" = ?1;" +
                        "INSERT OR REPLACE INTO \"UserDetailTable\" VALUES (?1, " +
                                 "?11, ?12, ?13, ?14, ?15, ?16, ?17, ?18, ?19, " +
                            "?20, ?21, ?22, ?23, ?24, ?25, ?26, ?27, ?28, ?29, " +
                            "?30, ?31, ?32, ?33, ?34, ?35, ?36, ?37, ?38, ?39, " +
                            "?40, ?41, ?42, ?43, ?44, ?45, ?46, ?47, ?48, ?49, " +
                            "?50, ?51, ?52);" +
                        "DELETE FROM \"UserTagCrossTable\" WHERE \"Id\" = ?1;");
                    ProcessTag(ref builder, "UserTagCrossTable", 52, tagCount);
                    builder.Append("END TRANSACTION;");
                    statement = Prepare(ref builder, true, out _);
                    builder.Dispose();
                }
            }

            return statement;
        }

        var statement = PrepareStatement(tagCount);
        BindUser(statement, answer);

        try
        {
            while (Step(statement) == SQLITE_BUSY)
            {
                await Task.Delay(TimeSpan.FromSeconds(1d), token).ConfigureAwait(false);
            }
        }
        finally
        {
            Reset(statement);
        }
    }

    private async ValueTask InsertAsync(User answer, CancellationToken token)
    {
        var tagCount = answer.ExtraTags?.Length ?? 0;
        sqlite3_stmt PrepareStatement(int tagCount)
        {
            var hasDetail = answer is { ProfilePublicity: null, Profile: null, Workspace: null };
            ref var statement = ref At(ref hasDetail ? ref insertUserStatementArray : ref insertUserDetailStatementArray, tagCount);
            if (statement is null)
            {
                var builder = ZString.CreateUtf8StringBuilder();
                builder.Append("BEGIN IMMEDIATE TRANSACTION;" +
                        "INSERT INTO \"UserTable\" VALUES (?1, ?2, ?3, ?4, ?5, ?6, ?7, ?8, ?9, ?10, ");
                int offset;
                if (hasDetail)
                {
                    builder.Append("1);INSERT OR REPLACE INTO \"UserDetailTable\" VALUES (?1, " +
                                 "?11, ?12, ?13, ?14, ?15, ?16, ?17, ?18, ?19, " +
                            "?20, ?21, ?22, ?23, ?24, ?25, ?26, ?27, ?28, ?29, " +
                            "?30, ?31, ?32, ?33, ?34, ?35, ?36, ?37, ?38, ?39, " +
                            "?40, ?41, ?42, ?43, ?44, ?45, ?46, ?47, ?48, ?49, " +
                            "?50, ?51, ?52);");
                    offset = 52;
                }
                else
                {
                    builder.Append("0);");
                    offset = 10;
                }

                ProcessTag(ref builder, "UserTagCrossTable", offset, tagCount);
                builder.Append("END TRANSACTION;");
                statement = Prepare(ref builder, true, out _);
                builder.Dispose();
            }

            return statement;
        }

        var statement = PrepareStatement(tagCount);
        BindUser(statement, answer);

        try
        {
            while (Step(statement) == SQLITE_BUSY)
            {
                if (token.IsCancellationRequested)
                {
                    break;
                }

                await Task.Delay(TimeSpan.FromSeconds(1d), token).ConfigureAwait(false);
            }
        }
        finally
        {
            Reset(statement);
        }
    }

    private void BindUser(sqlite3_stmt statement, User answer)
    {
        Bind(statement, 0x01, answer.Id);
        Bind(statement, 0x02, answer.Name);
        Bind(statement, 0x03, answer.Account);
        Bind(statement, 0x04, answer.IsFollowed);
        Bind(statement, 0x05, answer.IsMuted);
        Bind(statement, 0x06, answer.IsOfficiallyRemoved);
        Bind(statement, 0x07, answer.ExtraHideReason);
        Bind(statement, 0x08, answer.ImageUrls);
        Bind(statement, 0x09, answer.Comment);
        Bind(statement, 0x0a, answer.ExtraMemo);

        var offset = 0x0a;
        if (answer.Profile is { } profile)
        {
            offset = 0x34;
            Bind(statement, 0x0b, profile.Webpage);
            Bind(statement, 0x0c, profile.Gender);
            Bind(statement, 0x0d, profile.Birth);
            Bind(statement, 0x0e, profile.BirthYear);
            Bind(statement, 0x0f, profile.BirthDay);
            Bind(statement, 0x10, profile.Region);
            Bind(statement, 0x11, profile.AddressId);
            Bind(statement, 0x12, profile.CountryCode);
            Bind(statement, 0x13, profile.Job);
            Bind(statement, 0x14, profile.JobId);
            Bind(statement, 0x15, profile.TotalFollowUsers);
            Bind(statement, 0x16, profile.TotalIllusts);
            Bind(statement, 0x17, profile.TotalManga);
            Bind(statement, 0x18, profile.TotalNovels);
            Bind(statement, 0x19, profile.TotalIllustBookmarksPublic);
            Bind(statement, 0x1a, profile.TotalIllustSeries);
            Bind(statement, 0x1b, profile.TotalNovelSeries);
            Bind(statement, 0x1c, profile.BackgroundImageUrl);
            Bind(statement, 0x1d, profile.TwitterAccount);
            Bind(statement, 0x1e, profile.TwitterUrl);
            Bind(statement, 0x1f, profile.PawooUrl);
            Bind(statement, 0x20, profile.IsPremium);
            Bind(statement, 0x21, profile.IsUsingCustomProfileImage);
        }

        if (answer.ProfilePublicity is { } publicity)
        {
            offset = 0x34;
            Bind(statement, 0x22, publicity.Gender);
            Bind(statement, 0x23, publicity.Region);
            Bind(statement, 0x24, publicity.BirthDay);
            Bind(statement, 0x25, publicity.BirthYear);
            Bind(statement, 0x26, publicity.Job);
            Bind(statement, 0x27, publicity.Pawoo);
        }

        if (answer.Workspace is { } workspace)
        {
            offset = 0x34;
            Bind(statement, 0x28, workspace.Pc);
            Bind(statement, 0x29, workspace.Monitor);
            Bind(statement, 0x2a, workspace.Tool);
            Bind(statement, 0x2b, workspace.Scanner);
            Bind(statement, 0x2c, workspace.Tablet);
            Bind(statement, 0x2d, workspace.Mouse);
            Bind(statement, 0x2e, workspace.Printer);
            Bind(statement, 0x2f, workspace.Desktop);
            Bind(statement, 0x30, workspace.Music);
            Bind(statement, 0x31, workspace.Desk);
            Bind(statement, 0x32, workspace.Chair);
            Bind(statement, 0x33, workspace.Comment);
            Bind(statement, 0x34, workspace.WorkspaceImageUrl);
        }

        foreach (var tag in answer.ExtraTags.AsSpan())
        {
            Bind(statement, ++offset, tag);
            Bind(statement, ++offset, 1);
        }
    }

    public async ValueTask AddOrUpdateRankingAsync(DateOnly date, RankingKind kind, ulong[] values, CancellationToken token)
    {
        if (values.Length == 0)
        {
            return;
        }

        sqlite3_stmt PrepareStatement(int length)
        {
            ref var statement = ref At(ref addOrUpdateRankingStatementArray, length);
            if (statement is null)
            {
                var builder = ZString.CreateUtf8StringBuilder();
                builder.Append("INSERT INTO \"RankingTable\" VALUES (?1, ?2, ?3, ?4");
                for (int i = 1, offset = 4; i < length; i++)
                {
                    builder.Append("), (?1, ?2, ?");
                    builder.Append(++offset);
                    builder.Append(", ?");
                    builder.Append(++offset);
                }

                builder.Append(") ON CONFLICT (\"Date\", \"RankingKind\", \"Index\") DO UPDATE SET \"Id\" = \"excluded\".\"Id\";");
                statement = Prepare(ref builder, true, out _);
                builder.Dispose();
            }

            return statement;
        }

        var statement = PrepareStatement(values.Length);
        Bind(statement, 1, date);
        Bind(statement, 2, kind);
        for (int i = 0, offset = 2; i < values.Length; i++)
        {
            Bind(statement, ++offset, i);
            Bind(statement, ++offset, values[i]);
        }

        try
        {
            while (Step(statement) == SQLITE_BUSY)
            {
                await Task.Delay(TimeSpan.FromSeconds(1d), token).ConfigureAwait(false);
            }
        }
        finally
        {
            Reset(statement);
        }
    }

    private static void PrepareStringSet(ref Utf8ValueStringBuilder builder, string table, int offset, int count)
    {
        builder.Append("INSERT OR IGNORE INTO \"");
        builder.Append(table);
        builder.Append("\" (\"Value\") VALUES (?");
        builder.Append(offset);
        for (var i = 1; i < count; i++)
        {
            builder.Append(", ?");
        }

        builder.Append(");");
    }

    public async ValueTask<bool> ArtworkAddOrUpdateAsync(ArtworkResponseContent source, CancellationToken token)
    {
        sqlite3_stmt PrepareStatement(int tagLength, int toolLength)
        {
            ref var statement = ref CollectionsMarshal.GetValueRefOrAddDefault(artworkAddOrUpdateStatementDictionary ??= new(), (tagLength, toolLength), out _);
            if (statement is null)
            {
                var builder = ZString.CreateUtf8StringBuilder();
                builder.Append("BEGIN IMMEDIATE TRANSACTION;" +
                    "CREATE TEMP TABLE IF NOT EXISTS \"LAST_INSERT_ROWID_TEMP_TABLE\" (\"Id\" INTEGER NOT NULL PRIMARY KEY) STRICT;" +
                    "DELETE FROM \"LAST_INSERT_ROWID_TEMP_TABLE\";" +
                    "INSERT INTO \"ArtworkTable\" VALUES " +
                        "(?1, ?2, ?3, ?4, ?5, ?6, ?7, ?8, ?9, 0, ?10, ?11, ?12, ?13, ?14, 0, ?15, ?16, ?17, NULL) " +
                        "ON CONFLICT (\"Origin\".\"Id\") DO UPDATE SET \"IsVisible\" = \"excluded\".\"IsVisible\"," +
                        "\"IsMuted\" = \"excluded\".\"IsMuted\", \"CreateDate\" = \"excluded\".\"CreateDate\"," +
                        "\"FileDate\" = \"excluded\".\"FileDate\", \"TotalView\" = \"excluded\".\"TotalView\"," +
                        "\"TotalBookmarks\" = \"excluded\".\"TotalBookmarks\", \"IsOfficiallyRemoved\" = \"excluded\".\"IsOfficiallyRemoved\"," +
                        " \"IsBookmarked\" = \"excluded\".\"IsBookmarked\"," +
                        "\"Title\" = \"excluded\".\"Title\", \"Caption\" = \"excluded\".\"Caption\";" +
                    "INSERT INTO \"LAST_INSERT_ROWID_TEMP_TABLE\" VALUES (last_insert_rowid());");

                const int ConstOffset = 18;
                if (tagLength > 0)
                {
                    PrepareStringSet(ref builder, "TagTable", ConstOffset, tagLength);
                    builder.Append("DELETE FROM \"ArtworkTagCrossTable\" WHERE \"Id\" = ?1 AND \"ValueKind\" = 1;INSERT INTO \"ArtworkTagCrossTable\" (SELECT ?1, \"Id\", 1 FROM \"TagTable\" WHERE \"Value\" IN (?");
                    builder.Append(ConstOffset);
                    for (int i = 0, offset = ConstOffset; i < tagLength; i++)
                    {
                        builder.Append(", ?");
                        builder.Append(++offset);
                    }

                    builder.Append(")) ON CONFLICT (\"Id\", \"TagId\") WHERE \"ValueKind\" = 2 DO UPDATE SET \"ValueKind\" = 1 ON CONFLICT (\"Id\", \"TagId\") WHERE \"ValueKind\" = 0 DO NOTHING;");
                }

                if (toolLength > 0)
                {
                    PrepareStringSet(ref builder, "ToolTable", ConstOffset + tagLength, toolLength);
                    builder.Append("DELETE FROM \"ArtworkToolCrossTable\" WHERE \"Id\" = ?1;INSERT OR IGNORE INTO \"ArtworkToolCrossTable\" (SELECT ?1, \"Id\" FROM \"ToolTable\" WHERE \"Value\" IN (?");
                    builder.Append(ConstOffset + tagLength);
                    for (int i = 0, offset = ConstOffset + tagLength; i < toolLength; i++)
                    {
                        builder.Append(", ?");
                        builder.Append(++offset);
                    }

                    builder.Append("));");
                }

                builder.Append("SELECT \"Id\" = ?1 FROM \"LAST_INSERT_ROWID_TEMP_TABLE\" LIMIT 1; END TRANSACTION;");
                statement = Prepare(ref builder, true, out _);
                builder.Dispose();
            }

            return statement;
        }

        var tagLength = source.Tags?.Length ?? 0;
        var statement = PrepareStatement(tagLength, source.Tools?.Length ?? 0);
        Bind(statement, 0x01, source.Id);
        Bind(statement, 0x02, source.User.Id);
        Bind(statement, 0x03, source.PageCount);
        Bind(statement, 0x04, source.Width);
        Bind(statement, 0x05, source.Height);
        Bind(statement, 0x06, source.Type);
        Bind(statement, 0x07, LocalNetworkConverter.ConvertToFileExtensionKind(source));
        Bind(statement, 0x08, source.XRestrict != 0);
        Bind(statement, 0x09, source.Visible);
        Bind(statement, 0x0a, source.IsMuted);
        Bind(statement, 0x0b, source.CreateDate);
        Bind(statement, 0x0c, LocalNetworkConverter.ParseFileDate(source));
        Bind(statement, 0x0d, source.TotalView);
        Bind(statement, 0x0e, source.TotalBookmarks);
        Bind(statement, 0x0f, source.IsBookmarked);
        Bind(statement, 0x10, source.Title);
        Bind(statement, 0x11, source.Caption);

        if (source.Tags is { Length: > 0 } tags)
        {
            for (int i = 0, offset = 0x11; i < tags.Length; i++)
            {
                Bind(statement, ++offset, tags[i].Name);
            }
        }

        if (source.Tools is { Length: > 0 } tools)
        {
            for (int i = 0, offset = 0x11 + tagLength; i < tools.Length; i++)
            {
                Bind(statement, ++offset, tools[i]);
            }
        }

        try
        {
            int code;
            while ((code = Step(statement)) == SQLITE_BUSY)
            {
                await Task.Delay(TimeSpan.FromSeconds(1d), token).ConfigureAwait(false);
            }

            if (code == SQLITE_ROW)
            {
                return CBool(statement, 0);
            }
            else
            {
                throw new InvalidProgramException(code.ToString());
            }
        }
        finally
        {
            Reset(statement);
        }
    }

    private ulong SimpleCount([NotNull] ref sqlite3_stmt? statement, string table)
    {
        statement ??= Prepare($"SELECT COUNT(*) FROM \"{table}Table\"", true, out _);
        var answer = Step(statement) == SQLITE_ROW ? (ulong)sqlite3_column_int64(statement, 0) : 0;
        Reset(statement);
        return answer;
    }

    public ValueTask<ulong> CountArtworkAsync(CancellationToken token) => ValueTask.FromResult(SimpleCount(ref countArtworkStatement, "ArtworkConcrete"));

    /// <summary>
    /// Ignore Count, Offset and FileExistanceFilter when FileExistanceFilter exists.
    /// </summary>
    /// <param name="filter"></param>
    /// <param name="token"></param>
    /// <returns></returns>
    public ValueTask<ulong> CountArtworkAsync(ArtworkFilter filter, CancellationToken token)
    {
        var statement = ArtworkFilterUtility.CreateStatement(database, "SELECT COUNT(*) FROM \"ArtworkTable\" AS \"Origin\" WHERE ", filter);
        var answer = Step(statement) == SQLITE_ROW ? (ulong)sqlite3_column_int64(statement, 0) : 0;
        statement.manual_close();
        return ValueTask.FromResult(answer);
    }

    public ValueTask<ulong> CountRankingAsync(CancellationToken token) => ValueTask.FromResult(SimpleCount(ref countRankingStatement, "Ranking"));

    public ValueTask<ulong> CountTagAsync(CancellationToken token) => ValueTask.FromResult(SimpleCount(ref countTagStatement, "Tag"));

    public ValueTask<ulong> CountToolAsync(CancellationToken token) => ValueTask.FromResult(SimpleCount(ref countToolStatement, "Tool"));

    public ValueTask<ulong> CountUserAsync(CancellationToken token) => ValueTask.FromResult(SimpleCount(ref countUserStatement, "User"));

#pragma warning disable CS1998
    public async IAsyncEnumerable<Artwork> EnumerableArtworkAsync([EnumeratorCancellation] CancellationToken token)
#pragma warning restore CS1998
    {
        enumerableArtworkStatement ??= Prepare(EnumerableArtworkQuery, true, out _);
        while (Step(enumerableArtworkStatement) == SQLITE_ROW)
        {
            if (token.IsCancellationRequested)
            {
                break;
            }

            var id = CUI64(enumerableArtworkStatement, 0);
            if (id == 0)
            {
                continue;
            }

            var answer = new Artwork
            {
                Id = id,
            };

            ColumnArtwork(answer, enumerableArtworkStatement, 1);
            yield return answer;
        }

        Reset(enumerableArtworkStatement);
    }

#pragma warning disable CS1998
    public async IAsyncEnumerable<User> EnumerableUserAsync([EnumeratorCancellation] CancellationToken token)
#pragma warning restore CS1998
    {
        enumerableUserStatement ??= Prepare(EnumerableUserQuery, true, out _);
        while (Step(enumerableUserStatement) == SQLITE_ROW)
        {
            if (token.IsCancellationRequested)
            {
                break;
            }

            var id = CUI64(enumerableUserStatement, 0);
            if (id == 0)
            {
                continue;
            }

            var answer = new User()
            {
                Id = id,
            };
            ColumnUser(answer, enumerableUserStatement, 1);
            yield return answer;
        }

        Reset(enumerableUserStatement);
    }

#pragma warning disable CS1998
    public async IAsyncEnumerable<uint> EnumeratePartialMatchAsync(string key, [EnumeratorCancellation] CancellationToken token)
#pragma warning restore CS1998
    {
        if (key.Length == 0)
        {
            yield break;
        }

        sqlite3_stmt? statement = null;
        try
        {
            statement = Prepare(key.Length >= 3
                ? $"SELECT \"Id\" FROM \"TagTextTable\" ('{key}')"
                : $"SELECT \"Id\" FROM \"TagTextTable\" WHERE \"Value\" LIKE '%{key}%'", false, out _);
            while (!token.IsCancellationRequested && Step(statement) == SQLITE_ROW)
            {
                yield return CU32(statement, 0);
            }
        }
        finally
        {
            statement?.manual_close();
        }
    }

#pragma warning disable CS1998
    /// <summary>
    /// When FileExistanceFilter exists, ignore Offset, Count and FileExistanceFilter
    /// </summary>
    /// <param name="filter"></param>
    /// <param name="token"></param>
    /// <returns></returns>
    public async IAsyncEnumerable<Artwork> FilterAsync(ArtworkFilter filter, [EnumeratorCancellation] CancellationToken token)
#pragma warning restore CS1998
    {
        if (token.IsCancellationRequested)
        {
            yield break;
        }

        var statement = ArtworkFilterUtility.CreateStatement(database, EnumerableArtworkQuery + " WHERE ", filter);
        while (Step(statement) == SQLITE_ROW)
        {
            if (token.IsCancellationRequested)
            {
                break;
            }

            var id = CUI64(statement, 0);
            if (id == 0)
            {
                continue;
            }

            var answer = new Artwork
            {
                Id = id,
            };

            ColumnArtwork(answer, statement, 1);
            yield return answer;
        }

        statement.manual_close();
    }

#pragma warning disable CS1998
    public async IAsyncEnumerable<User> FilterAsync(UserFilter filter, [EnumeratorCancellation] CancellationToken token)
#pragma warning restore CS1998
    {
        if (token.IsCancellationRequested)
        {
            yield break;
        }

        var statement = UserFilterUtility.CreateStatement(database, EnumerableUserQuery + " WHERE ", filter);
        while (Step(statement) == SQLITE_ROW)
        {
            if (token.IsCancellationRequested)
            {
                break;
            }

            var id = CUI64(statement, 0);
            if (id == 0)
            {
                continue;
            }
            var answer = new User
            {
                Id = id,
            };

            ColumnUser(answer, statement, 1);
            yield return answer;
        }

        statement.manual_close();
    }

    public ValueTask<uint?> FindTagAsync(string key, CancellationToken token)
    {
        if (key.Length == 0)
        {
            return ValueTask.FromResult<uint?>(0);
        }

        findTagStatement ??= Prepare("SELECT \"Id\" FROM \"TagTable\" WHERE \"Value\" = ?", true, out _);
        _ = Bind(findTagStatement, 1, key);
        if (token.IsCancellationRequested)
        {
            return ValueTask.FromCanceled<uint?>(token);
        }

        var code = Step(findTagStatement);
        uint? answer = code == SQLITE_ROW ? (uint)sqlite3_column_int(findTagStatement, 0) : null;
        _ = Reset(findTagStatement);
        return ValueTask.FromResult(answer);
    }

    public ValueTask<Artwork?> GetArtworkAsync(ulong id, CancellationToken token)
    {
        Artwork? answer = null;
        if (id == 0)
        {
            return ValueTask.FromResult(answer);
        }

        getArtworkStatement ??= Prepare("SELECT \"UserId\", \"PageCount\", \"Width\", \"Height\", \"Type\", \"Extension\", \"IsXRestricted\", \"IsVisible\", \"IsMuted\", \"CreateDate\", \"FileDate\", \"TotalView\", \"TotalBookmarks\", \"HideReason\", \"IsOfficiallyRemoved\", \"IsBookmarked\", \"Title\", \"Caption\", \"Memo\" FROM \"ArtworkTable\" WHERE \"Id\" = ?", true, out _);
        _ = Bind(getArtworkStatement, 1, id);
        if (token.IsCancellationRequested)
        {
            return ValueTask.FromCanceled<Artwork?>(token);
        }


        if (Step(getArtworkStatement) == SQLITE_ROW)
        {
            answer = new Artwork
            {
                Id = id,
            };
            ColumnArtwork(answer, getArtworkStatement, 0);
        }

        Reset(getArtworkStatement);
        return ValueTask.FromResult(answer);
    }

    private Dictionary<uint, HideReason>? ProcessHidePage(ulong id)
    {
        Dictionary<uint, HideReason>? answer = null;
        getArtworkHidePageStatement ??= Prepare("SELECT \"Index\", \"HideReason\" FROM \"HidePageTable\" WHERE \"Id\" = ?", true, out _);
        _ = Bind(getArtworkHidePageStatement, 1, id);

        if (Step(getArtworkHidePageStatement) != SQLITE_ROW)
        {
            goto RETURN;
        }

        answer = new();
        answer.Add(CU32(getArtworkHidePageStatement, 0), (HideReason)CI32(getArtworkHidePageStatement, 1));
        while (Step(getArtworkHidePageStatement) == SQLITE_ROW)
        {
            answer.Add(CU32(getArtworkHidePageStatement, 0), (HideReason)CI32(getArtworkHidePageStatement, 1));
        }

    RETURN:
        Reset(getArtworkHidePageStatement);
        return answer;
    }

    private ushort[]? ProcessUgoiraFrames(ulong id)
    {
        getArtworkUgoiraFramesStatement ??= Prepare("SELECT \"Delay\" FROM \"UgoiraFrameTable\" WHERE \"Id\" = ? ORDER BY \"Index\" ASC", true, out _);
        _ = Bind(getArtworkUgoiraFramesStatement, 1, id);
        var buffer = ArrayPool<ushort>.Shared.Rent(256);
        var count = 0;
        while (Step(getArtworkUgoiraFramesStatement) == SQLITE_ROW)
        {
            if (++count == buffer.Length)
            {
                var tmp = ArrayPool<ushort>.Shared.Rent(buffer.Length << 1);
                buffer.CopyTo(tmp, 0);
                ArrayPool<ushort>.Shared.Return(buffer);
                buffer = tmp;
            }

            buffer[count - 1] = CU16(getArtworkUgoiraFramesStatement, 0);
        }

        Reset(getArtworkUgoiraFramesStatement);
        ushort[]? answer;
        if (count == 0)
        {
            answer = null;
        }
        else
        {
            answer = new ushort[count];
            buffer.AsSpan(0, count).CopyTo(answer);
        }

        ArrayPool<ushort>.Shared.Return(buffer);
        return answer;
    }

    private void ColumnArtwork(Artwork answer, sqlite3_stmt statement, int offset)
    {
        answer.UserId = CUI64(statement, offset++);
        answer.PageCount = CU32(statement, offset++);
        answer.Width = CU32(statement, offset++);
        answer.Height = CU32(statement, offset++);
        answer.Type = (ArtworkType)sqlite3_column_int(statement, offset++);
        answer.Extension = (FileExtensionKind)sqlite3_column_int(statement, offset++);
        answer.IsXRestricted = CBool(statement, offset++);
        answer.IsVisible = CBool(statement, offset++);
        answer.IsMuted = CBool(statement, offset++);

        if (!DateTime.TryParse(CStr(statement, offset++), out answer.CreateDate))
        {
            answer.CreateDate = default;
        }

        if (!DateTime.TryParse(CStr(statement, offset++), out answer.FileDate))
        {
            answer.FileDate = default;
        }

        answer.TotalView = CUI64(statement, offset++);
        answer.TotalBookmarks = CUI64(statement, offset++);
        answer.ExtraHideReason = (HideReason)sqlite3_column_int(statement, offset++);
        answer.IsOfficiallyRemoved = CBool(statement, offset++);
        answer.IsBookmarked = CBool(statement, offset++);

        (answer.Tags, answer.ExtraTags, answer.ExtraFakeTags) = ProcessTag(answer.Id);
        answer.ExtraPageHideReasonDictionary = ProcessHidePage(answer.Id);
        if (answer.Type == ArtworkType.Ugoira)
        {
            answer.UgoiraFrames = ProcessUgoiraFrames(answer.Id);
        }
    }

    [SkipLocalsInit]
    private (uint[] official, uint[]? extra, uint[]? ignore) ProcessTag(ulong id)
    {
        getArtworkTagStatement ??= Prepare("SELECT \"TagId\", \"ValueKind\" FROM \"ArtworkTagCrossTable\" WHERE \"Id\" = ?", true, out _);
        _ = Bind(getArtworkTagStatement, 1, id);

        Span<uint> officialTag = stackalloc uint[16];
        Span<uint> extraTag = stackalloc uint[4];
        Span<uint> ignoreTag = stackalloc uint[16];
        var officialTagCount = 0;
        var extraTagCount = 0;
        var ignoreTagCount = 0;
        while (Step(getArtworkTagStatement) == SQLITE_ROW)
        {
            var tag = CU32(getArtworkTagStatement, 0);
            var kind = CI32(getArtworkTagStatement, 1);
            switch (kind)
            {
#pragma warning disable CA2014
                case 0:
                    if (++ignoreTagCount >= ignoreTag.Length)
                    {
                        Span<uint> temp = stackalloc uint[ignoreTag.Length << 1];
                        ignoreTag.CopyTo(temp);
                        ignoreTag = temp;
                    }

                    ignoreTag[ignoreTagCount - 1] = tag;
                    break;
                case 1:
                    if (++officialTagCount >= officialTag.Length)
                    {
                        Span<uint> temp = stackalloc uint[officialTag.Length << 1];
                        officialTag.CopyTo(temp);
                        officialTag = temp;
                    }

                    officialTag[officialTagCount - 1] = tag;
                    break;
                default:
                    if (++extraTagCount >= extraTag.Length)
                    {
                        Span<uint> temp = stackalloc uint[extraTag.Length << 1];
                        extraTag.CopyTo(temp);
                        extraTag = temp;
                    }

                    extraTag[extraTagCount - 1] = tag;
                    break;
#pragma warning restore CA2014
            }
        }

        Reset(getArtworkTagStatement);

        uint[]? official = null, extra = null, ignore = null;
        if (officialTagCount > 0)
        {
            official = new uint[officialTagCount];
            officialTag[..officialTagCount].CopyTo(official);
        }
        else
        {
            official = Array.Empty<uint>();
        }

        if (extraTagCount > 0)
        {
            extra = new uint[extraTagCount];
            extraTag[..extraTagCount].CopyTo(extra);
        }

        if (ignoreTagCount > 0)
        {
            ignore = new uint[ignoreTagCount];
            ignoreTag[..ignoreTagCount].CopyTo(ignore);
        }

        return (official, extra, ignore);
    }

    [SkipLocalsInit]
    private uint[] ProcessTool(ulong id)
    {
        uint[] tools;
        getArtworkToolStatement ??= Prepare("SELECT \"ToolId\" FROM \"ArtworkToolCrossTable\" WHERE \"Id\" = ?", true, out _);
        _ = Bind(getArtworkToolStatement, 1, id);
        var code = Step(getArtworkToolStatement);
        if (code != SQLITE_ROW)
        {
            tools = Array.Empty<uint>();
            goto RETURN;
        }

        Span<uint> span = stackalloc uint[8];
        span[0] = CU32(getArtworkToolStatement, 0);
        var toolCount = 1;

        while (Step(getArtworkToolStatement) == SQLITE_ROW)
        {
            if (++toolCount >= span.Length)
            {
#pragma warning disable CA2014
                Span<uint> temp = stackalloc uint[span.Length << 1];
                span.CopyTo(temp);
                span = temp;
#pragma warning restore CA2014
            }

            span[toolCount - 1] = CU32(getArtworkToolStatement, 0);
        }

        tools = new uint[toolCount];
        span[..toolCount].CopyTo(tools);

    RETURN:
        Reset(getArtworkToolStatement);
        return tools;
    }

    public ValueTask<ulong[]?> GetRankingAsync(DateOnly date, RankingKind kind, CancellationToken token)
    {
        ulong[]? answer = null;
        getRankingStatement ??= Prepare("SELECT \"Id\" FROM \"RankingTable\" WHERE \"Date\" = ?1 AND \"RankingKind\" = ?2 ORDER BY \"Index\" ASC", true, out _);
        Bind(getRankingStatement, 1, date);
        Bind(getRankingStatement, 2, kind);
        if (Step(getRankingStatement) != SQLITE_OK)
        {
            goto RETURN;
        }

        answer = ArrayPool<ulong>.Shared.Rent(512);
        var count = 1;
        answer[0] = CUI64(getRankingStatement, 0);

        while (Step(getRankingStatement) == SQLITE_OK)
        {
            if (++count == answer.Length)
            {
                var tmp = ArrayPool<ulong>.Shared.Rent(answer.Length << 1);
                answer.CopyTo(tmp, 0);
                ArrayPool<ulong>.Shared.Return(answer);
                answer = tmp;
            }

            answer[count - 1] = CUI64(getRankingStatement, 0);
        }

        {
            var tmp = new ulong[count];
            answer.AsSpan(0, count).CopyTo(tmp);
            ArrayPool<ulong>.Shared.Return(answer);
            answer = tmp;
        }

    RETURN:
        Reset(getRankingStatement);
        return ValueTask.FromResult(answer);
    }

    public ValueTask<string?> GetTagAsync(uint id, CancellationToken token)
    {
        getTagStatement ??= Prepare("SELECT \"Value\" FROM \"TagTable\" WHERE \"Id\" = ?", true, out _);
        Bind(getTagStatement, 1, id);
        if (Step(getTagStatement) == SQLITE_OK)
        {
            return ValueTask.FromResult(CStr(getTagStatement, 0));
        }
        else
        {
            return ValueTask.FromResult<string?>(null);
        }
    }

    public ValueTask<string?> GetToolAsync(uint id, CancellationToken token)
    {
        getToolStatement ??= Prepare("SELECT \"Value\" FROM \"ToolTable\" WHERE \"Id\" = ?", true, out _);
        Bind(getToolStatement, 1, id);
        if (Step(getToolStatement) == SQLITE_OK)
        {
            return ValueTask.FromResult(CStr(getToolStatement, 0));
        }
        else
        {
            return ValueTask.FromResult<string?>(null);
        }
    }

    public ValueTask<User?> GetUserAsync(ulong id, CancellationToken token)
    {
        getUserStatement ??= Prepare("SELECT \"Name\", \"Account\", \"IsFollowed\", \"IsMuted\", \"IsOfficiallyRemoved\", \"HideReason\", \"ImageUrls\", \"Comment\", \"Memo\", \"HasDetail\" FROM \"UserTable\" WHERE \"Id\" = ?", true, out _);
        Bind(getUserStatement, 1, id);
        User? answer = null;
        if (Step(getUserStatement) != SQLITE_ROW)
        {
            goto RETURN;
        }

        answer = new()
        {
            Id = id,
        };

        ColumnUser(answer, getUserStatement, 0);

    RETURN:
        Reset(getUserStatement);
        return ValueTask.FromResult(answer);
    }

    public ValueTask<uint> RegisterTagAsync(string value, CancellationToken token)
    {
        registerTagStatement ??= Prepare("INSERT INTO \"TagTable\" (\"Value\") VALUES (?) ON CONFLICT (\"Value\") DO NOTHING RETURNING \"Id\"", true, out _);
        Bind(registerTagStatement, 1, value);
        if (Step(registerTagStatement) != SQLITE_OK)
        {
            throw new InvalidOperationException();
        }

        var answer = CU32(registerTagStatement, 0);
        Reset(registerTagStatement);
        return new(answer);
    }

    public ValueTask<uint> RegisterToolAsync(string value, CancellationToken token)
    {
        registerToolStatement ??= Prepare("INSERT INTO \"ToolTable\" (\"Value\") VALUES (?) RETURNING \"Id\"", true, out _);
        Bind(registerToolStatement, 1, value);
        if (Step(registerToolStatement) != SQLITE_OK)
        {
            throw new InvalidOperationException();
        }

        var answer = CU32(registerToolStatement, 0);
        Reset(registerToolStatement);
        return new(answer);
    }

    public async ValueTask<bool> UserAddOrUpdateAsync(UserDetailResponseData source, CancellationToken token)
    {
        sqlite3_stmt PrepareStatement()
        {
            ref var statement = ref userDetailAddOrUpdateStatement;
            if (statement is null)
            {
                var builder = ZString.CreateUtf8StringBuilder();
                builder.Append("BEGIN IMMEDIATE TRANSACTION;" +
                    "INSERT OR REPLACE INTO \"UserDetailTable\" VALUES (?1," +
                        "?7, ?8, ?9, ?10, ?11, ?12, ?13, ?14, ?15, ?16, ?17, ?18, ?19, ?20, ?21, ?22, ?23, ?24, ?25, ?26, ?27, ?28, ?29," +
                        "?30, ?31, ?32, ?33, ?34, ?35," +
                        "?36, ?37, ?38, ?39, ?40, ?41, ?42, ?43, ?44, ?45, ?46, ?47, ?48);" +
                    "INSERT INTO \"UserTable\" " +
                        "VALUES (?1, ?2, ?3, ?4, 0, 0, 0, ?5, ?6, NULL, 1) ON CONFLICT (\"Id\") DO UPDATE SET " +
                        "\"Name\" = \"excluded\".\"Name\", \"Account\" = \"excluded\".\"Account\", \"IsFollowed\" = \"excluded\".\"IsFollowed\"," +
                        "\"IsOfficiallyRemoved\" = 0, \"ImageUrls\" = \"excluded\".\"ImageUrls\"," +
                        "\"Comment\" = \"excluded\".\"Comment\", \"HasDetail\" = 1;" +
                    "SELECT last_insert_rowid() = ?1;" +
                    "END TRANSACTION;");
                statement = Prepare(ref builder, true, out _);
                builder.Dispose();
            }

            return statement;
        }

        var statement = PrepareStatement();
        Bind(statement, 0x01, source.User.Id);
        Bind(statement, 0x02, source.User.Name);
        Bind(statement, 0x03, source.User.Account);
        Bind(statement, 0x04, source.User.IsFollowed);
        Bind(statement, 0x05, source.User.ProfileImageUrls.Medium);
        Bind(statement, 0x06, source.User.Comment);

        if (source.Profile.HasValue)
        {
            void BindProfile(sqlite3_stmt statement, int offset, UserDetailProfile source)
            {
                Bind(statement, ++offset, source.Webpage);
                Bind(statement, ++offset, source.Gender);
                Bind(statement, ++offset, source.Birth);
                Bind(statement, ++offset, source.BirthYear);
                Bind(statement, ++offset, source.BirthDay);
                Bind(statement, ++offset, source.Region);
                Bind(statement, ++offset, source.AddressId);
                Bind(statement, ++offset, source.CountryCode);
                Bind(statement, ++offset, source.Job);
                Bind(statement, ++offset, source.JobId);
                Bind(statement, ++offset, source.TotalFollowUsers);
                Bind(statement, ++offset, source.TotalIllusts);
                Bind(statement, ++offset, source.TotalManga);
                Bind(statement, ++offset, source.TotalNovels);
                Bind(statement, ++offset, source.TotalIllustBookmarksPublic);
                Bind(statement, ++offset, source.TotalIllustSeries);
                Bind(statement, ++offset, source.TotalNovelSeries);
                Bind(statement, ++offset, source.BackgroundImageUrl);
                Bind(statement, ++offset, source.TwitterAccount);
                Bind(statement, ++offset, source.TwitterUrl);
                Bind(statement, ++offset, source.PawooUrl);
                Bind(statement, ++offset, source.IsPremium);
                Bind(statement, ++offset, source.IsUsingCustomProfileImage);
            }

            BindProfile(statement, 6, source.Profile.Value);
        }

        if (source.ProfilePublicity.HasValue)
        {
            void BindPublicity(sqlite3_stmt statement, int offset, UserDetailProfilePublicity source)
            {
                Bind(statement, ++offset, source.Gender);
                Bind(statement, ++offset, source.Region);
                Bind(statement, ++offset, source.BirthDay);
                Bind(statement, ++offset, source.BirthYear);
                Bind(statement, ++offset, source.Job);
                Bind(statement, ++offset, source.Pawoo);
            }

            BindPublicity(statement, 29, source.ProfilePublicity.Value);
        }

        if (source.Workspace.HasValue)
        {
            void BindWorkspace(sqlite3_stmt statement, int offset, UserDetailWorkspace source)
            {
                Bind(statement, ++offset, source.Pc);
                Bind(statement, ++offset, source.Monitor);
                Bind(statement, ++offset, source.Tool);
                Bind(statement, ++offset, source.Scanner);
                Bind(statement, ++offset, source.Tablet);
                Bind(statement, ++offset, source.Mouse);
                Bind(statement, ++offset, source.Printer);
                Bind(statement, ++offset, source.Desktop);
                Bind(statement, ++offset, source.Music);
                Bind(statement, ++offset, source.Desk);
                Bind(statement, ++offset, source.Chair);
                Bind(statement, ++offset, source.Comment);
                Bind(statement, ++offset, source.WorkspaceImageUrl);
            }

            BindWorkspace(statement, 35, source.Workspace.Value);
        }

        try
        {
            while (Step(statement) == SQLITE_BUSY)
            {
                await Task.Delay(TimeSpan.FromSeconds(1d), token).ConfigureAwait(false);
            }

            return CBool(statement, 0);
        }
        finally
        {
            Reset(statement);
        }
    }

    public async ValueTask<bool> UserAddOrUpdateAsync(UserPreviewResponseContent source, CancellationToken token)
    {
        sqlite3_stmt PrepareStatement(in UserPreviewResponseContent source)
        {
            var artworks = source.Illusts.AsSpan();
            var key = new ArtworkArrayKey(artworks.Length);
            if (artworks.Length > 0)
            {
                for (var i = 0; i < artworks.Length; i++)
                {
                    ref var item = ref artworks[i];
                    key.Collection[i] = (item.Tags?.Length ?? 0, item.Tools?.Length ?? 0);
                }

                key.Sort(artworks);
            }

            ref var statement = ref CollectionsMarshal.GetValueRefOrAddDefault(userPreviewAddOrUpdateStatementDictionary ??= new(), key, out _);
            if (statement is null)
            {
                var builder = ZString.CreateUtf8StringBuilder();
                builder.Append("BEGIN IMMEDIATE TRANSACTION;");
                if (artworks.Length > 0)
                {
                    builder.Append("DELETE FROM \"ArtworkTagCrossTable\" WHERE \"ValueKind\" = 1 AND \"Id\" IN (?8");
                    for (var i = 1; i < artworks.Length; i++)
                    {
                        builder.Append(", ?");
                        builder.Append(i * 16 + 8);
                    }
                    builder.Append(");");

                    builder.Append("DELETE FROM \"ArtworkToolCrossTable\" WHERE \"Id\" IN (?8");
                    for (var i = 1; i < artworks.Length; i++)
                    {
                        builder.Append(", ?");
                        builder.Append(i * 16 + 8);
                    }
                    builder.Append(");");

                    builder.Append("INSERT INTO \"ArtworkTable\" " +
                        "(\"UserId\", \"Id\", \"PageCount\", \"Width\", \"Height\", \"Type\", \"Extension\", \"IsXRestricted\", \"IsVisible\"," +
                        "\"IsMuted\", \"CreateDate\", \"FileDate\", \"TotalView\", \"TotalBookmarks\", \"IsBookmarked\", \"Title\", \"Caption\") VALUES ");
                    for (var i = 0; i < artworks.Length; i++)
                    {
                        builder.Append(i == 0 ? "(?1" : "), (?1");
                        for (var j = 0; j < 16; j++)
                        {
                            builder.Append(", ?");
                            builder.Append(i * 16 + j + 8);
                        }
                    }

                    builder.Append(") ON CONFLICT (\"Id\") DO UPDATE SET \"UserId\" = \"excluded\".\"UserId\", \"PageCount\" = \"excluded\".\"PageCount\", \"Width\" = \"excluded\".\"Width\"," +
                            "\"Height\" = \"excluded\".\"Height\", \"Type\" = \"excluded\".\"Type\", \"Extension\" = \"excluded\".\"Extension\", \"IsXRestricted\" = \"excluded\".\"IsXRestricted\"," +
                            "\"IsVisible\" = \"excluded\".\"IsVisible\", \"IsMuted\" = \"excluded\".\"IsMuted\", \"CreateDate\" = \"excluded\".\"CreateDate\"," +
                            "\"FileDate\" = \"excluded\".\"FileDate\", \"TotalView\" = \"excluded\".\"TotalView\", \"TotalBookmarks\" = \"excluded\".\"TotalBookmarks\"," +
                            "\"IsBookmarked\" = \"excluded\".\"IsBookmarked\", \"Title\" = \"excluded\".\"Title\", \"Caption\" = \"excluded\".\"Caption\";");

                    var tagOffset = artworks.Length * 16 + 8;
                    for (var i = 0; i < artworks.Length; i++)
                    {
                        ref var tags = ref artworks[i].Tags;
                        if (tags is not { Length: > 0 })
                        {
                            continue;
                        }

                        builder.Append("INSERT INTO \"ArtworkTagCrossTable\" (SELECT ?");
                        builder.Append(i * 16 + 8);
                        builder.Append(", \"Id\", 1 FROM \"TagTable\" WHERE \"Value\" IN (");
                        for (var j = 0; j < tags.Length; j++)
                        {
                            builder.Append(j == 0 ? "?" : ", ?");
                            builder.Append(tagOffset++);
                        }
                        builder.Append(")) ON CONFLICT (\"Id\", \"TagId\") DO UPDATE SET \"ValueKind\" = (CASE WHEN \"ValueKind\" = 2 THEN 1 ELSE \"ValueKind\" END);");
                    }

                    for (var i = 0; i < artworks.Length; i++)
                    {
                        ref var tools = ref artworks[i].Tools;
                        if (tools is not { Length: > 0 })
                        {
                            continue;
                        }

                        builder.Append("INSERT OR IGNORE INTO \"ArtworkToolCrossTable\" (SELECT ?");
                        builder.Append(i * 16 + 8);
                        builder.Append(", \"Id\" FROM \"ToolTable\" WHERE \"Value\" IN (");
                        for (var j = 0; j < tools.Length; j++)
                        {
                            builder.Append(j == 0 ? "?" : ", ?");
                            builder.Append(tagOffset++);
                        }
                        builder.Append("));");
                    }
                }

                builder.Append("INSERT INTO \"UserTable\" VALUES (?1, ?2, ?3, ?4, ?5, 0, 0, ?6, ?7, NULL, 0) ON CONFLICT (\"Id\") DO UPDATE SET " +
                        "\"Name\" = \"excluded\".\"Name\", \"Account\" = \"excluded\".\"Account\", \"IsFollowed\" = \"excluded\".\"IsFollowed\", \"IsMuted\"=\"excluded\".\"IsMuted\"," +
                        "\"IsOfficiallyRemoved\" = 0, \"ImageUrls\" = \"excluded\".\"ImageUrls\", \"Comment\" = \"excluded\".\"Comment\";" +
                    "SELECT last_insert_rowid() = ?1;" +
                    "END TRANSACTION;");
                statement = Prepare(ref builder, true, out _);
                builder.Dispose();
            }

            return statement;
        }

        var statement = PrepareStatement(source);
        Bind(statement, 1, source.User.Id);
        Bind(statement, 2, source.User.Name);
        Bind(statement, 3, source.User.Account);
        Bind(statement, 4, source.User.IsFollowed);
        Bind(statement, 5, source.IsMuted);
        Bind(statement, 6, source.User.ProfileImageUrls.Medium);
        Bind(statement, 7, source.User.Comment);

        if (source.Illusts is { Length: > 0 } artworks)
        {
            int BindArtwork(sqlite3_stmt statement, int offset, ref ArtworkResponseContent artwork, int totalOffset)
            {
                Bind(statement, offset, artwork.Id);
                Bind(statement, ++offset, artwork.PageCount);
                Bind(statement, ++offset, artwork.Width);
                Bind(statement, ++offset, artwork.Height);
                Bind(statement, ++offset, artwork.Type);
                Bind(statement, ++offset, artwork.ConvertToFileExtensionKind());
                Bind(statement, ++offset, artwork.XRestrict != 0);
                Bind(statement, ++offset, artwork.Visible);
                Bind(statement, ++offset, artwork.IsMuted);
                Bind(statement, ++offset, artwork.CreateDate);
                Bind(statement, ++offset, LocalNetworkConverter.ParseFileDate(artwork));
                Bind(statement, ++offset, artwork.TotalView);
                Bind(statement, ++offset, artwork.TotalBookmarks);
                Bind(statement, ++offset, artwork.IsBookmarked);
                Bind(statement, ++offset, artwork.Title);
                Bind(statement, ++offset, artwork.Caption);

                foreach (var tag in artwork.Tags.AsSpan())
                {
                    Bind(statement, totalOffset++, tag.Name);
                }

                return totalOffset;
            }

            var totalOffset = artworks.Length * 16 + 8;
            for (var i = 0; i < artworks.Length; i++)
            {
                totalOffset = BindArtwork(statement, i * 16 + 8, ref artworks[i], totalOffset);
            }

            int BindTool(sqlite3_stmt statement, int offset, ref string[]? tools)
            {
                foreach (var item in tools.AsSpan())
                {
                    Bind(statement, offset++, item);
                }

                return offset;
            }

            for (var i = 0; i < artworks.Length; i++)
            {
                totalOffset = BindTool(statement, totalOffset, ref artworks[i].Tools);
            }
        }

        try
        {
            while (Step(statement) == SQLITE_BUSY)
            {
                await Task.Delay(TimeSpan.FromSeconds(1d), token).ConfigureAwait(false);
            }

            return CBool(statement, 0);
        }
        finally
        {
            Reset(statement);
        }
    }
}
