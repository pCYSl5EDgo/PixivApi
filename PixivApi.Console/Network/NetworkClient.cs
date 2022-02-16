using System.Net;

namespace PixivApi.Console;

[Command("net")]
public sealed partial class NetworkClient : ConsoleAppBase
{
    private const string ApiHost = "app-api.pixiv.net";
    private readonly ConfigSettings config;
    private readonly ILogger<NetworkClient> logger;
    private readonly HttpClient client;
    private readonly CancellationTokenSource cancellationTokenSource;

    public NetworkClient(ConfigSettings config, ILogger<NetworkClient> logger, HttpClient client, CancellationTokenSource cancellationTokenSource)
    {
        this.config = config;
        this.logger = logger;
        this.client = client;
        this.cancellationTokenSource = cancellationTokenSource;
    }

    private void AddToHeader(HttpRequestMessage request)
    {
        if (!request.TryAddToHeader(config.HashSecret, ApiHost))
        {
            throw new InvalidOperationException();
        }
    }

    private async ValueTask<bool> Connect()
    {
        var accessToken = await AccessTokenUtility.GetAccessTokenAsync(client, config, Context.CancellationToken).ConfigureAwait(false);
        if (accessToken is null)
        {
            logger.LogError(IOUtility.ErrorColor + "Failed to get access token." + IOUtility.NormalizeColor);
            return false;
        }

        return client.TryAddToDefaultHeader(config, accessToken);
    }

    private async ValueTask<bool> Reconnect()
    {
        var accessToken = await AccessTokenUtility.GetAccessTokenAsync(client, config, Context.CancellationToken).ConfigureAwait(false);
        if (accessToken is null)
        {
            logger.LogError(IOUtility.ErrorColor + "Failed to get access token." + IOUtility.NormalizeColor);
            return false;
        }

        var headers = client.DefaultRequestHeaders;
        headers.Authorization = new("Bearer", accessToken);
        return true;
    }

    private async ValueTask<byte[]?> RetryGetAsync(string url, CancellationToken token)
    {
        do
        {
            using HttpRequestMessage request = new(HttpMethod.Get, url);
            AddToHeader(request);
            string? reasonPhrase = null;
            try
            {
                using var responseMessage = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, token).ConfigureAwait(false);
                reasonPhrase = responseMessage.ReasonPhrase;
                responseMessage.EnsureSuccessStatusCode();
                return await responseMessage.Content.ReadAsByteArrayAsync(token).ConfigureAwait(false);
            }
            catch (HttpRequestException e)
            {
                if (e.StatusCode.HasValue)
                {
                    switch (e.StatusCode.Value)
                    {
                        case HttpStatusCode.Forbidden:
                            token.ThrowIfCancellationRequested();
                            logger.LogWarning($"{IOUtility.WarningColor}Downloading {url} is forbidden. Retry {config.RetrySeconds} seconds later. Time: {DateTime.Now}{IOUtility.NormalizeColor}");
                            await Task.Delay(config.RetryTimeSpan, token).ConfigureAwait(false);
                            logger.LogWarning($"{IOUtility.WarningColor}Restart.{IOUtility.NormalizeColor}");
                            continue;
                        #region Http Status Code
                        case HttpStatusCode.OK:
                        case HttpStatusCode.Created:
                        case HttpStatusCode.Accepted:
                        case HttpStatusCode.NonAuthoritativeInformation:
                        case HttpStatusCode.NoContent:
                        case HttpStatusCode.ResetContent:
                        case HttpStatusCode.PartialContent:
                        case HttpStatusCode.MultiStatus:
                        case HttpStatusCode.AlreadyReported:
                        case HttpStatusCode.IMUsed:
                            throw new InvalidProgramException("Http Success cannot happen.", e);
                        case HttpStatusCode.Continue:
                        case HttpStatusCode.SwitchingProtocols:
                        case HttpStatusCode.Processing:
                        case HttpStatusCode.EarlyHints:
                        case HttpStatusCode.Ambiguous:
                        // case HttpStatusCode.MultipleChoices:
                        case HttpStatusCode.Moved:
                        //case HttpStatusCode.MovedPermanently:
                        case HttpStatusCode.Found:
                        //case HttpStatusCode.Redirect:
                        case HttpStatusCode.RedirectMethod:
                        // case HttpStatusCode.SeeOther:
                        case HttpStatusCode.NotModified:
                        case HttpStatusCode.UseProxy:
                        case HttpStatusCode.Unused:
                        case HttpStatusCode.RedirectKeepVerb:
                        // case HttpStatusCode.TemporaryRedirect:
                        case HttpStatusCode.PermanentRedirect:
                        case HttpStatusCode.BadRequest:
                        case HttpStatusCode.Unauthorized:
                        case HttpStatusCode.PaymentRequired:
                        case HttpStatusCode.NotFound:
                        case HttpStatusCode.MethodNotAllowed:
                        case HttpStatusCode.NotAcceptable:
                        case HttpStatusCode.ProxyAuthenticationRequired:
                        case HttpStatusCode.RequestTimeout:
                        case HttpStatusCode.Conflict:
                        case HttpStatusCode.Gone:
                        case HttpStatusCode.LengthRequired:
                        case HttpStatusCode.PreconditionFailed:
                        case HttpStatusCode.RequestEntityTooLarge:
                        case HttpStatusCode.RequestUriTooLong:
                        case HttpStatusCode.UnsupportedMediaType:
                        case HttpStatusCode.RequestedRangeNotSatisfiable:
                        case HttpStatusCode.ExpectationFailed:
                        case HttpStatusCode.MisdirectedRequest:
                        case HttpStatusCode.UnprocessableEntity:
                        case HttpStatusCode.Locked:
                        case HttpStatusCode.FailedDependency:
                        case HttpStatusCode.UpgradeRequired:
                        case HttpStatusCode.PreconditionRequired:
                        case HttpStatusCode.TooManyRequests:
                        case HttpStatusCode.RequestHeaderFieldsTooLarge:
                        case HttpStatusCode.UnavailableForLegalReasons:
                        case HttpStatusCode.InternalServerError:
                        case HttpStatusCode.NotImplemented:
                        case HttpStatusCode.BadGateway:
                        case HttpStatusCode.ServiceUnavailable:
                        case HttpStatusCode.GatewayTimeout:
                        case HttpStatusCode.HttpVersionNotSupported:
                        case HttpStatusCode.VariantAlsoNegotiates:
                        case HttpStatusCode.InsufficientStorage:
                        case HttpStatusCode.LoopDetected:
                        case HttpStatusCode.NotExtended:
                        case HttpStatusCode.NetworkAuthenticationRequired:
                        default:
                            break;
                            #endregion
                    }
                }
                else
                {
                    logger.LogError(e, $"{IOUtility.ErrorColor}Long wait {config.RetrySeconds} seconds to reconnect. Status Code: {e.StatusCode}\r\nCurrent Url: {url}{IOUtility.NormalizeColor}");
                    await Task.Delay(config.RetryTimeSpan, token).ConfigureAwait(false);
                    if (await Reconnect().ConfigureAwait(false))
                    {
                        continue;
                    }
                }

                logger.LogError(e, $"{IOUtility.ErrorColor}Reason: {reasonPhrase} Url: {url}{IOUtility.NormalizeColor}");
                throw;
            }
        } while (true);
    }
}
