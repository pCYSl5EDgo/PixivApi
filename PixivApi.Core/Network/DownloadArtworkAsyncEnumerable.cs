using Artworks = System.Collections.Generic.IEnumerable<PixivApi.Core.Network.Artwork>;
using Users = System.Collections.Generic.IEnumerable<PixivApi.Core.Network.UserPreview>;
using Authentication = System.Net.Http.Headers.AuthenticationHeaderValue;
using QueryAsync = System.Func<string, System.Net.Http.Headers.AuthenticationHeaderValue, bool, System.Threading.CancellationToken, System.Threading.Tasks.ValueTask<byte[]>>;
using ReconnectAsyncFunc = System.Func<System.Exception, bool, System.Threading.CancellationToken, System.Threading.Tasks.ValueTask<System.Net.Http.Headers.AuthenticationHeaderValue>>;

namespace PixivApi.Core.Network;

public sealed class DownloadArtworkAsyncEnumerable : IAsyncEnumerable<Artworks>
{
    private readonly string initialUrl;
    private readonly Authentication authentication;
    private readonly QueryAsync query;
    private readonly ReconnectAsyncFunc reconnect;
    private readonly bool pipe;

    public DownloadArtworkAsyncEnumerable(string initialUrl, Authentication authentication, QueryAsync query, ReconnectAsyncFunc reconnect, bool pipe)
    {
        this.initialUrl = initialUrl;
        this.authentication = authentication;
        this.query = query;
        this.reconnect = reconnect;
        this.pipe = pipe;
    }

    public Enumerator GetAsyncEnumerator(CancellationToken cancellationToken) => new(initialUrl, authentication, query, reconnect, pipe, cancellationToken);

    IAsyncEnumerator<Artworks> IAsyncEnumerable<Artworks>.GetAsyncEnumerator(CancellationToken cancellationToken) => GetAsyncEnumerator(cancellationToken);

    public sealed class Enumerator : IAsyncEnumerator<Artworks>
    {
        private string? url;
        private Authentication authentication;
        private readonly QueryAsync query;
        private readonly ReconnectAsyncFunc reconnect;
        private readonly bool pipe;
        private readonly CancellationToken cancellationToken;

        private Artwork[]? array;

        public Enumerator(string initialUrl, Authentication authentication, QueryAsync query, ReconnectAsyncFunc reconnect, bool pipe, CancellationToken cancellationToken)
        {
            this.query = query;
            url = initialUrl;
            this.authentication = authentication;
            this.reconnect = reconnect;
            this.pipe = pipe;
            this.cancellationToken = cancellationToken;
        }

        public Artworks Current => array ?? Array.Empty<Artwork>();

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
                    responseByteArray = await query(url, authentication, pipe, cancellationToken).ConfigureAwait(false);
                }
                catch (HttpRequestException e) when (e.StatusCode == HttpStatusCode.BadRequest)
                {
                    authentication = await reconnect(e, pipe, cancellationToken).ConfigureAwait(false);
                    continue;
                }

                break;
            } while (true);

            if (responseByteArray.Length == 0)
            {
                return false;
            }

            var response = IOUtility.JsonDeserialize<IllustsResponseData>(responseByteArray.AsSpan());
            var container = response.GetContainer();
            if (container is not { Length: > 0 })
            {
                return false;
            }
            else if (array is not null && array.Length > container.Length)
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
    private readonly bool pipe;

    public SearchArtworkAsyncNewToOldEnumerable(string initialUrl, Authentication authentication, QueryAsync query, ReconnectAsyncFunc reconnect, bool pipe)
    {
        this.initialUrl = initialUrl;
        this.authentication = authentication;
        this.query = query;
        this.reconnect = reconnect;
        this.pipe = pipe;
    }

    public Enumerator GetAsyncEnumerator(CancellationToken cancellationToken) => new(initialUrl, authentication, query, reconnect, pipe, cancellationToken);

    IAsyncEnumerator<Artworks> IAsyncEnumerable<Artworks>.GetAsyncEnumerator(CancellationToken cancellationToken) => GetAsyncEnumerator(cancellationToken);

    public sealed class Enumerator : IAsyncEnumerator<Artworks>
    {
        private readonly QueryAsync query;
        private readonly CancellationToken cancellationToken;

        private string? url;
        private Authentication authentication;
        private readonly ReconnectAsyncFunc reconnect;
        private readonly bool pipe;
        private Artwork[]? array;

        public Enumerator(string initialUrl, Authentication authentication, QueryAsync query, ReconnectAsyncFunc reconnect, bool pipe, CancellationToken cancellationToken)
        {
            url = initialUrl;
            this.authentication = authentication;
            this.query = query;
            this.reconnect = reconnect;
            this.pipe = pipe;
            this.cancellationToken = cancellationToken;
        }

        public Artworks Current => array ?? Array.Empty<Artwork>();

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
                    responseByteArray = await query(url, authentication, pipe, cancellationToken).ConfigureAwait(false);
                }
                catch (HttpRequestException e) when (e.StatusCode == HttpStatusCode.BadRequest)
                {
                    authentication = await reconnect(e, pipe, cancellationToken).ConfigureAwait(false);
                    continue;
                }

                break;
            } while (true);

            if (responseByteArray.Length == 0)
            {
                return false;
            }

            var response = IOUtility.JsonDeserialize<IllustsResponseData>(responseByteArray.AsSpan());
            var container = response.GetContainer();
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
                    array = dayIndex == 0 ? Array.Empty<Artwork>() : array[..dayIndex];
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
    private readonly bool pipe;

    public DownloadUserPreviewAsyncEnumerable(string initialUrl, Authentication authentication, QueryAsync query, ReconnectAsyncFunc reconnect, bool pipe)
    {
        this.query = query;
        this.initialUrl = initialUrl;
        this.authentication = authentication;
        this.reconnect = reconnect;
        this.pipe = pipe;
    }

    public Enumerator GetAsyncEnumerator(CancellationToken cancellationToken) => new(initialUrl, authentication, query, reconnect, pipe, cancellationToken);

    IAsyncEnumerator<Users> IAsyncEnumerable<Users>.GetAsyncEnumerator(CancellationToken cancellationToken) => GetAsyncEnumerator(cancellationToken);

    public sealed class Enumerator : IAsyncEnumerator<Users>
    {
        private string? url;
        private Authentication authentication;
        private readonly QueryAsync query;
        private readonly ReconnectAsyncFunc reconnect;
        private readonly bool pipe;
        private readonly CancellationToken cancellationToken;

        private UserPreview[]? array;

        public Enumerator(string initialUrl, Authentication authentication, QueryAsync query, ReconnectAsyncFunc reconnect, bool pipe, CancellationToken cancellationToken)
        {
            url = initialUrl;
            this.authentication = authentication;
            this.query = query;
            this.reconnect = reconnect;
            this.pipe = pipe;
            this.cancellationToken = cancellationToken;
        }

        public Users Current => array ?? Array.Empty<UserPreview>();

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
                    responseByteArray = await query(url, authentication, pipe, cancellationToken).ConfigureAwait(false);
                }
                catch (HttpRequestException e) when (e.StatusCode == HttpStatusCode.BadRequest)
                {
                    authentication = await reconnect(e, pipe, cancellationToken).ConfigureAwait(false);
                    continue;
                }

                break;
            } while (true);

            if (responseByteArray.Length == 0)
            {
                return false;
            }

            var response = IOUtility.JsonDeserialize<UserPreviewsResponseData>(responseByteArray.AsSpan());
            var container = response.GetContainer();
            if (container is not { Length: > 0 })
            {
                return false;
            }
            else if (array is not null && array.Length > container.Length)
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
