using PixivApi.Core.Network;

namespace PixivApi.Console;

public partial class NetworkClient
{
    [Command("search")]
    public async ValueTask SearchAsync(
        [Option(0, ArgumentDescriptions.DatabaseDescription)] string output,
        [Option(1, "search text")] string text,
        [Option("e", "end_date")] string? end_date = null,
        ushort offset = 0,
        [Option("a", ArgumentDescriptions.AddKindDescription)] bool addBehaviour = false,
        bool pipe = false
    )
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            if (!pipe)
            {
                logger.LogError("empty.");
            }

            return;
        }

        var searchArray = text.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (CalcSearchUrl(searchArray, end_date, offset) is not string url)
        {
            if (!pipe)
            {
                logger.LogError("invalid url.");
            }

            return;
        }

        var token = Context.CancellationToken;
        var databaseTask = IOUtility.MessagePackDeserializeAsync<DatabaseFile>(output, token);
        var authentication = await ConnectAsync(token).ConfigureAwait(false);
        var database = default(DatabaseFile);
        var responseList = default(List<ArtworkResponseContent>);
        ulong add = 0UL, update = 0UL;
        bool RegisterNotShow(DatabaseFile database, ArtworkResponseContent item)
        {
            var isAdd = true;
            _ = database.ArtworkDictionary.AddOrUpdate(
                item.Id,
                _ => LocalNetworkConverter.Convert(item, database.TagSet, database.ToolSet, database.UserDictionary),
                (_, artwork) =>
                {
                    isAdd = false;
                    LocalNetworkConverter.Overwrite(artwork, item, database.TagSet, database.ToolSet, database.UserDictionary);
                    return artwork;
                }
            );

            return isAdd;
        }

        void RegisterShow(DatabaseFile database, ArtworkResponseContent item, bool pipe)
        {
            _ = database.ArtworkDictionary.AddOrUpdate(
                item.Id,
                _ =>
                {
                    ++add;
                    if (pipe)
                    {
                        logger.LogInformation($"{item.Id}");
                    }
                    else
                    {
                        logger.LogInformation($"{add,4}: {item.Id,20}");
                    }

                    return LocalNetworkConverter.Convert(item, database.TagSet, database.ToolSet, database.UserDictionary);
                },
                (_, artwork) =>
                {
                    ++update;
                    LocalNetworkConverter.Overwrite(artwork, item, database.TagSet, database.ToolSet, database.UserDictionary);
                    return artwork;
                }
            );
        }

        try
        {
            await foreach (var artworkCollection in new SearchArtworkAsyncNewToOldEnumerable(url, authentication, RetryGetAsync, ReconnectAsync, pipe).WithCancellation(token))
            {
                token.ThrowIfCancellationRequested();
                if (database is null)
                {
                    if (!databaseTask.IsCompleted)
                    {
                        responseList ??= new List<ArtworkResponseContent>(5010);
                        foreach (var item in artworkCollection)
                        {
                            responseList.Add(item);
                            if (pipe)
                            {
                                logger.LogInformation($"{item.Id}");
                            }
                            else
                            {
                                logger.LogInformation($"{responseList.Count,4}: {item.Id}");
                            }
                        }

                        token.ThrowIfCancellationRequested();
                        continue;
                    }

                    database = await databaseTask.ConfigureAwait(false) ?? new();
                    if (responseList is { Count: > 0 })
                    {
                        foreach (var item in responseList)
                        {
                            (RegisterNotShow(database, item) ? ref add : ref update)++;
                        }

                        responseList = null;
                    }
                }

                var oldAdd = add;
                foreach (var item in artworkCollection)
                {
                    RegisterShow(database, item, pipe);
                }

                token.ThrowIfCancellationRequested();
                if (!addBehaviour && add == oldAdd)
                {
                    break;
                }
            }
        }
        catch (TaskCanceledException)
        {
            logger.LogError("Accept cancel. Please wait for writing to the database file.");
        }
        finally
        {
            if (database is null)
            {
                database = await databaseTask.ConfigureAwait(false) ?? new();
                if (responseList is { Count: > 0 })
                {
                    foreach (var item in responseList)
                    {
                        (RegisterNotShow(database, item) ? ref add : ref update)++;
                    }

                    responseList = null;
                }
            }

            if (add != 0 || update != 0)
            {
                await IOUtility.MessagePackSerializeAsync(output, database, FileMode.Create).ConfigureAwait(false);
            }

            if (!pipe)
            {
                logger.LogInformation($"Total: {database.ArtworkDictionary.Count} Add: {add} Update: {update}");
            }
        }
    }

    private static string CalcSearchUrl(string[] array, string? end_date, ushort offset)
    {
        DefaultInterpolatedStringHandler handler = $"https://{ApiHost}/v1/search/illust?word=";
        handler.AppendFormatted(new PercentEncoding(array[0]));
        for (var i = 1; i < array.Length; i++)
        {
            handler.AppendLiteral("%20");
            handler.AppendFormatted(new PercentEncoding(array[i]));
        }

        handler.AppendLiteral("&search_target=partial_match_for_tags&sort=date_desc");
        if (!string.IsNullOrWhiteSpace(end_date))
        {
            handler.AppendLiteral("&end_date=");
            handler.AppendLiteral(end_date);
        }

        if (offset != 0)
        {
            handler.AppendLiteral("&offset=");
            handler.AppendFormatted(offset);
        }

        return handler.ToStringAndClear();
    }
}
