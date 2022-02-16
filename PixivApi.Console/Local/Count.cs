﻿using PixivApi.Core.Local;
using PixivApi.Core.Local.Filter;

namespace PixivApi.Console;

partial class LocalClient
{
    [Command("count", "")]
    public async ValueTask<int> CountAsync(
        [Option(0, $"input {IOUtility.ArtworkDatabaseDescription}")] string input,
        [Option(1, "filter json content or json file path")] string? filter = null
    )
    {
        var token = Context.CancellationToken;
        var artworkItemFilter = string.IsNullOrWhiteSpace(filter) ? null : await IOUtility.JsonDeserializeAsync<ArtworkFilter>(filter, token).ConfigureAwait(false);
        if (string.IsNullOrWhiteSpace(input))
        {
            return -1;
        }

        int count = 0;
        if (artworkItemFilter is null)
        {
            using var handle = File.OpenHandle(input, FileMode.Open, FileAccess.Read, FileShare.Read, FileOptions.Asynchronous);
            long length = RandomAccess.GetLength(handle);
            if (length == 0)
            {
                logger.LogInformation("0");
                return 0;
            }

            const int SIZE = 10;
            using var segment = ArraySegmentFromPool.Rent(SIZE);
            var memory = segment.AsMemory()[..(length < SIZE ? (int)length : SIZE)];
            var actualReadCount = await RandomAccess.ReadAsync(handle, memory, 0, token).ConfigureAwait(false);
            static int ReadCount(ReadOnlyMemory<byte> memory)
            {
                var reader = new MessagePackReader(memory);
                return reader.TryReadArrayHeader(out var header) && header != 0 && reader.TryReadArrayHeader(out var header2) ? header2 : 0;
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

            count = await ArtworkEnumerable.CountAsync(database, artworkItemFilter, token).ConfigureAwait(false);
        }

    END:
        logger.LogInformation($"{count}");
        return 0;
    }
}
