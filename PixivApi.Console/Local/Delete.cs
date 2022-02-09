namespace PixivApi;

partial class LocalClient
{
    [Command("delete", "")]
    public async ValueTask<int> DeleteAsync(
        [Option(0, $"input database *{IOUtility.ArtworkDatabaseFileExtension} file path")] string input,
        [Option(1, IOUtility.FilterDescription)] string filter
    )
    {
        var found = IOUtility.FindArtworkDatabase(input, true)!;
        if (found is null)
        {
            goto END;
        }

        var info = new FileInfo(found);
        if (info.Length == 0)
        {
            goto END;
        }

        var token = Context.CancellationToken;
        var artworkItemFilter = await IOUtility.JsonParseAsync<ArtworkDatabaseInfoFilter>(filter, token).ConfigureAwait(false);
        if (artworkItemFilter is null)
        {
            goto END;
        }

        if (artworkItemFilter is null)
        {
            goto FILE;
        }

        var array = await IOUtility.MessagePackDeserializeAsync<ArtworkDatabaseInfo[]>(info.FullName, token).ConfigureAwait(false);
        var list = new List<ArtworkDatabaseInfo>();
        if (array is not { Length: > 0 })
        {
            goto END;
        }

        int count = 0;
        foreach (var item in array)
        {
            if (artworkItemFilter.Filter(item))
            {
                ++count;
            }
            else
            {
                list.Add(item);
            }
        }

        if (count > 0)
        {
            if (count == array.Length)
            {
                goto FILE;
            }

            logger.LogWarning($"{IOUtility.ErrorColor}DO YOU REALLY WANT TO DELETE? DELETE COUNT: {count}. no/yes{IOUtility.NormalizeColor}");
            if (Console.ReadLine() is "y" or "yes" or "Y" or "Yes")
            {
                await IOUtility.MessagePackSerializeAsync(info.FullName, list, FileMode.Create).ConfigureAwait(false);
            }
        }

        logger.LogWarning($"{IOUtility.ErrorColor}Delete Count: {count}{IOUtility.NormalizeColor}");

    END:
        return 0;

    FILE:
        logger.LogError(IOUtility.ErrorColor + "THIS OPERATION DELETE EVERYTHING. JUST DELETE THE FILE." + IOUtility.NormalizeColor);
        goto END;
    }
}
