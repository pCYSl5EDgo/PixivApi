using Artworks = System.Collections.Generic.IEnumerable<PixivApi.Core.Network.ArtworkResponseContent>;
using Users = System.Collections.Generic.IEnumerable<PixivApi.Core.Network.UserPreviewResponseContent>;
using Authentication = System.Net.Http.Headers.AuthenticationHeaderValue;
using QueryAsync = System.Func<string, System.Net.Http.Headers.AuthenticationHeaderValue, System.Threading.CancellationToken, System.Threading.Tasks.ValueTask<byte[]>>;
using ReconnectAsyncFunc = System.Func<System.Exception, System.Threading.CancellationToken, System.Threading.Tasks.ValueTask<System.Net.Http.Headers.AuthenticationHeaderValue>>;
using System.Net.Sockets;

namespace PixivApi.Core.Network;

public static class NetworkAsyncEnumerableHelper
{
    public static bool ShouldReconnect(this HttpRequestException e) => e.StatusCode == HttpStatusCode.BadRequest || e.InnerException is IOException { InnerException: SocketException { ErrorCode: 10054 } };
}

public sealed class DownloadArtworkAsyncEnumerable : IAsyncEnumerable<Artworks>
{
    private readonly string initialUrl;
    private readonly Authentication authentication;
    private readonly QueryAsync query;
    private readonly ReconnectAsyncFunc reconnect;

    public DownloadArtworkAsyncEnumerable(string initialUrl, Authentication authentication, QueryAsync query, ReconnectAsyncFunc reconnect)
    {
        this.initialUrl = initialUrl;
        this.authentication = authentication;
        this.query = query;
        this.reconnect = reconnect;
    }

    public Enumerator GetAsyncEnumerator(CancellationToken cancellationToken) => new(initialUrl, authentication, query, reconnect, cancellationToken);

    IAsyncEnumerator<Artworks> IAsyncEnumerable<Artworks>.GetAsyncEnumerator(CancellationToken cancellationToken) => GetAsyncEnumerator(cancellationToken);

    public sealed class Enumerator : IAsyncEnumerator<Artworks>
    {
        private string? url;
        private Authentication authentication;
        private readonly QueryAsync query;
        private readonly ReconnectAsyncFunc reconnect;
        private readonly CancellationToken cancellationToken;

        private ArtworkResponseContent[]? array;

        public Enumerator(string initialUrl, Authentication authentication, QueryAsync query, ReconnectAsyncFunc reconnect, CancellationToken cancellationToken)
        {
            this.query = query;
            url = initialUrl;
            this.authentication = authentication;
            this.reconnect = reconnect;
            this.cancellationToken = cancellationToken;
        }

        public Artworks Current => array ?? Array.Empty<ArtworkResponseContent>();

        public ValueTask DisposeAsync()
        {
            url = null;
            array = null;
            return ValueTask.CompletedTask;
        }

        public async ValueTask<bool> MoveNextAsync()
        {
            if (string.IsNullOrWhiteSpace(url))
            {
                return false;
            }

            byte[] responseByteArray;
            do
            {
                try
                {
                    responseByteArray = await query(url, authentication, cancellationToken).ConfigureAwait(false);
                }
                catch (HttpRequestException e) when (e.ShouldReconnect())
                {
                    authentication = await reconnect(e, cancellationToken).ConfigureAwait(false);
                    continue;
                }

                break;
            } while (true);

            if (responseByteArray.Length == 0)
            {
                return false;
            }

            var response = IOUtility.JsonDeserialize<IllustsResponseData>(responseByteArray.AsSpan());
            var container = response.Illusts;
            if (container is not { Length: > 0 })
            {
                return false;
            }

            if (response.NextUrl is null || response.NextUrl.Contains("&offset=5010"))
            {
                url = null;
            }
            else
            {
                url = response.NextUrl;
            }

            array = container;
            return true;
        }
    }
}

public sealed class SearchArtworkAsyncNewToOldEnumerable : IAsyncEnumerable<Artworks>
{
    public delegate string SearchNextUrl(ReadOnlySpan<char> url, DateOnly date);

    private readonly string initialUrl;
    private readonly Authentication authentication;
    private readonly QueryAsync query;
    private readonly ReconnectAsyncFunc reconnect;

    public SearchArtworkAsyncNewToOldEnumerable(string initialUrl, Authentication authentication, QueryAsync query, ReconnectAsyncFunc reconnect)
    {
        this.initialUrl = initialUrl;
        this.authentication = authentication;
        this.query = query;
        this.reconnect = reconnect;
    }

    public Enumerator GetAsyncEnumerator(CancellationToken cancellationToken) => new(initialUrl, authentication, query, reconnect, cancellationToken);

    IAsyncEnumerator<Artworks> IAsyncEnumerable<Artworks>.GetAsyncEnumerator(CancellationToken cancellationToken) => GetAsyncEnumerator(cancellationToken);

    public sealed class Enumerator : IAsyncEnumerator<Artworks>
    {
        private readonly QueryAsync query;
        private readonly CancellationToken cancellationToken;

        private string? url;
        private Authentication authentication;
        private readonly ReconnectAsyncFunc reconnect;
        private ArtworkResponseContent[]? array;

        public Enumerator(string initialUrl, Authentication authentication, QueryAsync query, ReconnectAsyncFunc reconnect, CancellationToken cancellationToken)
        {
            url = initialUrl;
            this.authentication = authentication;
            this.query = query;
            this.reconnect = reconnect;
            this.cancellationToken = cancellationToken;
        }

        public Artworks Current => array ?? Array.Empty<ArtworkResponseContent>();

        public ValueTask DisposeAsync()
        {
            url = null;
            array = null;
            return ValueTask.CompletedTask;
        }

        public async ValueTask<bool> MoveNextAsync()
        {
            if (string.IsNullOrWhiteSpace(url))
            {
                return false;
            }

            byte[] responseByteArray;
            do
            {
                try
                {
                    responseByteArray = await query(url, authentication, cancellationToken).ConfigureAwait(false);
                }
                catch (HttpRequestException e) when (e.ShouldReconnect())
                {
                    authentication = await reconnect(e, cancellationToken).ConfigureAwait(false);
                    continue;
                }

                break;
            } while (true);

            if (responseByteArray.Length == 0)
            {
                return false;
            }

            var response = IOUtility.JsonDeserialize<IllustsResponseData>(responseByteArray.AsSpan());
            var container = response.Illusts;
            if (container is not { Length: > 0 })
            {
                return false;
            }

            url = response.NextUrl;
            if (url is null || array is not { Length: > 0 })
            {
                goto DEFAULT;
            }

            const string parts = "&offset=5010";
            var partsIndex = url.IndexOf(parts);
            if (partsIndex != -1)
            {
                var dayIndex = SearchUrlUtility.GetIndexOfOldestDay(array);
                var date = DateOnly.FromDateTime(array[dayIndex].CreateDate.ToLocalTime());
                if (SearchUrlUtility.TryGetEndDate(url, out var searchDate) && date.Equals(searchDate))
                {
                    url = SearchUrlUtility.CalculateNextEndDateUrl(url.AsSpan(0, partsIndex), date.AddDays(-1));
                }
                else
                {
                    array = dayIndex == 0 ? Array.Empty<ArtworkResponseContent>() : array[..dayIndex];
                    url = SearchUrlUtility.CalculateNextEndDateUrl(url.AsSpan(0, partsIndex), date);
                }

                return true;
            }

        DEFAULT:
            array = container;
            return true;
        }
    }
}

public sealed class DownloadUserPreviewAsyncEnumerable : IAsyncEnumerable<Users>
{
    private readonly QueryAsync query;
    private readonly string initialUrl;
    private readonly Authentication authentication;
    private readonly ReconnectAsyncFunc reconnect;

    public DownloadUserPreviewAsyncEnumerable(string initialUrl, Authentication authentication, QueryAsync query, ReconnectAsyncFunc reconnect)
    {
        this.query = query;
        this.initialUrl = initialUrl;
        this.authentication = authentication;
        this.reconnect = reconnect;
    }

    public Enumerator GetAsyncEnumerator(CancellationToken cancellationToken) => new(initialUrl, authentication, query, reconnect, cancellationToken);

    IAsyncEnumerator<Users> IAsyncEnumerable<Users>.GetAsyncEnumerator(CancellationToken cancellationToken) => GetAsyncEnumerator(cancellationToken);

    public sealed class Enumerator : IAsyncEnumerator<Users>
    {
        private string? url;
        private Authentication authentication;
        private readonly QueryAsync query;
        private readonly ReconnectAsyncFunc reconnect;
        private readonly CancellationToken cancellationToken;

        private UserPreviewResponseContent[]? array;

        public Enumerator(string initialUrl, Authentication authentication, QueryAsync query, ReconnectAsyncFunc reconnect, CancellationToken cancellationToken)
        {
            url = initialUrl;
            this.authentication = authentication;
            this.query = query;
            this.reconnect = reconnect;
            this.cancellationToken = cancellationToken;
        }

        public Users Current => array ?? Array.Empty<UserPreviewResponseContent>();

        public ValueTask DisposeAsync()
        {
            array = null;
            url = null;
            return ValueTask.CompletedTask;
        }

        public async ValueTask<bool> MoveNextAsync()
        {
            if (string.IsNullOrWhiteSpace(url))
            {
                return false;
            }

            byte[] responseByteArray;
            do
            {
                try
                {
                    responseByteArray = await query(url, authentication, cancellationToken).ConfigureAwait(false);
                }
                catch (HttpRequestException e) when (e.ShouldReconnect())
                {
                    authentication = await reconnect(e, cancellationToken).ConfigureAwait(false);
                    continue;
                }

                break;
            } while (true);

            if (responseByteArray.Length == 0)
            {
                return false;
            }

            var response = IOUtility.JsonDeserialize<UserPreviewsResponseData>(responseByteArray.AsSpan());
            var container = response.UserPreviews;
            if (container is not { Length: > 0 })
            {
                return false;
            }

            if (response.NextUrl is null || response.NextUrl.Contains("&offset=5010"))
            {
                url = null;
            }
            else
            {
                url = response.NextUrl;
            }

            array = container;
            return true;
        }
    }
}
