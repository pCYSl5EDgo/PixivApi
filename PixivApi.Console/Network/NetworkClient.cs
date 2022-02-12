using System.Net;

namespace PixivApi;

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
            if (!request.TryAddToHeader(config.HashSecret, ApiHost))
            {
                throw new InvalidOperationException();
            }

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
                throw e;
            }
        } while (true);
    }

    private async ValueTask<IEnumerable<T>> LoopDownloadAsync<THandler, TContainer, T>(string url, THandler handler)
        where THandler : ILoopDownloadHandler<TContainer, T>
        where TContainer : INext, IArrayContainer<T>
        where T : IComparable<T>
    {
        var token = Context.CancellationToken;
        try
        {
            do
            {
                var content = await RetryGetAsync(url, token).ConfigureAwait(false);
                var container = IOUtility.JsonDeserialize<TContainer>(content);
                if (container is null)
                {
                    break;
                }

                url = (await handler.GetNextUrlAsync(container, token).ConfigureAwait(false))!;
            } while (!string.IsNullOrWhiteSpace(url));
        }
        catch
        {
            return handler.Get();
        }

        return handler.Get();
    }

    private async ValueTask OverwriteLoopDownloadAsync<THandler, TMergeHandler, TContainer, TElement>(
        string output,
        string url,
        OverwriteKind overwrite,
        bool isPipeOutput,
        THandler defaultHandler,
        TMergeHandler mergeHandler,
        Func<string, CancellationToken, ValueTask<TElement[]?>> deserializeAsync,
        Func<string, IEnumerable<TElement>, FileMode, ValueTask> serializeAsync
    )
        where THandler : ILoopDownloadHandler<TContainer, TElement>
        where TMergeHandler : IMergeLoopDownloadHandler<TContainer, TElement>
        where TContainer : INext, IArrayContainer<TElement>
        where TElement : IEquatable<TElement>, IComparable<TElement>, IOverwrite<TElement>
    {
        var fileInfo = new FileInfo(output);
        var token = Context.CancellationToken;
        switch (overwrite)
        {
            case OverwriteKind.ClearAndAdd:
                await ClearAndAddDownloadAsync<THandler, TContainer, TElement>(output, url, isPipeOutput, defaultHandler, serializeAsync).ConfigureAwait(false);
                break;
            case OverwriteKind.SearchAndAdd:
                {
                    await SearchAndAddDownloadAsync<THandler, TContainer, TElement>(output, url, isPipeOutput, defaultHandler, deserializeAsync, serializeAsync, token).ConfigureAwait(false);
                }
                break;
            default:
                if (!fileInfo.Exists || fileInfo.Length == 0)
                {
                    goto case OverwriteKind.ClearAndAdd;
                }
                else
                {
                    await AddDownloadAsync<TMergeHandler, TContainer, TElement>(output, url, isPipeOutput, mergeHandler, deserializeAsync, serializeAsync, token).ConfigureAwait(false);
                }
                break;
        }
    }

    private async ValueTask AddDownloadAsync<TMergeHandler, TContainer, TElement>(string output, string url, bool isPipeOutput, TMergeHandler mergeHandler, Func<string, CancellationToken, ValueTask<TElement[]?>> deserializeAsync, Func<string, IEnumerable<TElement>, FileMode, ValueTask> serializeAsync, CancellationToken token)
        where TMergeHandler : IMergeLoopDownloadHandler<TContainer, TElement>
        where TContainer : INext, IArrayContainer<TElement>
        where TElement : IEquatable<TElement>, IComparable<TElement>, IOverwrite<TElement>
    {
        if (!isPipeOutput)
        {
            logger.LogInformation($"edit add {output}");
        }

        IEnumerable<TElement>? enumerable = null;
        var artworkItems = await deserializeAsync(output, token).ConfigureAwait(false);
        mergeHandler.Initialize(artworkItems);
        try
        {
            enumerable = await LoopDownloadAsync<TMergeHandler, TContainer, TElement>(url, mergeHandler).ConfigureAwait(false);
        }
        finally
        {
            var result = new HashSet<TElement>(artworkItems ?? Array.Empty<TElement>());
            if (enumerable is null)
            {
                if (!isPipeOutput)
                {
                    logger.LogInformation($"Count: {result.Count}, Add: 0");
                }
            }
            else
            {
                var count = 0U;
                foreach (var item in enumerable)
                {
                    if (result.TryGetValue(item, out var actual))
                    {
                        actual.Overwrite(item);
                    }
                    else
                    {
                        ++count;
                        result.Add(item);
                        if (isPipeOutput)
                        {
                            if (item.ToString() is string text)
                            {
                                logger.LogInformation(text);
                            }
                        }
                        else
                        {
                            logger.LogInformation($"{IOUtility.SuccessColor}download success. Item: {item}{IOUtility.NormalizeColor}");
                        }
                    }
                }

                if (!isPipeOutput)
                {
                    logger.LogInformation($"Count: {result.Count}, Add: {count}");
                }

                await serializeAsync(output, result, FileMode.Create).ConfigureAwait(false);
            }
        }
    }

    private async ValueTask SearchAndAddDownloadAsync<THandler, TContainer, TElement>(string output, string url, bool isPipeOutput, THandler defaultHandler, Func<string, CancellationToken, ValueTask<TElement[]?>> deserializeAsync, Func<string, IEnumerable<TElement>, FileMode, ValueTask> serializeAsync, CancellationToken token)
        where THandler : ILoopDownloadHandler<TContainer, TElement>
        where TContainer : INext, IArrayContainer<TElement>
        where TElement : IEquatable<TElement>, IComparable<TElement>, IOverwrite<TElement>
    {
        if (!isPipeOutput)
        {
            logger.LogInformation($"edit all-add {output}");
        }

        IEnumerable<TElement>? enumerable = null;
        try
        {
            enumerable = await LoopDownloadAsync<THandler, TContainer, TElement>(url, defaultHandler).ConfigureAwait(false);
        }
        finally
        {
            var result = new HashSet<TElement>(await deserializeAsync(output, token).ConfigureAwait(false) ?? Array.Empty<TElement>());
            if (enumerable is null)
            {
                if (!isPipeOutput)
                {
                    logger.LogInformation($"Count: {result.Count}, Add: 0");
                }
            }
            else
            {
                var count = 0U;
                foreach (var item in enumerable)
                {
                    if (result.TryGetValue(item, out var actual))
                    {
                        actual.Overwrite(item);
                    }
                    else
                    {
                        ++count;
                        result.Add(item);
                        if (isPipeOutput)
                        {
                            if (item.ToString() is string text)
                            {
                                logger.LogInformation(text);
                            }
                        }
                        else
                        {
                            logger.LogInformation($"{IOUtility.SuccessColor}download success. Item: {item}{IOUtility.NormalizeColor}");
                        }
                    }
                }

                if (!isPipeOutput)
                {
                    logger.LogInformation($"Count: {result.Count}, Add: {count}");
                }

                await serializeAsync(output, result, FileMode.Create).ConfigureAwait(false);
            }
        }
    }

    private async ValueTask ClearAndAddDownloadAsync<THandler, TContainer, TElement>(string output, string url, bool isPipeOutput, THandler defaultHandler, Func<string, IEnumerable<TElement>, FileMode, ValueTask> serializeAsync)
        where THandler : ILoopDownloadHandler<TContainer, TElement>
        where TContainer : INext, IArrayContainer<TElement>
        where TElement : IEquatable<TElement>, IComparable<TElement>, IOverwrite<TElement>
    {
        if (!isPipeOutput)
        {
            logger.LogInformation($"create {output}");
        }

        TElement[]? result = null;
        try
        {
            var tmp = await LoopDownloadAsync<THandler, TContainer, TElement>(url, defaultHandler).ConfigureAwait(false);
            result = tmp?.ToArray();
        }
        finally
        {
            if (result is { Length: > 0 })
            {
                await serializeAsync(output, result, FileMode.Create).ConfigureAwait(false);
            }

            if (!isPipeOutput)
            {
                if (result is { Length: > 0 })
                {
                    logger.LogInformation($"Count: {result.Length}");
                }
                else
                {
                    logger.LogInformation("file is not created. Count: 0");
                }
            }
        }
    }
}
