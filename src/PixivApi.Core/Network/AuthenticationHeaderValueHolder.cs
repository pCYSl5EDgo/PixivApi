using System.Net.Http.Headers;

namespace PixivApi.Core.Network;

public sealed class AuthenticationHeaderValueHolder : IDisposable
{
    public readonly ConfigSettings ConfigSettings;
    public readonly HttpClient HttpClient;
    public readonly TimeSpan LoopInterval;
    private readonly AsyncLock asyncLock = new();
    private readonly AuthenticationHeaderValue?[] values;
    private int index;

    public AuthenticationHeaderValueHolder(ConfigSettings configSettings, HttpClient httpClient, TimeSpan loopInterval)
    {
        ConfigSettings = configSettings;
        HttpClient = httpClient;
        LoopInterval = loopInterval;
        values = ConfigSettings.RefreshTokens.Length == 0 ? Array.Empty<AuthenticationHeaderValue?>() : new AuthenticationHeaderValue?[ConfigSettings.RefreshTokens.Length];
        index = 0;
    }

    private int CalcNextIndex(int index) => ++index == values.Length ? 0 : index;

    public async ValueTask<AuthenticationHeaderValue> GetAsync(CancellationToken token)
    {
        var currentIndex = index;
        var currentValue = values[currentIndex];
        if (currentValue is not null)
        {
            return currentValue;
        }

        using var @lock = await asyncLock.LockAsync(token).ConfigureAwait(false);
        currentValue = values[currentIndex];
        if (currentValue is not null)
        {
            return currentValue;
        }

        var accessToken = await AccessTokenUtility.GetAccessTokenAsync(HttpClient, ConfigSettings, currentIndex, token).ConfigureAwait(false);
        return values[currentIndex] = new("Bearer", accessToken);
    }

    public async ValueTask InvalidateAsync(CancellationToken token)
    {
        using var @lock = await asyncLock.LockAsync(token).ConfigureAwait(false);
        var currentIndex = index;
        var nextIndex = CalcNextIndex(currentIndex);
        _ = Interlocked.Exchange(ref index, nextIndex);
        Interlocked.Exchange(ref values[currentIndex], null);
    }

    public void Dispose() => asyncLock.Dispose();
}