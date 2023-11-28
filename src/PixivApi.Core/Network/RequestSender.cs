using Microsoft.Extensions.Logging;
using System.Net.Http.Headers;

namespace PixivApi.Core.Network;

public sealed class RequestSender
{
  private readonly ILogger<RequestSender> logger;
  private readonly IHttpClientFactory httpClientFactory;
  private readonly AuthenticationHeaderValueHolder holder;
  private readonly string hashSecret;
  private readonly TimeSpan retryTimeSpan;
  private const string ApiHost = "app-api.pixiv.net";

  public RequestSender(ILogger<RequestSender> logger, IHttpClientFactory httpClientFactory, AuthenticationHeaderValueHolder holder, ConfigSettings configSettings)
  {
    this.logger = logger;
    this.httpClientFactory = httpClientFactory;
    this.holder = holder;
    hashSecret = configSettings.HashSecret;
    retryTimeSpan = configSettings.RetryTimeSpan;
  }

  [SuppressMessage("Usage", "CA2254")]
  public async ValueTask<HttpResponseMessage> GetAsync(string url, CancellationToken token)
  {
    HttpResponseMessage responseMessage;
    var client = httpClientFactory.CreateClient();
    do
    {
      token.ThrowIfCancellationRequested();

      using (HttpRequestMessage request = new(HttpMethod.Get, url))
      {
        var authentication = await holder.GetAsync(token).ConfigureAwait(false);
        AddToHeader(request, authentication);
        responseMessage = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, token).ConfigureAwait(false);
      }

      var statusCode = responseMessage.StatusCode;
      var isBadRequest = statusCode == HttpStatusCode.BadRequest;
      if (responseMessage.IsSuccessStatusCode || (statusCode != HttpStatusCode.Forbidden && !isBadRequest))
      {
        return responseMessage;
      }

      try
      {
        if (!Console.IsOutputRedirected)
        {
          var text = isBadRequest ? "a bad request" : "forbidden";
          logger.LogWarning($"Downloading {url} is {text}. Retry {retryTimeSpan.TotalSeconds} seconds later. Time: {DateTime.Now} Restart: {DateTime.Now.Add(retryTimeSpan)}");
        }

        await Task.Delay(retryTimeSpan, token).ConfigureAwait(false);
        if (isBadRequest)
        {
          await holder.InvalidateAsync(token).ConfigureAwait(false);
        }

        if (!Console.IsOutputRedirected)
        {
          logger.LogWarning($"Restart. Time: {DateTime.Now}");
        }
      }
      finally
      {
        responseMessage.Dispose();
      }
    } while (true);
  }

  private void AddToHeader(HttpRequestMessage request, AuthenticationHeaderValue authentication)
  {
    request.Headers.Authorization = authentication;
    if (!request.TryAddToHeader(hashSecret, ApiHost))
    {
      throw new InvalidOperationException();
    }
  }
}
