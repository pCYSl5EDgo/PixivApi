#pragma warning disable CA2254
using Microsoft.Extensions.Logging;
using Artworks = System.Collections.Generic.IEnumerable<PixivApi.Core.Network.ArtworkResponseContent>;
using QueryWithRetryAndReconnectAsync = System.Func<string, System.Threading.CancellationToken, System.Threading.Tasks.ValueTask<System.Net.Http.HttpResponseMessage>>;
using Users = System.Collections.Generic.IEnumerable<PixivApi.Core.Network.UserPreviewResponseContent>;

namespace PixivApi.Core.Network;

public static class NetworkAsyncEnumerableHelper
{
  public static async ValueTask<byte[]> GetByteArrayAsync(QueryWithRetryAndReconnectAsync query, string url, CancellationToken token)
  {
    using var responseMessage = await query(url, token).ConfigureAwait(false);
    responseMessage.EnsureSuccessStatusCode();
    var answer = await responseMessage.Content.ReadAsByteArrayAsync(token).ConfigureAwait(false);
    return answer;
  }
}

public sealed class DownloadArtworkAsyncEnumerable : IAsyncEnumerable<Artworks>
{
  private readonly string initialUrl;
  private readonly QueryWithRetryAndReconnectAsync query;
  private readonly ILogger logger;

  public DownloadArtworkAsyncEnumerable(string initialUrl, QueryWithRetryAndReconnectAsync query, ILogger logger)
  {
    this.initialUrl = initialUrl;
    this.query = query;
    this.logger = logger;
  }

  public Enumerator GetAsyncEnumerator(CancellationToken cancellationToken) => new(initialUrl, query, logger, cancellationToken);

  IAsyncEnumerator<Artworks> IAsyncEnumerable<Artworks>.GetAsyncEnumerator(CancellationToken cancellationToken) => GetAsyncEnumerator(cancellationToken);

  public sealed class Enumerator : IAsyncEnumerator<Artworks>
  {
    private string? url;
    private readonly ILogger logger;
    private readonly bool logTrace;
    private readonly QueryWithRetryAndReconnectAsync query;
    private readonly CancellationToken cancellationToken;

    private ArtworkResponseContent[]? array;

    public Enumerator(string initialUrl, QueryWithRetryAndReconnectAsync query, ILogger logger, CancellationToken cancellationToken)
    {
      url = initialUrl;
      this.logger = logger;
      logTrace = logger.IsEnabled(LogLevel.Trace);
      this.query = query;
      this.cancellationToken = cancellationToken;
    }

    public Artworks Current => array ?? [];

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

      var responseByteArray = await NetworkAsyncEnumerableHelper.GetByteArrayAsync(query, url, cancellationToken).ConfigureAwait(false);
      if (responseByteArray.Length == 0)
      {
        return false;
      }

      if (logTrace)
      {
        logger.LogTrace(System.Text.Encoding.UTF8.GetString(responseByteArray));
      }

      var response = IOUtility.JsonDeserialize<IllustsResponseData>(responseByteArray.AsSpan());
      array = response.Illusts;
      if (array is not { Length: > 0 })
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

      return true;
    }
  }
}

public sealed class SearchArtworkAsyncNewToOldEnumerable : IAsyncEnumerable<Artworks>
{
  public delegate string SearchNextUrl(ReadOnlySpan<char> url, DateOnly date);

  private readonly string initialUrl;
  private readonly QueryWithRetryAndReconnectAsync query;
  private readonly ILogger logger;

  public SearchArtworkAsyncNewToOldEnumerable(string initialUrl, QueryWithRetryAndReconnectAsync query, ILogger logger)
  {
    this.initialUrl = initialUrl;
    this.query = query;
    this.logger = logger;
  }

  public Enumerator GetAsyncEnumerator(CancellationToken cancellationToken) => new(initialUrl, query, logger, cancellationToken);

  IAsyncEnumerator<Artworks> IAsyncEnumerable<Artworks>.GetAsyncEnumerator(CancellationToken cancellationToken) => GetAsyncEnumerator(cancellationToken);

  public sealed class Enumerator : IAsyncEnumerator<Artworks>
  {
    private readonly QueryWithRetryAndReconnectAsync query;
    private readonly ILogger logger;
    private readonly bool logTrace;
    private readonly CancellationToken cancellationToken;

    private string? url;
    private ArtworkResponseContent[]? array;

    public Enumerator(string initialUrl, QueryWithRetryAndReconnectAsync query, ILogger logger, CancellationToken cancellationToken)
    {
      url = initialUrl;
      this.query = query;
      this.logger = logger;
      logTrace = logger.IsEnabled(LogLevel.Trace);
      this.cancellationToken = cancellationToken;
    }

    public Artworks Current => array ?? [];

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

      var responseByteArray = await NetworkAsyncEnumerableHelper.GetByteArrayAsync(query, url, cancellationToken).ConfigureAwait(false);
      if (responseByteArray.Length == 0)
      {
        return false;
      }

      if (logTrace)
      {
        logger.LogTrace(System.Text.Encoding.UTF8.GetString(responseByteArray));
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
          array = dayIndex == 0 ? [] : array[..dayIndex];
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
  private readonly QueryWithRetryAndReconnectAsync query;
  private readonly ILogger? logger;
  private readonly string initialUrl;

  public DownloadUserPreviewAsyncEnumerable(string initialUrl, QueryWithRetryAndReconnectAsync query, ILogger? logger)
  {
    this.initialUrl = initialUrl;
    this.query = query;
    this.logger = logger is null ? null : logger.IsEnabled(LogLevel.Trace) ? logger : null;
  }

  public Enumerator GetAsyncEnumerator(CancellationToken cancellationToken) => new(initialUrl, query, logger, cancellationToken);

  IAsyncEnumerator<Users> IAsyncEnumerable<Users>.GetAsyncEnumerator(CancellationToken cancellationToken) => GetAsyncEnumerator(cancellationToken);

  public sealed class Enumerator : IAsyncEnumerator<Users>
  {
    private string? url;
    private readonly QueryWithRetryAndReconnectAsync query;
    private readonly ILogger? logger;
    private readonly CancellationToken cancellationToken;

    private UserPreviewResponseContent[]? array;

    public Enumerator(string initialUrl, QueryWithRetryAndReconnectAsync query, ILogger? logger, CancellationToken cancellationToken)
    {
      url = initialUrl;
      this.query = query;
      this.logger = logger;
      this.cancellationToken = cancellationToken;
    }

    public Users Current => array ?? [];

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

      var responseByteArray = await NetworkAsyncEnumerableHelper.GetByteArrayAsync(query, url, cancellationToken).ConfigureAwait(false);
      if (responseByteArray.Length == 0)
      {
        return false;
      }

      if (logger is not null)
      {
        logger.LogTrace(System.Text.Encoding.UTF8.GetString(responseByteArray));
      }

      var response = IOUtility.JsonDeserialize<UserPreviewsResponseData>(responseByteArray.AsSpan());
      array = response.UserPreviews;
      if (array is not { Length: > 0 })
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

      return true;
    }
  }
}
#pragma warning restore CA2254
