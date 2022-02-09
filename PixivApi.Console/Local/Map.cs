namespace PixivApi;

partial class LocalClient
{
    [Command("map", "")]
    public async ValueTask<int> MapAsync(
        [Option(0, $"input {IOUtility.ArtworkDatabaseDescription}")] string input,
        [Option(1, IOUtility.FilterDescription)] string filter
    )
    {
        var info = new FileInfo(input);
        if (!info.Exists || info.Length == 0)
        {
            goto END;
        }

        var token = Context.CancellationToken;
        var itemFilter = await IOUtility.JsonParseAsync<ArtworkDatabaseInfoFilter>(filter, token).ConfigureAwait(false);

        if (!input.EndsWith(IOUtility.ArtworkDatabaseFileExtension))
        {
            goto END;
        }

        var array = await IOUtility.MessagePackDeserializeAsync<ArtworkDatabaseInfo[]>(info.FullName, token).ConfigureAwait(false);
        if (array is null)
        {
            logger.LogInformation("null");
            goto END;
        }

        if (itemFilter is null)
        {
            logger.LogInformation(IOUtility.JsonStringSerialize(array));
        }
        else
        {
            logger.LogInformation(IOUtility.JsonStringSerialize(await ArtworkDatabaseInfoEnumerable.CreateAsync(array, itemFilter, token).ConfigureAwait(false)));
        }

    END:
        return 0;
    }
}
