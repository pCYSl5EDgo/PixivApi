using System.Text;

namespace PixivApi.Console;

public partial class NetworkClient
{
  [Command("bookmarks")]
  public ValueTask DownloadBookmarksOfUserAsync
  (
      [Option("a", ArgumentDescriptions.AddKindDescription)] bool addBehaviour = false,
      [Option("d")] bool download = false,
      [Option("p")] bool isPrivate = false
  )
  {
    if (string.IsNullOrWhiteSpace(configSettings.DatabaseFilePath))
    {
      return ValueTask.CompletedTask;
    }

    if (configSettings.UserId == 0UL)
    {
      Context.Logger.LogError($"{VirtualCodes.BrightRedColor}User Id should be written in appsettings.json{VirtualCodes.NormalizeColor}");
      return ValueTask.CompletedTask;
    }

    var privateOrPublic = isPrivate ? "private" : "public";
    var url = $"https://{ApiHost}/v1/user/bookmarks/illust?user_id={configSettings.UserId}&restrict={privateOrPublic}";
    return DownloadArtworkResponses(addBehaviour, download, url, Context.CancellationToken);
  }

  [Command("delete-bookmark")]
  public async ValueTask<int> DeleteBookmarkAsync(
      [Option(0)] ulong id
  )
  {
    var db = await databaseFactory.RentAsync(Context.CancellationToken).ConfigureAwait(false);
    try
    {
      var filter = new ArtworkFilter()
      {
        IdFilter = new()
        {
          Ids = [id]
        },
        IsBookmark = true,
        HideFilter = new()
        {
          DisallowedReason = [],
        }
      };

      if (db is not IExtenededDatabase database)
      {
        return 1;
      }

      AuthenticationHeaderValue? authentication = null;
      var printDebug = Context.Logger.IsEnabled(LogLevel.Debug);
      var printTrace = Context.Logger.IsEnabled(LogLevel.Trace);
      await foreach (var _id in database.DeleteBookmarksAsync(filter, Context.CancellationToken))
      {
        if (Context.CancellationToken.IsCancellationRequested)
        {
          return 0;
        }

        authentication ??= await holder.GetAsync(Context.CancellationToken).ConfigureAwait(false);
        using var request = new HttpRequestMessage(HttpMethod.Post, $"https://{ApiHost}/v1/illust/bookmark/delete");
        request.Headers.Authorization = authentication;
        if (!request.TryAddToHeader(configSettings.HashSecret, ApiHost))
        {
          throw new InvalidOperationException();
        }

        request.Content = new StringContent($"get_secure_url=1&illust_id={id}", Encoding.ASCII, "application/x-www-form-urlencoded");
        if (printDebug)
        {
          Context.Logger.LogDebug(id.ToString());
        }

        using var responseMessage = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, Context.CancellationToken).ConfigureAwait(false);
        if (printTrace)
        {
          Context.Logger.LogTrace(await responseMessage.Content.ReadAsStringAsync().ConfigureAwait(false));
        }
      }
    }
    finally
    {
      databaseFactory.Return(ref db);
    }
    return 1;
  }

  [Command("delete-bookmarks")]
  public async ValueTask<int> DeleteBookmarksAsync()
  {
    if (string.IsNullOrWhiteSpace(configSettings.DatabaseFilePath))
    {
      return 0;
    }

    if (await databaseFactory.RentAsync(Context.CancellationToken).ConfigureAwait(false) is not IExtenededDatabase database)
    {
      return 1;
    }

    ulong totalCount = 0;
    try
    {
      var filter = await filterFactory.CreateAsync(database, new(configSettings.ArtworkFilterFilePath!), Context.CancellationToken).ConfigureAwait(false);
      if (filter is null || Context.CancellationToken.IsCancellationRequested)
      {
        return 0;
      }

      AuthenticationHeaderValue? authentication = null;
      var printDebug = Context.Logger.IsEnabled(LogLevel.Debug);
      var printTrace = Context.Logger.IsEnabled(LogLevel.Trace);
      await foreach (var id in database.DeleteBookmarksAsync(filter, Context.CancellationToken))
      {
        if (Context.CancellationToken.IsCancellationRequested)
        {
          return 0;
        }

        totalCount++;
        authentication ??= await holder.GetAsync(Context.CancellationToken).ConfigureAwait(false);

        using var request = new HttpRequestMessage(HttpMethod.Post, $"https://{ApiHost}/v1/illust/bookmark/delete");
        request.Headers.Authorization = authentication;
        if (!request.TryAddToHeader(configSettings.HashSecret, ApiHost))
        {
          throw new InvalidOperationException();
        }

        request.Content = new StringContent($"get_secure_url=1&illust_id={id}", Encoding.ASCII, "application/x-www-form-urlencoded");
        if (printDebug)
        {
          Context.Logger.LogDebug(id.ToString());
        }

        using var responseMessage = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, Context.CancellationToken).ConfigureAwait(false);
        if (printTrace)
        {
          Context.Logger.LogTrace(await responseMessage.Content.ReadAsStringAsync().ConfigureAwait(false));
        }
      }
    }
    finally
    {
      IDatabase? db = database;
      databaseFactory.Return(ref db);
      if (!System.Console.IsErrorRedirected)
      {
        System.Console.Error.WriteLine($"Total Deleted Bookmark Count: {totalCount}");
      }
    }
    return 1;
  }
}
