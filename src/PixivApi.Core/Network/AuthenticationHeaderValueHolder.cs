using System.Net.Http.Headers;

namespace PixivApi.Core.Network;

public sealed record class AuthenticationHeaderValueHolder(ConfigSettings ConfigSettings, HttpClient HttpClient, TimeSpan LoopInterval) : IDisposable
{
    private readonly AsyncLock asyncLock = new();
    private volatile AuthenticationHeaderValue? value;

    public async ValueTask<AuthenticationHeaderValue> GetAsync(CancellationToken token)
    {
        var currentValue = value;
        if (currentValue is not null)
        {
            return currentValue;
        }

        using var @lock = await asyncLock.LockAsync(token).ConfigureAwait(false);
        if (value is null)
        {
            var accessToken = await AccessTokenUtility.GetAccessTokenAsync(HttpClient, ConfigSettings, token).ConfigureAwait(false);
            value = new("Bearer", accessToken);
        }

        return value;
    }

    public async ValueTask InvalidateAsync(CancellationToken token)
    {
        var currentValue = value;
        if (currentValue is null)
        {
            return;
        }

        using var @lock = await asyncLock.LockAsync(token).ConfigureAwait(false);
        if (currentValue == value)
        {
            value = null;
        }
    }

    public void Dispose() => asyncLock.Dispose();
}