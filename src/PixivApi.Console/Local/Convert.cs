namespace PixivApi.Console;

public partial class LocalClient
{
    [Command("convert")]
    public async ValueTask ConvertAsync()
    {
        var token = Context.CancellationToken;
        var outputFactory = Context.ServiceProvider.GetRequiredService<Core.SqliteDatabase.DatabaseFactory>();
        var output = await outputFactory.RentAsync(token).ConfigureAwait(false);
        //var input = await databaseFactory.RentAsync(token).ConfigureAwait(false);
        try
        {
            //var users = await input.EnumerateUserAsync(token).ToArrayAsync(token).ConfigureAwait(false);
            //var artworks = await input.EnumerateArtworkAsync(token).ToArrayAsync(token).ConfigureAwait(false);
            //var tagDictionary = new Dictionary<string, ulong>();
            //var toolDictionary = new Dictionary<string, ulong>();

            //static void Increment(Dictionary<string, ulong> dictionary, string? text)
            //{
            //    if (string.IsNullOrEmpty(text))
            //    {
            //        return;
            //    }

            //    ++CollectionsMarshal.GetValueRefOrAddDefault(dictionary, text, out _);
            //}

            //foreach (var item in users)
            //{
            //    if (item.ExtraTags is { Length: > 0 } tags)
            //    {
            //        foreach (var number in tags)
            //        {
            //            var text = await input.GetTagAsync(number, token).ConfigureAwait(false);
            //            Increment(tagDictionary, text);
            //        }
            //    }
            //}

            //foreach (var item in artworks)
            //{
            //    foreach (var number in item.ExtraFakeTags ?? Array.Empty<uint>())
            //    {
            //        var text = await input.GetTagAsync(number, token).ConfigureAwait(false);
            //        Increment(tagDictionary, text);
            //    }

            //    foreach (var number in item.Tags)
            //    {
            //        var text = await input.GetTagAsync(number, token).ConfigureAwait(false);
            //        Increment(tagDictionary, text);
            //    }

            //    foreach (var number in item.ExtraTags ?? Array.Empty<uint>())
            //    {
            //        var text = await input.GetTagAsync(number, token).ConfigureAwait(false);
            //        Increment(tagDictionary, text);
            //    }

            //    foreach (var number in item.Tools)
            //    {
            //        var text = await input.GetToolAsync(number, token).ConfigureAwait(false);
            //        Increment(toolDictionary, text);
            //    }
            //}

            //var tagArray = tagDictionary.Keys.ToArray();
            //var tagCounts = tagDictionary.Values.ToArray();
            //tagCounts.AsSpan().Sort(tagArray.AsSpan(), static (x, y) => y.CompareTo(x));
            //var tagConversion = new Dictionary<uint, uint>(tagArray.Length);
            //foreach (var text in tagArray)
            //{
            //    var newId = await output.RegisterTagAsync(text, token).ConfigureAwait(false);
            //    var oldId = await input.FindTagAsync(text, token).ConfigureAwait(false) ?? throw new NullReferenceException();
            //    tagConversion.Add(oldId, newId);
            //}

            //var toolArray = toolDictionary.Keys.ToArray();
            //var toolCounts = toolDictionary.Values.ToArray();
            //toolCounts.AsSpan().Sort(toolArray.AsSpan(), static (x, y) => y.CompareTo(x));
            //var toolConversion = new Dictionary<uint, uint>(toolArray.Length);
            //foreach (var text in toolArray)
            //{
            //    var newId = await output.RegisterToolAsync(text, token).ConfigureAwait(false);
            //    var oldId = await input.FindToolAsync(text, token).ConfigureAwait(false) ?? throw new NullReferenceException();
            //    toolConversion.Add(oldId, newId);
            //}
        }
        finally
        {
            //databaseFactory.Return(ref input);
            outputFactory.Return(ref output);
        }
    }
}
