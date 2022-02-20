using PixivApi.Core.Local;

namespace PixivApi.Console;

public partial class LocalClient
{
    [Command("count", "")]
    public async ValueTask<int> CountAsync(
        [Option(0, $"input {ArgumentDescriptions.DatabaseDescription}")] string input,
        [Option(1, ArgumentDescriptions.FilterDescription)] string? filter = null
    )
    {
        var token = Context.CancellationToken;
        var artworkItemFilter = string.IsNullOrWhiteSpace(filter) ? null : await IOUtility.JsonDeserializeAsync<ArtworkFilter>(filter, token).ConfigureAwait(false);
        if (string.IsNullOrWhiteSpace(input))
        {
            return -1;
        }

        var count = 0;
        if (artworkItemFilter is null)
        {
            using var handle = File.OpenHandle(input, FileMode.Open, FileAccess.Read, FileShare.Read, FileOptions.Asynchronous);
            var length = RandomAccess.GetLength(handle);
            if (length == 0)
            {
                logger.LogInformation("0");
                return 0;
            }

            const int SIZE = 20;
            using var segment = ArraySegmentFromPool.Rent(SIZE);
            var memory = segment.AsMemory()[..(length < SIZE ? (int)length : SIZE)];
            var actualReadCount = await RandomAccess.ReadAsync(handle, memory, 0, token).ConfigureAwait(false);
            static int ReadCount(ReadOnlyMemory<byte> memory)
            {
                var reader = new MessagePackReader(memory);
                if (!reader.TryReadArrayHeader(out var header) || header == 0)
                {
                    return 0;
                }

                // skip major version
                reader.Skip();
                // skip minor version
                reader.Skip();

                return reader.TryReadArrayHeader(out var header2) ? header2 : 0;
            }

            count = ReadCount(segment.AsReadOnlyMemory()[..actualReadCount]);
        }
        else
        {
            var database = await IOUtility.MessagePackDeserializeAsync<DatabaseFile>(input, token).ConfigureAwait(false);
            if (database is not { Artworks.Length: > 0 })
            {
                goto END;
            }

            var parallelOptions = new ParallelOptions()
            {
                CancellationToken = token,
                MaxDegreeOfParallelism = configSettings.MaxParallel,
            };
            count = await ArtworkEnumerable.CountAsync(configSettings, database, artworkItemFilter, parallelOptions).ConfigureAwait(false);
        }

    END:
        logger.LogInformation($"{count}");
        return 0;
    }
}
