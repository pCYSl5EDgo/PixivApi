﻿namespace PixivApi.Core.Local;

public interface IUserDatabase
{
    ValueTask<ulong> CountUserAsync(CancellationToken token);

    ValueTask<User?> GetUserAsync(ulong id, CancellationToken token);

    ValueTask<User> GetOrAddAsync(ulong id, DatabaseAddUserFunc add, CancellationToken token);

    ValueTask AddOrUpdateAsync(ulong id, DatabaseAddUserFunc add, DatabaseUpdateUserFunc update, CancellationToken token);

    IAsyncEnumerable<User> FilterAsync(IFilter<User> filter, CancellationToken token);

    IAsyncEnumerable<User> EnumerableUserAsync(CancellationToken token);
}