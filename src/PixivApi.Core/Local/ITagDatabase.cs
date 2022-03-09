﻿namespace PixivApi.Core.Local;

public interface ITagDatabase
{
    ValueTask<ulong> CountTagAsync(CancellationToken token);

    ValueTask<string?> GetTagAsync(uint id, CancellationToken token);

    ValueTask<uint?> FindTagAsync(string key, CancellationToken token);

    ValueTask<uint> RegisterTagAsync(string value, CancellationToken token);

    IAsyncEnumerable<uint> EnumeratePartialMatchAsync(string key, CancellationToken token);
}