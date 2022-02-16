using Artworks = System.Collections.Generic.IEnumerable<PixivApi.Core.Network.Artwork>;
using Users = System.Collections.Generic.IEnumerable<PixivApi.Core.Network.UserPreview>;
using QueryAsync = System.Func<string, System.Threading.CancellationToken, System.Threading.Tasks.ValueTask<byte[]?>>;

namespace PixivApi.Core.Network;

public sealed class DownloadArtworkAsyncEnumerable : IAsyncEnumerable<Artworks>
{
    private readonly string initialUrl;
    private readonly QueryAsync query;

    public DownloadArtworkAsyncEnumerable(QueryAsync query, string initialUrl)
    {
        this.initialUrl = initialUrl;
        this.query = query;
    }

    public Enumerator GetAsyncEnumerator(CancellationToken cancellationToken) => new(query, initialUrl, cancellationToken);

    IAsyncEnumerator<Artworks> IAsyncEnumerable<Artworks>.GetAsyncEnumerator(CancellationToken cancellationToken) => GetAsyncEnumerator(cancellationToken);

    public sealed class Enumerator : IAsyncEnumerator<Artworks>
    {
        private readonly QueryAsync query;
        private readonly CancellationToken cancellationToken;

        private string? url;
        private Artwork[]? array;

        public Enumerator(QueryAsync query, string initialUrl, CancellationToken cancellationToken)
        {
            url = initialUrl;
            this.query = query;
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

            var responseByteArray = await query(url, cancellationToken).ConfigureAwait(false);
            if (responseByteArray is not { Length: > 0 })
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

public sealed class SearchArtworkAsyncEnumerable : IAsyncEnumerable<Artworks>
{
    private readonly QueryAsync query;
    private readonly string initialUrl;

    public SearchArtworkAsyncEnumerable(QueryAsync query, string initialUrl)
    {
        this.query = query;
        this.initialUrl = initialUrl;
    }

    public Enumerator GetAsyncEnumerator(CancellationToken cancellationToken) => new(query, initialUrl, cancellationToken);

    IAsyncEnumerator<Artworks> IAsyncEnumerable<Artworks>.GetAsyncEnumerator(CancellationToken cancellationToken) => GetAsyncEnumerator(cancellationToken);

    public sealed class Enumerator : IAsyncEnumerator<Artworks>
    {
        private readonly QueryAsync query;
        private readonly CancellationToken cancellationToken;

        private string? url;
        private Artwork[]? array;

        public Enumerator(QueryAsync query, string initialUrl, CancellationToken cancellationToken)
        {
            url = initialUrl;
            this.query = query;
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

            var responseByteArray = await query(url, cancellationToken).ConfigureAwait(false);
            if (responseByteArray is not { Length: > 0 })
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
                if (url is not null && array is { Length: > 0 })
                {
                    const string parts = "&offset=5010";
                    var index = url.IndexOf(parts);
                    if (index != -1)
                    {
                        var lastDayIndex = SearchUrlUtility.GetIndexOfOldestDay(array);
                        var date = DateOnly.FromDateTime(array[lastDayIndex].CreateDate);
                        url = SearchUrlUtility.CalculateNextUrl(url.AsSpan(0, index), date);
                        array = lastDayIndex == 0 ? Array.Empty<Artwork>() : array[..lastDayIndex];
                    }
                }
            }

            array = container;
            return true;
        }
    }
}

public sealed class DownloadUserPreviewAsyncEnumerable : IAsyncEnumerable<Users>
{
    private readonly string initialUrl;
    private readonly QueryAsync query;

    public DownloadUserPreviewAsyncEnumerable(QueryAsync query, string initialUrl)
    {
        this.initialUrl = initialUrl;
        this.query = query;
    }

    public Enumerator GetAsyncEnumerator(CancellationToken cancellationToken) => new(query, initialUrl, cancellationToken);

    IAsyncEnumerator<Users> IAsyncEnumerable<Users>.GetAsyncEnumerator(CancellationToken cancellationToken) => GetAsyncEnumerator(cancellationToken);

    public sealed class Enumerator : IAsyncEnumerator<Users>
    {
        private readonly QueryAsync query;
        private readonly CancellationToken cancellationToken;

        private UserPreview[]? array;
        private string? url;


        public Enumerator(QueryAsync query, string initialUrl, CancellationToken cancellationToken)
        {
            this.query = query;
            this.url = initialUrl;
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

            var responseByteArray = await query(url, cancellationToken).ConfigureAwait(false);
            if (responseByteArray is not { Length: > 0 })
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
