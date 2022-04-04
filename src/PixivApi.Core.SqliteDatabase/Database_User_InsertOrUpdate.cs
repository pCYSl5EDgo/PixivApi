namespace PixivApi.Core.SqliteDatabase;

internal sealed partial class Database
{
    private sqlite3_stmt? insertUserStatement;
    private sqlite3_stmt? insertUser_UserResponse_Statement;
    private sqlite3_stmt? insertUser_UserPreviewResponse_Statement;
    private sqlite3_stmt? insertUserDetailStatement;
    private sqlite3_stmt?[]? insertTagsOfUserStatementArray;

    [StringLiteral.Utf8("INSERT INTO \"UserTable\" VALUES (?1, ?2, ?3, ?4, ?5, ?6, ?7, ?8, ?9, ?10, ?11) ON CONFLICT (\"Id\") DO UPDATE SET " +
        "\"Id\" = \"excluded\".\"Id\", \"Name\" = \"excluded\".\"Name\", \"Account\" = \"excluded\".\"Account\", \"IsFollowed\" = \"excluded\".\"IsFollowed\"," +
        "\"IsMuted\" = \"excluded\".\"IsMuted\", \"IsOfficiallyRemoved\" = \"excluded\".\"IsOfficiallyRemoved\", \"HideReason\" = \"excluded\".\"HideReason\"," +
        "\"ImageUrls\" = \"excluded\".\"ImageUrls\", \"Comment\" = \"excluded\".\"Comment\", \"Memo\" = \"excluded\".\"Memo\", \"HasDetail\" = \"excluded\".\"HasDetail\"")]
    private static partial ReadOnlySpan<byte> Literal_InsertUser();

    [StringLiteral.Utf8("INSERT OR REPLACE INTO \"UserDetailTable\" VALUES (?1, ?2, ?3, ?4, ?5, ?6, ?7, ?8, ?9, ?10, ?11, ?12, ?13, ?14, ?15, ?16, ?17, ?18, ?19," +
        "?20, ?21, ?22, ?23, ?24, ?25, ?26, ?27, ?28, ?29, ?30, ?31, ?32, ?33, ?34, ?35, ?36, ?37, ?38, ?39, ?40, ?41, ?42, ?43)")]
    private static partial ReadOnlySpan<byte> Literal_InsertUserDetail();

    private async ValueTask InsertOrUpdateAsync(User user, CancellationToken token)
    {
        await InsertOrUpdateUserAsync(user, token).ConfigureAwait(false);
        await InsertOrUpdateUserDetailAsync(user, token).ConfigureAwait(false);
        await DeleteTagsOfUserStatementAsync(user.Id, token).ConfigureAwait(false);
        await InsertTagsOfUserAsync(user.Id, user.ExtraTags, token).ConfigureAwait(false);
    }

    [StringLiteral.Utf8("INSERT INTO \"UserTagCrossTable\" VALUES (?1, ?2")]
    private static partial ReadOnlySpan<byte> Literal_Insert_TagsOfUser_Parts_0();

    private ValueTask InsertTagsOfUserAsync(ulong id, ReadOnlySpan<uint> tags, CancellationToken token)
    {
        if (tags.IsEmpty)
        {
            return ValueTask.CompletedTask;
        }

        ref var statement = ref At(ref insertTagsOfUserStatementArray, tags.Length);
        if (statement is null)
        {
            var builder = ZString.CreateUtf8StringBuilder();
            builder.AppendLiteral(Literal_Insert_TagsOfUser_Parts_0());
            for (var i = 1; i < tags.Length; i++)
            {
                builder.AppendLiteral(Literal_Insert_TagOrTool_Parts_1());
                builder.Append(i + 2);
            }

            builder.AppendAscii(')');
            statement = Prepare(ref builder, true, out _);
            builder.Dispose();
        }
        else
        {
            Reset(statement);
        }

        Bind(statement, 1, id);
        for (var i = 0; i < tags.Length; i++)
        {
            Bind(statement, i + 2, tags[i]);
        }

        return ExecuteAsync(statement, token);
    }

    private ValueTask InsertOrUpdateUserAsync(User user, CancellationToken token)
    {
        if (insertUserStatement is null)
        {
            insertUserStatement = Prepare(Literal_InsertUser(), true, out _);
        }
        else
        {
            Reset(insertUserStatement);
        }

        var statement = insertUserStatement;
        Bind(statement, 0x01, user.Id);
        Bind(statement, 0x02, user.Name);
        Bind(statement, 0x03, user.Account);
        Bind(statement, 0x04, user.IsFollowed);
        Bind(statement, 0x05, user.IsMuted);
        Bind(statement, 0x06, user.IsOfficiallyRemoved);
        Bind(statement, 0x07, user.ExtraHideReason);
        Bind(statement, 0x08, user.ImageUrls);
        Bind(statement, 0x09, user.Comment);
        Bind(statement, 0x0a, user.ExtraMemo);
        var hasDetail = user is not { Profile: null, ProfilePublicity: null, Workspace: null };
        Bind(statement, 0x0b, hasDetail);
        return ExecuteAsync(statement, token);
    }

    private ValueTask InsertOrUpdateUserDetailAsync(User user, CancellationToken token)
    {
        if (user is { Profile: null, ProfilePublicity: null, Workspace: null })
        {
            return ValueTask.CompletedTask;
        }

        if (insertUserDetailStatement is null)
        {
            insertUserDetailStatement = Prepare(Literal_InsertUserDetail(), true, out _);
        }
        else
        {
            Reset(insertUserDetailStatement);
        }

        var statement = insertUserDetailStatement;
        BindUserDetail(statement, user);
        return ExecuteAsync(statement, token);
    }

    [StringLiteral.Utf8("INSERT INTO \"UserTable\" VALUES (?1, ?2, ?3, ?4, 0, 0, 0, ?5, ?6, NULL, 1) ON CONFLICT (\"Id\") DO UPDATE SET " +
        "\"Id\" = \"excluded\".\"Id\", \"Name\" = \"excluded\".\"Name\", \"Account\" = \"excluded\".\"Account\", \"IsFollowed\" = \"excluded\".\"IsFollowed\"," +
        "\"ImageUrls\" = \"excluded\".\"ImageUrls\", \"Comment\" = \"excluded\".\"Comment\", \"HasDetail\" = 1")]
    private static partial ReadOnlySpan<byte> Literal_InsertUser_UserResponse();

    private ValueTask InsertOrUpdateUserAsync(in UserDetailResponseData user, CancellationToken token) => InsertOrUpdateUserAsync(user.User, token);

    private ValueTask InsertOrUpdateUserAsync(in UserResponse user, CancellationToken token)
    {
        if (insertUser_UserResponse_Statement is null)
        {
            insertUser_UserResponse_Statement = Prepare(Literal_InsertUser_UserResponse(), true, out _);
        }
        else
        {
            Reset(insertUser_UserResponse_Statement);
        }

        var statement = insertUser_UserResponse_Statement;
        Bind(statement, 1, user.Id);
        Bind(statement, 2, user.Name);
        Bind(statement, 3, user.Account);
        Bind(statement, 4, user.IsFollowed);
        Bind(statement, 5, user.ProfileImageUrls.Medium);
        Bind(statement, 6, user.Comment);
        return ExecuteAsync(statement, token);
    }

    [StringLiteral.Utf8("INSERT INTO \"UserTable\" VALUES (?1, ?2, ?3, ?4, ?5, 0, 0, ?6, ?7, NULL, 0) ON CONFLICT (\"Id\") DO UPDATE SET " +
        "\"Id\" = \"excluded\".\"Id\", \"Name\" = \"excluded\".\"Name\", \"Account\" = \"excluded\".\"Account\", \"IsFollowed\" = \"excluded\".\"IsFollowed\"," +
        "\"IsMuted\" = \"excluded\".\"IsMuted\", \"IsOfficiallyRemoved\" = \"excluded\".\"IsOfficiallyRemoved\", \"ImageUrls\" = \"excluded\".\"ImageUrls\", \"Comment\" = \"excluded\".\"Comment\"")]
    private static partial ReadOnlySpan<byte> Literal_InsertUser_UserPreviewResponse();

    private ValueTask InsertOrUpdateUserAsync(in UserPreviewResponseContent user, CancellationToken token)
    {
        if (insertUser_UserPreviewResponse_Statement is null)
        {
            insertUser_UserPreviewResponse_Statement = Prepare(Literal_InsertUser(), true, out _);
        }
        else
        {
            Reset(insertUser_UserPreviewResponse_Statement);
        }

        var statement = insertUser_UserPreviewResponse_Statement;
        Bind(statement, 1, user.User.Id);
        Bind(statement, 2, user.User.Name);
        Bind(statement, 3, user.User.Account);
        Bind(statement, 4, user.User.IsFollowed);
        Bind(statement, 5, user.IsMuted);
        Bind(statement, 6, user.User.ProfileImageUrls.Medium);
        Bind(statement, 7, user.User.Comment);
        return ExecuteAsync(statement, token);
    }

    public async ValueTask<bool> UserAddOrUpdateAsync(UserDetailResponseData user, CancellationToken token)
    {
        await InsertOrUpdateUserAsync(user, token).ConfigureAwait(false);
        var rowId = GetLastInsertRowId();
        await InsertOrUpdateUserDetailAsync(user, token).ConfigureAwait(false);
        return rowId == user.User.Id;
    }

    private ValueTask InsertOrUpdateUserDetailAsync(UserDetailResponseData user, CancellationToken token)
    {
        if (insertUserDetailStatement is null)
        {
            insertUserDetailStatement = Prepare(Literal_InsertUserDetail(), true, out _);
        }
        else
        {
            Reset(insertUserDetailStatement);
        }

        var statement = insertUserDetailStatement;
        Bind(statement, 1, user.User.Id);
        if (user.Profile.HasValue)
        {
            BindProfile(statement, user.Profile.Value);
        }

        if (user.ProfilePublicity.HasValue)
        {
            BindPublicity(statement, user.ProfilePublicity.Value);
        }

        if (user.Workspace.HasValue)
        {
            BindWorkspace(statement, user.Workspace.Value);
        }

        return ExecuteAsync(statement, token);
    }

    public async ValueTask<bool> UserAddOrUpdateAsync(UserPreviewResponseContent user, CancellationToken token)
    {
        await InsertOrUpdateUserAsync(user, token).ConfigureAwait(false);
        var rowId = GetLastInsertRowId();
        if (user.Illusts is { Length: > 0 } artworks)
        {
            foreach (var artwork in artworks)
            {
                await ArtworkAddOrUpdateAsync(artwork, token).ConfigureAwait(false);
            }
        }

        return rowId == user.User.Id;
    }

    private void BindUserDetail(sqlite3_stmt statement, User answer)
    {
        Bind(statement, 0x01, answer.Id);
        if (answer.Profile is { } profile)
        {
            Bind(statement, 0x02, profile.Webpage);
            Bind(statement, 0x03, profile.Gender);
            Bind(statement, 0x04, profile.Birth);
            Bind(statement, 0x05, profile.BirthYear);
            Bind(statement, 0x06, profile.BirthDay);
            Bind(statement, 0x07, profile.Region);
            Bind(statement, 0x08, profile.AddressId);
            Bind(statement, 0x09, profile.CountryCode);
            Bind(statement, 0x0a, profile.Job);
            Bind(statement, 0x0b, profile.JobId);
            Bind(statement, 0x0c, profile.TotalFollowUsers);
            Bind(statement, 0x0d, profile.TotalIllusts);
            Bind(statement, 0x0e, profile.TotalManga);
            Bind(statement, 0x0f, profile.TotalNovels);
            Bind(statement, 0x10, profile.TotalIllustBookmarksPublic);
            Bind(statement, 0x11, profile.TotalIllustSeries);
            Bind(statement, 0x12, profile.TotalNovelSeries);
            Bind(statement, 0x13, profile.BackgroundImageUrl);
            Bind(statement, 0x14, profile.TwitterAccount);
            Bind(statement, 0x15, profile.TwitterUrl);
            Bind(statement, 0x16, profile.PawooUrl);
            Bind(statement, 0x17, profile.IsPremium);
            Bind(statement, 0x18, profile.IsUsingCustomProfileImage);
        }

        if (answer.ProfilePublicity is { } publicity)
        {
            Bind(statement, 0x19, publicity.Gender);
            Bind(statement, 0x1a, publicity.Region);
            Bind(statement, 0x1b, publicity.BirthDay);
            Bind(statement, 0x1c, publicity.BirthYear);
            Bind(statement, 0x1d, publicity.Job);
            Bind(statement, 0x1e, publicity.Pawoo);
        }

        if (answer.Workspace is { } workspace)
        {
            Bind(statement, 0x1f, workspace.Pc);
            Bind(statement, 0x20, workspace.Monitor);
            Bind(statement, 0x21, workspace.Tool);
            Bind(statement, 0x22, workspace.Scanner);
            Bind(statement, 0x23, workspace.Tablet);
            Bind(statement, 0x24, workspace.Mouse);
            Bind(statement, 0x25, workspace.Printer);
            Bind(statement, 0x26, workspace.Desktop);
            Bind(statement, 0x27, workspace.Music);
            Bind(statement, 0x28, workspace.Desk);
            Bind(statement, 0x29, workspace.Chair);
            Bind(statement, 0x2a, workspace.Comment);
            Bind(statement, 0x2b, workspace.WorkspaceImageUrl);
        }
    }

    private void BindProfile(sqlite3_stmt statement, UserDetailProfile source)
    {
        Bind(statement, 0x02, source.Webpage);
        Bind(statement, 0x03, source.Gender);
        Bind(statement, 0x04, source.Birth);
        Bind(statement, 0x05, source.BirthYear);
        Bind(statement, 0x06, source.BirthDay);
        Bind(statement, 0x07, source.Region);
        Bind(statement, 0x08, source.AddressId);
        Bind(statement, 0x09, source.CountryCode);
        Bind(statement, 0x0a, source.Job);
        Bind(statement, 0x0b, source.JobId);
        Bind(statement, 0x0c, source.TotalFollowUsers);
        Bind(statement, 0x0d, source.TotalIllusts);
        Bind(statement, 0x0e, source.TotalManga);
        Bind(statement, 0x0f, source.TotalNovels);
        Bind(statement, 0x10, source.TotalIllustBookmarksPublic);
        Bind(statement, 0x11, source.TotalIllustSeries);
        Bind(statement, 0x12, source.TotalNovelSeries);
        Bind(statement, 0x13, source.BackgroundImageUrl);
        Bind(statement, 0x14, source.TwitterAccount);
        Bind(statement, 0x15, source.TwitterUrl);
        Bind(statement, 0x16, source.PawooUrl);
        Bind(statement, 0x17, source.IsPremium);
        Bind(statement, 0x18, source.IsUsingCustomProfileImage);
    }

    private void BindPublicity(sqlite3_stmt statement, UserDetailProfilePublicity source)
    {
        Bind(statement, 0x19, source.Gender);
        Bind(statement, 0x1a, source.Region);
        Bind(statement, 0x1b, source.BirthDay);
        Bind(statement, 0x1c, source.BirthYear);
        Bind(statement, 0x1d, source.Job);
        Bind(statement, 0x1e, source.Pawoo);
    }

    private void BindWorkspace(sqlite3_stmt statement, UserDetailWorkspace source)
    {
        Bind(statement, 0x1f, source.Pc);
        Bind(statement, 0x20, source.Monitor);
        Bind(statement, 0x21, source.Tool);
        Bind(statement, 0x22, source.Scanner);
        Bind(statement, 0x23, source.Tablet);
        Bind(statement, 0x24, source.Mouse);
        Bind(statement, 0x25, source.Printer);
        Bind(statement, 0x26, source.Desktop);
        Bind(statement, 0x27, source.Music);
        Bind(statement, 0x28, source.Desk);
        Bind(statement, 0x29, source.Chair);
        Bind(statement, 0x2a, source.Comment);
        Bind(statement, 0x2b, source.WorkspaceImageUrl);
    }
}
