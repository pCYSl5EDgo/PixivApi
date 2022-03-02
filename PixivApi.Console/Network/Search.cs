using PixivApi.Core;
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
        [Option("o", ArgumentDescriptions.OverwriteKindDescription)] OverwriteKind overwrite = OverwriteKind.diff,
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
        var database = await IOUtility.MessagePackDeserializeAsync<Core.Local.DatabaseFile>(output, token).ConfigureAwait(false) ?? new();
        var authentication = await ConnectAsync(token).ConfigureAwait(false);
        ulong add = 0UL, update = 0UL;
        try
        {
            await foreach (var artworkCollection in new SearchArtworkAsyncNewToOldEnumerable(url, authentication, RetryGetAsync, ReconnectAsync, pipe).WithCancellation(token))
            {
                var oldAdd = add;
                foreach (var item in artworkCollection)
                {
                    if (token.IsCancellationRequested)
                    {
                        return;
                    }

                    var converted = Core.Local.Artwork.ConvertFromNetwrok(item, database.TagSet, database.ToolSet, database.UserDictionary);
                    database.ArtworkDictionary.AddOrUpdate(
                        item.Id,
                        _ =>
                        {
                            ++add;
                            if (pipe)
                            {
                                logger.LogInformation($"{converted.Id}");
                            }
                            else
                            {
                                logger.LogInformation($"{add,4}: {converted.Id,20}");
                            }
                            return converted;
                        },
                        (_, v) =>
                        {
                            ++update;
                            v.Overwrite(converted);
                            return v;
                        }
                    );
                }

                if (overwrite == OverwriteKind.diff && add == oldAdd)
                {
                    break;
                }
            }
        }
        finally
        {
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
