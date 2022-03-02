using PixivApi.Core;
using PixivApi.Core.Network;

namespace PixivApi.Console;

public sealed partial class NetworkClient
{
    private sealed record class AuthenticationHeaderValueHolder(ConfigSettings ConfigSettings, HttpClient HttpClient) : ISingleUpdater<AuthenticationHeaderValue>
    {
        private Task<AuthenticationHeaderValue>? taskBearerToken;

        public ref Task<AuthenticationHeaderValue>? GetTask => ref taskBearerToken;

        public async Task<AuthenticationHeaderValue> UpdateAsync(CancellationToken token)
        {
            var accessToken = await AccessTokenUtility.GetAccessTokenAsync(HttpClient, ConfigSettings, token).ConfigureAwait(false);
            if (accessToken is null)
            {
                throw new IOException();
            }

            return new("Bearer", accessToken);
        }
    }
}
