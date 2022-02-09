namespace PixivApi;

partial class LocalClient
{
    [Command("ranking")]
    public async ValueTask<int> RankingAsync(
        [Option(0, $"input {IOUtility.ArtworkDatabaseDescription}")] string input,
        [Option(1)] string query,
        [Option(2)] int count = 10,
        [Option(3)] int offset = 0,
        [Option("f")] string? filter = null
    )
    {
        var artworkItemFilter = await IOUtility.JsonParseAsync<ArtworkDatabaseInfoFilter>(filter, Context.CancellationToken).ConfigureAwait(false);
        var info = new FileInfo(input);
        if (!info.Exists || info.Length == 0)
        {
            return 0;
        }

        IEnumerable<ArtworkDatabaseInfo> enumerable = await IOUtility.MessagePackDeserializeAsync<ArtworkDatabaseInfo[]>(input, Context.CancellationToken).ConfigureAwait(false) ?? throw new NullReferenceException();
        if (artworkItemFilter is not null)
        {
            enumerable = await ArtworkDatabaseInfoEnumerable.CreateAsync((ArtworkDatabaseInfo[])enumerable, artworkItemFilter, Context.CancellationToken).ConfigureAwait(false);
        }

        IEnumerable<(string, ulong, ulong)> xs;
        string kind = "";
        switch (query)
        {
            case "count":
                xs = enumerable.GroupBy(x => x.User.Id, (k, xs) =>
                {
                    var f = xs.ToArray();
                    return (f[0].User.Name, f[0].User.Id, (ulong)f.Length);
                }).OrderByDescending(x => x.Item3).Skip(offset).Take(count);
                kind = "Count";
                break;
            case "max-view":
                xs = enumerable.GroupBy(x => x.User.Id, (k, xs) =>
                {
                    var f = xs.ToArray();
                    return (f[0].User.Name, f[0].User.Id, f.Max(x => x.TotalView));
                }).OrderByDescending(x => x.Item3).Skip(offset).Take(count);
                kind = "Max View";
                break;
            case "view":
                xs = enumerable.GroupBy(x => x.User.Id, (k, xs) =>
                {
                    var f = xs.ToArray();
                    return (f[0].User.Name, f[0].User.Id, f.Aggregate(0UL, (a, p) => a + p.TotalView));
                }).OrderByDescending(x => x.Item3).Skip(offset).Take(count);
                kind = "View";
                break;
            case "average-view":
                xs = enumerable.GroupBy(x => x.User.Id, (k, xs) =>
                {
                    var f = xs.ToArray();
                    return (f[0].User.Name, f[0].User.Id, (ulong)(f.Aggregate(0UL, (a, p) => a + p.TotalView) / (double)f.Length));
                }).OrderByDescending(x => x.Item3).Skip(offset).Take(count);
                kind = "Average View";
                break;
            case "max-bookmark":
                xs = enumerable.GroupBy(x => x.User.Id, (k, xs) =>
                {
                    var f = xs.ToArray();
                    return (f[0].User.Name, f[0].User.Id, f.Max(x => x.TotalBookmarks));
                }).OrderByDescending(x => x.Item3).Skip(offset).Take(count);
                kind = "Max Bookmark";
                break;
            case "bookmark":
                xs = enumerable.GroupBy(x => x.User.Id, (k, xs) =>
                {
                    var f = xs.ToArray();
                    return (f[0].User.Name, f[0].User.Id, f.Aggregate(0UL, (a, p) => a + p.TotalBookmarks));
                }).OrderByDescending(x => x.Item3).Skip(offset).Take(count);
                kind = "Bookmark";
                break;
            case "average-bookmark":
                xs = enumerable.GroupBy(x => x.User.Id, (k, xs) =>
                {
                    var f = xs.ToArray();
                    return (f[0].User.Name, f[0].User.Id, (ulong)(f.Aggregate(0UL, (a, p) => a + p.TotalBookmarks) / (double)f.Length));
                }).OrderByDescending(x => x.Item3).Skip(offset).Take(count);
                kind = "Average Bookmark";
                break;
            default:
                return 0;
        }

        foreach (var (Name, Id, Length) in xs)
        {
#pragma warning disable CA2254
            logger.LogInformation($"Name: {Name} Id: {Id} {kind}: {Length}");
#pragma warning restore CA2254
        }
        return 0;
    }
}
