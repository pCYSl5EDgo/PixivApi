using PixivApi.Core.Network;

namespace PixivApi.Console;

partial class NetworkClient
{
    [Command("search")]
    public async ValueTask<int> SearchAsync(
        [Option(0)] string text,
        bool pipe = false
    )
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            if (!pipe)
            {
                logger.LogError("empty.");
            }

            return -1;
        }

        return await InternalSearchAsync(text.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries), OverwriteKind.Add, pipe);
    }

    private async ValueTask<int> InternalSearchAsync(string[] searchArray, OverwriteKind overwrite, bool pipe)
    {
        if (CalcUrl(searchArray) is not string url)
        {
            if (!pipe)
            {
                logger.LogError("invalid url.");
            }

            return -1;
        }

        var output = CalcFileName(searchArray);
        if (string.IsNullOrWhiteSpace(output))
        {
            if (!pipe)
            {
                logger.LogError("invalid file name.");
            }

            return -1;
        }

        var token = Context.CancellationToken;
        var database = overwrite == OverwriteKind.ClearAndAdd ?
            null :
            await IOUtility.MessagePackDeserializeAsync<Core.Local.DatabaseFile>(output, token).ConfigureAwait(false);
        if (database is null)
        {
            overwrite = OverwriteKind.ClearAndAdd;
            database = new();
        }

        var dictionary = new ConcurrentDictionary<ulong, Core.Local.Artwork>();
        if (overwrite != OverwriteKind.ClearAndAdd)
        {
            foreach (var item in database.Artworks)
            {
                dictionary.TryAdd(item.Id, item);
            }
        }

        if (!await Connect().ConfigureAwait(false))
        {
            return -1;
        }

        var add = 0UL;
        var update = 0UL;
        var enumerator = new DownloadArtworkAsyncEnumerable(RetryGetAsync, url).GetAsyncEnumerator(token);
        try
        {
            while (await enumerator.MoveNextAsync().ConfigureAwait(false))
            {
                var c = enumerator.Current;
                var oldAdd = add;
                var oldUpdate = update;
                await Parallel.ForEachAsync(c, token, (item, token) =>
                {
                    token.ThrowIfCancellationRequested();
                    var converted = Core.Local.Artwork.ConvertFromNetwrok(item, database.TagSet, database.ToolSet, database.UserDictionary);
                    dictionary.AddOrUpdate(
                        item.Id,
                        _ =>
                        {
                            var added = Interlocked.Increment(ref add);
                            if (pipe)
                            {
                                logger.LogInformation($"{converted.Id}");
                            }
                            else
                            {
                                logger.LogInformation($"{added,4}: {converted.Id,20}");
                            }
                            return converted;
                        },
                        (_, v) =>
                        {
                            Interlocked.Increment(ref update);
                            v.Overwrite(converted);
                            return v;
                        }
                    );

                    return ValueTask.CompletedTask;
                }).ConfigureAwait(false);

                if (overwrite == OverwriteKind.Add && update == oldUpdate && add != oldAdd)
                {
                    break;
                }
            }
        }
        finally
        {
            if (enumerator is not null)
            {
                await enumerator.DisposeAsync().ConfigureAwait(false);
            }

            if (add != 0 || update != 0)
            {
                database.Artworks = dictionary.Values.ToArray();
                await IOUtility.MessagePackSerializeAsync(output, database, FileMode.Create).ConfigureAwait(false);
            }

            if (!pipe)
            {
                logger.LogInformation($"Total: {database.Artworks.Length} Add: {add} Update: {update}");
            }
        }

        return 0;

        static string CalcUrl(string[] array)
        {
            DefaultInterpolatedStringHandler handler = $"https://{ApiHost}/v1/search/illust?word=";
            handler.AppendFormatted(new PercentEncoding(array[0]));
            for (int i = 1; i < array.Length; i++)
            {
                handler.AppendLiteral("%20");
                handler.AppendFormatted(new PercentEncoding(array[i]));
            }

            handler.AppendLiteral("&search_target=");
            handler.AppendLiteral("partial_match_for_tags&sort=date_desc");
            return handler.ToStringAndClear();
        }

        static string CalcFileName(string[] array)
        {
            DefaultInterpolatedStringHandler handler = $"search_";
            foreach (var c in array[0])
            {
                if (IOUtility.PathInvalidChars.Contains(c))
                {
                    handler.AppendLiteral("_");
                }
                else
                {
                    handler.AppendFormatted(c);
                }
            }

            for (int i = 1; i < array.Length; i++)
            {
                handler.AppendLiteral(" ");
                foreach (var c in array[i])
                {
                    if (IOUtility.PathInvalidChars.Contains(c))
                    {
                        handler.AppendLiteral("_");
                    }
                    else
                    {
                        handler.AppendFormatted(c);
                    }
                }
            }

            handler.AppendLiteral(IOUtility.ArtworkDatabaseFileExtension);
            return handler.ToStringAndClear();
        }
    }
}
