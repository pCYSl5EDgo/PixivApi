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

        return await InternalSearchAsync(text.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries), OverwriteKind.Add, pipe);
    }

    private async ValueTask<int> InternalSearchAsync(string[] searchArray, OverwriteKind overwrite, bool isPipeOutput)
    {
        if (CalcUrl(searchArray) is not string url)
        {
            if (!isPipeOutput)
            {
                logger.LogError("invalid url.");
            }

            return -1;
        }

        var output = CalcFileName(searchArray);
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
