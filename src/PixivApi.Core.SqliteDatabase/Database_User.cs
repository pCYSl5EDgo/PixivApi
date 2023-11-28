namespace PixivApi.Core.SqliteDatabase;

internal sealed partial class Database
{
  private sqlite3_stmt? getUserStatement;
  private sqlite3_stmt? getUserDetailStatement;
  private sqlite3_stmt? getTagsOfUserStatement;
  private sqlite3_stmt? enumerateUserStatement;
  private sqlite3_stmt? officiallyRemoveUserStatement;

  public async ValueTask<User?> GetUserAsync(ulong id, CancellationToken token)
  {
    if (getUserStatement is null)
    {
      getUserStatement = Prepare("SELECT \"Name\", \"Account\", \"IsFollowed\", \"IsMuted\", \"IsOfficiallyRemoved\", \"HideReason\", \"ImageUrls\", \"Comment\", \"Memo\", \"HasDetail\""u8 +
          " FROM \"UserTable\" WHERE \"Id\" = ?"u8, true, out _);
    }
    else
    {
      Reset(getUserStatement);
    }

    var statement = getUserStatement;
    Bind(statement, 1, id);
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

  private async ValueTask ColumnTagsAsync(User user, CancellationToken token)
  {
    if (getTagsOfUserStatement is null)
    {
      getTagsOfUserStatement = Prepare("SELECT \"TagId\" FROM \"UserTagCrossTable\" WHERE \"Id\" = ?"u8, true, out _);
    }
    else
    {
      Reset(getTagsOfUserStatement);
    }

    var statement = getTagsOfUserStatement;
    Bind(statement, 1, user.Id);
    user.ExtraTags = await CU32ArrayAsync(statement, token).ConfigureAwait(false);
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

  private async ValueTask ColumnUserDetailAsync(User user, CancellationToken token)
  {
    if (getUserDetailStatement is null)
    {
      getUserDetailStatement = Prepare("SELECT \"Profile_Webpage\", \"Profile_Gender\", \"Profile_Birth\", \"Profile_BirthYear\","u8 +
          "\"Profile_BirthDay\", \"Profile_Region\", \"Profile_AddressId\", \"Profile_CountryCode\", \"Profile_Job\", \"Profile_JobId\","u8 +
          "\"Profile_TotalFollowUsers\", \"Profile_TotalIllusts\", \"Profile_TotalManga\", \"Profile_TotalNovels\","u8 +
          "\"Profile_TotalIllustBookmarksPublic\", \"Profile_TotalIllustSeries\", \"Profile_TotalNovelSeries\", \"Profile_BackgroundImageUrl\","u8 +
          "\"Profile_TwitterAccount\", \"Profile_TwitterUrl\", \"Profile_PawooUrl\", \"Profile_IsPremium\", \"Profile_IsUsingCustomProfileImage\","u8 +
          "\"ProfilePublicity_Gender\", \"ProfilePublicity_Region\", \"ProfilePublicity_BirthDay\", \"ProfilePublicity_BirthYear\","u8 +
          "\"ProfilePublicity_Job\", \"ProfilePublicity_Pawoo\", \"Workspace_Pc\", \"Workspace_Monitor\", \"Workspace_Tool\","u8 +
          "\"Workspace_Scanner\", \"Workspace_Tablet\", \"Workspace_Mouse\", \"Workspace_Printer\", \"Workspace_Desktop\","u8 +
          "\"Workspace_Music\", \"Workspace_Desk\", \"Workspace_Chair\", \"Workspace_Comment\", \"Workspace_WorkspaceImageUrl\" "u8 +
          "FROM \"UserDetailTable\" WHERE \"Id\" = ?"u8, true, out _);
    }
    else
    {
      Reset(getUserDetailStatement);
    }

    var statement = getUserDetailStatement;
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

  public async IAsyncEnumerable<User> EnumerateUserAsync([EnumeratorCancellation] CancellationToken token)
  {
    if (enumerateUserStatement is null)
    {
      enumerateUserStatement = Prepare("SELECT \"Origin\".\"Id\", \"Origin\".\"Name\", \"Origin\".\"Account\","u8 +
          "\"Origin\".\"IsFollowed\", \"Origin\".\"IsMuted\", \"Origin\".\"IsOfficiallyRemoved\","u8 +
          "\"Origin\".\"HideReason\", \"Origin\".\"ImageUrls\", \"Origin\".\"Comment\", \"Origin\".\"Memo\","u8 +
          "\"Origin\".\"HasDetail\" FROM \"UserTable\" AS \"Origin\""u8, true, out _);
    }
    else
    {
      Reset(enumerateUserStatement);
    }

    var statement = enumerateUserStatement;
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

  public async ValueTask OfficiallyRemoveUser(ulong id, CancellationToken token)
  {
    if (officiallyRemoveUserStatement is null)
    {
      officiallyRemoveUserStatement = Prepare("INSERT OR IGNORE INTO \"UserRemoveTable\" VALUES (?)"u8, true, out _);
    }
    else
    {
      Reset(officiallyRemoveUserStatement);
    }

    var statement = officiallyRemoveUserStatement;
    Bind(statement, 1, id);
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

  public async IAsyncEnumerable<User> FilterAsync(UserFilter filter, [EnumeratorCancellation] CancellationToken token)
  {
    if (token.IsCancellationRequested)
    {
      yield break;
    }

    sqlite3_stmt PrepareStatement()
    {
      var builder = ZString.CreateUtf8StringBuilder();
      var first = true;
      int intersect = -1, except = -1;
      FilterUtility.Preprocess(ref builder, filter, ref first, ref intersect, ref except);
      builder.AppendLiteral("SELECT \"Origin\".\"Id\", \"Origin\".\"Name\", \"Origin\".\"Account\","u8 +
          "\"Origin\".\"IsFollowed\", \"Origin\".\"IsMuted\", \"Origin\".\"IsOfficiallyRemoved\","u8 +
          "\"Origin\".\"HideReason\", \"Origin\".\"ImageUrls\", \"Origin\".\"Comment\", \"Origin\".\"Memo\","u8 +
          "\"Origin\".\"HasDetail\" FROM \"UserTable\" AS \"Origin\" WHERE "u8);
      var statement = FilterUtility.CreateStatement(database, ref builder, filter, logger, intersect, except);
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
