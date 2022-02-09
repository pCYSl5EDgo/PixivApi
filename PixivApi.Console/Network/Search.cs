namespace PixivApi;

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

        var filter = new ArtworkDatabaseInfoFilter()
        {
            TagFilter = new()
            {
                Partials = text.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries),
            }
        };

        return await InternalSearchAsync(null, filter, OverwriteKind.Add, pipe);
    }

    [Command("search-filter")]
    public async ValueTask<int> SearchFilterAsync
    (
        [Option(0, IOUtility.FilterDescription)] string filter,
        [Option("o", $"output {IOUtility.ArtworkDatabaseDescription}")] string? output = null,
        [Option(null, IOUtility.OverwriteKindDescription)] string overwrite = "add",
        bool pipe = false
    )
    {
        if (string.IsNullOrWhiteSpace(filter))
        {
            if (!pipe)
            {
                logger.LogError("empty.");
            }

            return -1;
        }

        if (!await Connect().ConfigureAwait(false))
        {
            return -3;
        }

        var token = Context.CancellationToken;
        var artworkItemFilter = await IOUtility.JsonParseAsync<ArtworkDatabaseInfoFilter>(filter, token).ConfigureAwait(false);
        if (artworkItemFilter is null)
        {
            if (!pipe)
            {
                logger.LogError("cannot search anything.");
            }

            return -1;
        }

        output = IOUtility.FindArtworkDatabase(output, false);
        return await InternalSearchAsync(output, artworkItemFilter, OverwriteKindExtensions.Parse(overwrite), pipe).ConfigureAwait(false);
    }

    private async ValueTask<int> InternalSearchAsync(string? output, ArtworkDatabaseInfoFilter filter, OverwriteKind overwrite, bool isPipeOutput)
    {
        if (CalcUrl(filter) is not string url)
        {
            if (!isPipeOutput)
            {
                logger.LogError("invalid url.");
            }

            return -1;
        }

        output ??= CalcFileName(filter);
        if (string.IsNullOrWhiteSpace(output))
        {
            if (!isPipeOutput)
            {
                logger.LogError("invalid file name.");
            }

            return -1;
        }

        if (!await Connect().ConfigureAwait(false))
        {
            return -1;
        }

        await OverwriteLoopDownloadAsync<SearchLoopDownloadHandler, SearchMergeLoopDownloadHandler, IllustsResponseData, ArtworkDatabaseInfo>(
            output, url, overwrite, isPipeOutput,
            new(),
            new(),
            IOUtility.MessagePackDeserializeAsync<ArtworkDatabaseInfo[]>,
            IOUtility.MessagePackSerializeAsync
        ).ConfigureAwait(false);
        return 0;

        static string? CalcUrl(ArtworkDatabaseInfoFilter filter)
        {
            DefaultInterpolatedStringHandler handler;
            static void Add(out DefaultInterpolatedStringHandler handler, string[] array)
            {
                handler = $"https://{ApiHost}/v1/search/illust?word=";
                handler.AppendFormatted(new PercentEncoding(array[0]));
                for (int i = 1; i < array.Length; i++)
                {
                    handler.AppendLiteral("%20");
                    handler.AppendFormatted(new PercentEncoding(array[i]));
                }

                handler.AppendLiteral("&search_target=");
            }

            if (filter.TagFilter is { IsNoFilter: false } tagFilter)
            {
                if (tagFilter.Partials is { Length: > 0 } partials)
                {
                    Add(out handler, partials);
                    handler.AppendLiteral("partial_match_for_tags");
                }
                else if (tagFilter.Exacts is { Length: > 0 } exacts)
                {
                    Add(out handler, exacts);
                    handler.AppendLiteral("exact_match_for_tags");
                }
                else
                {
                    return null;
                }
            }
            else if (filter.PartialTitles is { Length: > 0 } partials)
            {
                Add(out handler, partials);
                handler.AppendLiteral("title_and_caption");
            }
            else
            {
                return null;
            }

            handler.AppendLiteral("&sort=date_desc");
            return handler.ToStringAndClear();
        }

        static string? CalcFileName(ArtworkDatabaseInfoFilter filter)
        {
            DefaultInterpolatedStringHandler handler;
            static void Add(out DefaultInterpolatedStringHandler handler, string[] array)
            {
                handler = $"search_";
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
            }

            if (filter.TagFilter is { IsNoFilter: false } tagFilter)
            {
                if (tagFilter.Partials is { Length: > 0 } partials)
                {
                    Add(out handler, partials);
                }
                else if (tagFilter.Exacts is { Length: > 0 } exacts)
                {
                    Add(out handler, exacts);
                }
                else
                {
                    return null;
                }
            }
            else if (filter.PartialTitles is { Length: > 0 } partials)
            {
                Add(out handler, partials);
            }
            else
            {
                return null;
            }

            return handler.ToStringAndClear();
        }
    }
}
