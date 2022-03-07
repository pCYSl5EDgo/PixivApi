namespace PixivApi.Console;

public sealed partial class NetworkClient
{
    private sealed record class AuthenticationHeaderValueHolder(ConfigSettings ConfigSettings, HttpClient HttpClient, TimeSpan LoopInterval) : IDisposable
    {
        private readonly AsyncLock asyncLock = new();
        private AuthenticationHeaderValue? value;
        private DateTime expires;

        public async ValueTask<AuthenticationHeaderValue> ConnectAsync(CancellationToken token)
        {
            if (value is not null)
            {
                throw new InvalidOperationException("ConnectAsync must be called only once.");
            }

            var accessToken = await AccessTokenUtility.GetAccessTokenAsync(HttpClient, ConfigSettings, token).ConfigureAwait(false);
            Interlocked.Exchange(ref value, new("Bearer", accessToken));
            expires = DateTime.UtcNow + LoopInterval;
            return value;
        }

        public async ValueTask<AuthenticationHeaderValue> GetAsync(CancellationToken token)
        {
            if (value is null || DateTime.UtcNow.CompareTo(expires) > 0)
            {
                // renew the value;
                using var @lock = await asyncLock.LockAsync(token).ConfigureAwait(false);
                if (value is null || DateTime.UtcNow.CompareTo(expires) > 0)
                {
                    var accessToken = await AccessTokenUtility.GetAccessTokenAsync(HttpClient, ConfigSettings, token).ConfigureAwait(false);
                    Interlocked.Exchange(ref value, new("Bearer", accessToken));
                    expires = DateTime.UtcNow + LoopInterval;
                }
            }

            return value;
        }

        public async ValueTask<AuthenticationHeaderValue> RegetAsync(CancellationToken token)
        {
            // renew the value;
            using var @lock = await asyncLock.LockAsync(token).ConfigureAwait(false);
            if (value is null || DateTime.UtcNow.CompareTo(expires) > 0)
            {
                var accessToken = await AccessTokenUtility.GetAccessTokenAsync(HttpClient, ConfigSettings, token).ConfigureAwait(false);
                Interlocked.Exchange(ref value, new("Bearer", accessToken));
                expires = DateTime.UtcNow + LoopInterval;
            }

            return value;
        }

        public void Dispose() => asyncLock.Dispose();
    }
}
