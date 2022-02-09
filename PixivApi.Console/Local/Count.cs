namespace PixivApi;

partial class LocalClient
{
    [Command("count", "")]
    public async ValueTask<int> CountAsync(
        [Option(0, $"input {IOUtility.ArtworkDatabaseDescription} or {IOUtility.UserDatabaseDescription}")] string input,
        [Option(1, "filter json content or json file path")] string? filter = null
    )
    {
        var token = Context.CancellationToken;
        var artworkItemFilter = await IOUtility.JsonParseAsync<ArtworkDatabaseInfoFilter>(filter, token).ConfigureAwait(false);
        if (input.EndsWith(IOUtility.ArtworkDatabaseFileExtension))
        {
            var info = new FileInfo(input);
            if (!info.Exists || info.Length == 0)
            {
                goto END;
            }

            int count = 0;
            if (artworkItemFilter is not null)
            {
                var array = await IOUtility.MessagePackDeserializeAsync<ArtworkDatabaseInfo[]>(info.FullName, token).ConfigureAwait(false);
                if (array is not { Length: > 0 })
                {
                    goto END;
                }

                count = await ArtworkDatabaseInfoEnumerable.CountAsync(array, artworkItemFilter, token).ConfigureAwait(false);
            }
            else
            {
                using var handle = File.OpenHandle(info.FullName, FileMode.Open, FileAccess.Read, FileShare.Read, FileOptions.Asynchronous);
                using var segment = ArraySegmentFromPool.Rent(5);
                var memory = segment.AsMemory()[..(info.Length < 5 ? (int)info.Length : 5)];
                var actualReadCount = await RandomAccess.ReadAsync(handle, memory, 0, token).ConfigureAwait(false);
                static int ReadCount(ReadOnlyMemory<byte> memory)
                {
                    var reader = new MessagePackReader(memory);
                    return reader.TryReadArrayHeader(out var header) ? header : 0;
                }

                count = ReadCount(segment.AsReadOnlyMemory()[..actualReadCount]);
            }

            logger.LogInformation($"{count}");
            return 0;
        }

    END:
        logger.LogInformation("0");
        return 0;
    }
}
