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

    [Command("delete-bookmarks")]
    public async ValueTask<int> DeleteBookmarksAsync()
    {
        if (string.IsNullOrWhiteSpace(configSettings.DatabaseFilePath))
        {
            return 0;
        }

        var token = Context.CancellationToken;
        if (await databaseFactory.RentAsync(token).ConfigureAwait(false) is not IExtenededDatabase database)
        {
            return 1;
        }

        ulong totalCount = 0;
        try
        {
            var filter = await filterFactory.CreateAsync(database, new(configSettings.ArtworkFilterFilePath!), token).ConfigureAwait(false);
            if (filter is null || token.IsCancellationRequested)
            {
                return 0;
            }

            AuthenticationHeaderValue? authentication = null;
            var printDebug = Context.Logger.IsEnabled(LogLevel.Debug);
            var printTrace = Context.Logger.IsEnabled(LogLevel.Trace);
            var requestSender = Context.ServiceProvider.GetRequiredService<RequestSender>();
            var encoding = new System.Text.UTF8Encoding(false);
            await foreach (var id in database.DeleteBookmarksAsync(filter, token))
            {
                if (token.IsCancellationRequested)
                {
                    return 0;
                }

                totalCount++;
                authentication ??= await holder.GetAsync(token).ConfigureAwait(false);

                using var request = new HttpRequestMessage(HttpMethod.Post, $"https://{ApiHost}/v1/illust/bookmark/delete");
                request.Headers.Authorization = authentication;
                if (!request.TryAddToHeader(configSettings.HashSecret, ApiHost))
                {
                    throw new InvalidOperationException();
                }

                request.Content = new StringContent($"get_secure_url=1&illust_id={id}", encoding, "application/x-www-form-urlencoded");
                if (printDebug)
                {
                    Context.Logger.LogDebug(id.ToString());
                }

                using var responseMessage = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, token).ConfigureAwait(false);
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
