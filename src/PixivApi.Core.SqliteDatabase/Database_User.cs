namespace PixivApi.Core.SqliteDatabase;

internal sealed partial class Database
{
    private sqlite3_stmt? getUserStatement;
    private sqlite3_stmt? getUserDetailStatement;
    private sqlite3_stmt? getTagsOfUserStatement;
    private sqlite3_stmt? enumerateUserStatement;
    private sqlite3_stmt? officiallyRemoveUserStatement;

    [StringLiteral.Utf8("SELECT \"Name\", \"Account\", \"IsFollowed\", \"IsMuted\", \"IsOfficiallyRemoved\", \"HideReason\", \"ImageUrls\", \"Comment\", \"Memo\", \"HasDetail\" FROM \"UserTable\" WHERE \"Id\" = ?")]
    private static partial ReadOnlySpan<byte> Literal_SelectUser_FromUserTable_WhereId();

    [StringLiteral.Utf8("SELECT \"TagId\" FROM \"UserTagCrossTable\" WHERE \"Id\" = ?")]
    private static partial ReadOnlySpan<byte> Literal_SelectTagId_FromUserTagCrossTable_WhereId();

    public async ValueTask<User?> GetUserAsync(ulong id, CancellationToken token)
    {
        var statement = getUserStatement ??= Prepare(Literal_SelectUser_FromUserTable_WhereId(), true, out _);
        Bind(statement, 1, id);

        try
        {
            do
            {
                var code = Step(statement);
                if (code == SQLITE_BUSY)
                {
                    await Task.Delay(TimeSpan.FromSeconds(1d), token).ConfigureAwait(false);
                    token.ThrowIfCancellationRequested();
                    continue;
                }

                if (code == SQLITE_DONE)
                {
                    return null;
                }

                var answer = new User()
                {
                    Id = id,
                };

                var hasDetail = ColumnUser(answer, getUserStatement, 0);
                if (hasDetail)
                {
                    await ColumnUserDetailAsync(answer, token).ConfigureAwait(false);
                }

                await ColumnTagsAsync(answer, token).ConfigureAwait(false);
                return answer;
            } while (true);
        }
        finally
        {
            Reset(getUserStatement);
        }
    }

    private async ValueTask ColumnTagsAsync(User user, CancellationToken token)
    {
        var statement = getTagsOfUserStatement ??= Prepare(Literal_SelectTagId_FromUserTagCrossTable_WhereId(), true, out _);
        Bind(statement, 1, user.Id);
        try
        {
            user.ExtraTags = await CU32ArrayAsync(statement, token).ConfigureAwait(false);
        }
        finally
        {
            Reset(statement);
        }
    }

    private static bool ColumnUser(User user, sqlite3_stmt statement, int offset)
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
        return CBool(statement, offset);
    }

    [StringLiteral.Utf8("SELECT \"Profile_Webpage\", \"Profile_Gender\", \"Profile_Birth\", \"Profile_BirthYear\"," +
            "\"Profile_BirthDay\", \"Profile_Region\", \"Profile_AddressId\", \"Profile_CountryCode\", \"Profile_Job\", \"Profile_JobId\"," +
            "\"Profile_TotalFollowUsers\", \"Profile_TotalIllusts\", \"Profile_TotalManga\", \"Profile_TotalNovels\"," +
            "\"Profile_TotalIllustBookmarksPublic\", \"Profile_TotalIllustSeries\", \"Profile_TotalNovelSeries\", \"Profile_BackgroundImageUrl\"," +
            "\"Profile_TwitterAccount\", \"Profile_TwitterUrl\", \"Profile_PawooUrl\", \"Profile_IsPremium\", \"Profile_IsUsingCustomProfileImage\"," +
            "\"ProfilePublicity_Gender\", \"ProfilePublicity_Region\", \"ProfilePublicity_BirthDay\", \"ProfilePublicity_BirthYear\"," +
            "\"ProfilePublicity_Job\", \"ProfilePublicity_Pawoo\", \"Workspace_Pc\", \"Workspace_Monitor\", \"Workspace_Tool\"," +
            "\"Workspace_Scanner\", \"Workspace_Tablet\", \"Workspace_Mouse\", \"Workspace_Printer\", \"Workspace_Desktop\"," +
            "\"Workspace_Music\", \"Workspace_Desk\", \"Workspace_Chair\", \"Workspace_Comment\", \"Workspace_WorkspaceImageUrl\" " +
            "FROM \"UserDetailTable\" WHERE \"Id\" = ?")]
    private static partial ReadOnlySpan<byte> Literal_GetUserDetail();

    private async ValueTask ColumnUserDetailAsync(User user, CancellationToken token)
    {
        var statement = getUserDetailStatement ??= Prepare(Literal_GetUserDetail(), true, out _);
        Bind(statement, 1, user.Id);

        static void ColumnProfile(sqlite3_stmt statement, [NotNull] ref User.DetailProfile? profile)
        {
            profile ??= new();
            profile.Webpage = CStr(statement, 0);
            profile.Gender = CStr(statement, 1);
            profile.Birth = CStr(statement, 2);
            profile.BirthYear = CU32(statement, 3);
            profile.BirthDay = CStr(statement, 4);
            profile.Region = CStr(statement, 5);
            profile.AddressId = CI64(statement, 6);
            profile.CountryCode = CStr(statement, 7);
            profile.Job = CStr(statement, 8);
            profile.JobId = CI64(statement, 9);
            profile.TotalFollowUsers = CU64(statement, 10);
            profile.TotalIllusts = CU64(statement, 11);
            profile.TotalManga = CU64(statement, 12);
            profile.TotalNovels = CU64(statement, 13);
            profile.TotalIllustBookmarksPublic = CU64(statement, 14);
            profile.TotalIllustSeries = CU64(statement, 15);
            profile.TotalNovelSeries = CU64(statement, 16);
            profile.BackgroundImageUrl = CStr(statement, 17);
            profile.TwitterAccount = CStr(statement, 18);
            profile.TwitterUrl = CStr(statement, 19);
            profile.PawooUrl = CStr(statement, 20);
            profile.IsPremium = CBool(statement, 21);
            profile.IsUsingCustomProfileImage = CBool(statement, 22);
        }

        static void ColumnPublicity(sqlite3_stmt statement, [NotNull] ref User.DetailProfilePublicity? publicity)
        {
            publicity ??= new();
            publicity.Gender = CStr(statement, 23);
            publicity.Region = CStr(statement, 24);
            publicity.BirthDay = CStr(statement, 25);
            publicity.BirthYear = CStr(statement, 26);
            publicity.Job = CStr(statement, 27);
            publicity.Pawoo = CBool(statement, 28);
        }

        static void ColumnWorkspace(sqlite3_stmt statement, [NotNull] ref User.DetailWorkspace? workspace)
        {
            workspace ??= new();
            workspace.Pc = CStr(statement, 29);
            workspace.Monitor = CStr(statement, 30);
            workspace.Tool = CStr(statement, 31);
            workspace.Scanner = CStr(statement, 32);
            workspace.Tablet = CStr(statement, 33);
            workspace.Mouse = CStr(statement, 34);
            workspace.Printer = CStr(statement, 35);
            workspace.Desktop = CStr(statement, 36);
            workspace.Music = CStr(statement, 37);
            workspace.Desk = CStr(statement, 38);
            workspace.Chair = CStr(statement, 39);
            workspace.Comment = CStr(statement, 40);
            workspace.WorkspaceImageUrl = CStr(statement, 41);
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
                ColumnProfile(statement, ref user.Profile);
                ColumnPublicity(statement, ref user.ProfilePublicity);
                ColumnWorkspace(statement, ref user.Workspace);
            }
        }
        finally
        {
            Reset(statement);
        }
    }

    private const string EnumerateUserQuery = "SELECT \"Origin\".\"Id\", \"Origin\".\"Name\", \"Origin\".\"Account\"," +
        "\"Origin\".\"IsFollowed\", \"Origin\".\"IsMuted\", \"Origin\".\"IsOfficiallyRemoved\"," +
        "\"Origin\".\"HideReason\", \"Origin\".\"ImageUrls\", \"Origin\".\"Comment\", \"Origin\".\"Memo\"," +
        "\"Origin\".\"HasDetail\" FROM \"UserTable\" AS \"Origin\"";

    [StringLiteral.Utf8(EnumerateUserQuery)]
    private static partial ReadOnlySpan<byte> Literal_EnumerateUser();

    public async IAsyncEnumerable<User> EnumerateUserAsync([EnumeratorCancellation] CancellationToken token)
    {
        var statement = enumerateUserStatement ??= Prepare(Literal_EnumerateUser(), true, out _);
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
                    break;
                }

                var id = CU64(enumerateUserStatement, 0);
                if (id == 0)
                {
                    continue;
                }

                var answer = new User
                {
                    Id = id,
                };

                await ColumnTagsAsync(answer, token).ConfigureAwait(false);
                var hasDetail = ColumnUser(answer, enumerateUserStatement, 1);
                if (hasDetail)
                {
                    await ColumnUserDetailAsync(answer, token).ConfigureAwait(false);
                }
                yield return answer;
            } while (true);
        }
        finally
        {
            Reset(statement);
        }
    }

    [StringLiteral.Utf8("INSERT OR IGNORE INTO \"UserRemoveTable\" VALUES (?)")]
    private static partial ReadOnlySpan<byte> Literal_Remove_User();

    public async ValueTask OfficiallyRemoveUser(ulong id, CancellationToken token)
    {
        var statement = officiallyRemoveUserStatement ??= Prepare(Literal_Remove_User(), true, out _);
        Bind(statement, 1, id);
        try
        {
            do
            {
                var code = Step(statement);
                if (code != SQLITE_BUSY)
                {
                    break;
                }

                await Task.Delay(TimeSpan.FromSeconds(1d), token).ConfigureAwait(false);
            } while (!token.IsCancellationRequested);
        }
        finally
        {
            Reset(statement);
        }
    }

    public async IAsyncEnumerable<User> FilterAsync(UserFilter filter, [EnumeratorCancellation] CancellationToken token)
    {
        if (token.IsCancellationRequested)
        {
            yield break;
        }

        sqlite3_stmt PrepareStatement()
        {
            var builder = ZString.CreateUtf8StringBuilder();
            builder.AppendLiteral(Literal_EnumerateUser());
            builder.AppendLiteral(Literal_Where());
            var statement = UserFilterUtility.CreateStatement(database, ref builder, filter);
            builder.Dispose();
            return statement;
        }

        var statement = PrepareStatement();
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
                    yield break;
                }

                var id = CU64(statement, 0);
                if (id == 0)
                {
                    continue;
                }

                var answer = new User
                {
                    Id = id,
                };

                var hasDetail = ColumnUser(answer, statement, 1);
                if (hasDetail)
                {
                    await ColumnUserDetailAsync(answer, token).ConfigureAwait(false);
                }

                await ColumnTagsAsync(answer, token).ConfigureAwait(false);
                yield return answer;
            } while (!token.IsCancellationRequested);
        }
        finally
        {
            statement.manual_close();
        }
    }
}
