namespace PixivApi;

public sealed class SearchLoopDownloadHandler : ILoopDownloadHandler<IllustsResponseData, ArtworkDatabaseInfo>
{
    public SearchLoopDownloadHandler()
    {
        dictionary = new();
    }

    private readonly Dictionary<ulong, ArtworkDatabaseInfo> dictionary;

    public IEnumerable<ArtworkDatabaseInfo> Get() => dictionary.Values;

    public ValueTask<string?> GetNextUrlAsync(IllustsResponseData container, CancellationToken token)
    {
        token.ThrowIfCancellationRequested();
        var artworkItems = container.GetContainer();
        if (artworkItems.Length == 0)
        {
            return ValueTask.FromResult(default(string));
        }

        var nextUrl = container.NextUrl;
        if (string.IsNullOrWhiteSpace(nextUrl))
        {
            return ValueTask.FromResult(default(string));
        }

        const string parts = "&offset=5010";
        var index = nextUrl.IndexOf(parts);
        if (index != -1)
        {
            var splitIndex = SearchUrlUtility.GetIndexOfOldestDay(artworkItems);
            for (int i = 0; i < splitIndex; i++)
            {
                var artwork = artworkItems[i];
                if (artwork.User.Id == 0)
                {
                    continue;
                }

                ref var item = ref CollectionsMarshal.GetValueRefOrAddDefault(dictionary, artwork.Id, out var exists);
                if (exists)
                {
                    OverwriteExtensions.Overwrite(ref item, artwork);
                }
                else
                {
                    item = artwork;
                }
            }

            var date = DateOnly.FromDateTime(artworkItems[splitIndex].CreateDate);
            nextUrl = SearchUrlUtility.CalculateNextUrl(nextUrl.AsSpan(0, index), date);
            return ValueTask.FromResult<string?>(nextUrl);
        }

        foreach (var artwork in artworkItems.AsSpan())
        {
            if (artwork.User.Id == 0)
            {
                continue;
            }

            ref var item = ref CollectionsMarshal.GetValueRefOrAddDefault(dictionary, artwork.Id, out var exists);
            if (exists)
            {
                OverwriteExtensions.Overwrite(ref item, artwork);
            }
            else
            {
                item = artwork;
            }
        }

        return ValueTask.FromResult(nextUrl)!;
    }

    public void Dispose()
    {
        dictionary.Clear();
    }
}
