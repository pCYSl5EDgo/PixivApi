using PixivApi.Core;
using PixivApi.Core.Network;
using System.Runtime.ExceptionServices;

namespace PixivApi.Console;

[Command("net")]
public sealed partial class NetworkClient : ConsoleAppBase
{
    private const string ApiHost = "app-api.pixiv.net";
    private readonly ConfigSettings configSettings;
    private readonly ILogger<NetworkClient> logger;
    private readonly HttpClient client;
    private readonly FinderFacade finder;
    private readonly ConverterFacade converter;
    private readonly AuthenticationHeaderValueHolder authenticationHeaderValueHolder;

    public NetworkClient(ConfigSettings config, ILogger<NetworkClient> logger, HttpClient client, FinderFacade finderFacade, ConverterFacade converterFacade)
    {
        configSettings = config;
        this.logger = logger;
        this.client = client;
        finder = finderFacade;
        converter = converterFacade;
        authenticationHeaderValueHolder = new(config, client, configSettings.ReconnectWaitIntervalTimeSpan, configSettings.ReconnectLoopIntervalTimeSpan);
    }

    private void AddToHeader(HttpRequestMessage request, AuthenticationHeaderValue authentication)
    {
        request.Headers.Authorization = authentication;
        if (!request.TryAddToHeader(configSettings.HashSecret, ApiHost))
        {
            throw new InvalidOperationException();
        }
    }

    /// <summary>
    /// This must be called from single thread.
    /// </summary>
    private Task<AuthenticationHeaderValue> ConnectAsync(CancellationToken token)
    {
        client.AddToDefaultHeader(configSettings);
        return authenticationHeaderValueHolder.GetTask ??= authenticationHeaderValueHolder.UpdateAsync(token);
    }

    private async ValueTask<AuthenticationHeaderValue> ReconnectAsync(bool pipe, CancellationToken token)
    {
        if (!pipe)
        {
            logger.LogInformation($"{VirtualCodes.BrightYellowColor}Wait for {configSettings.RetryTimeSpan.TotalSeconds} seconds to reconnect. Time: {DateTime.Now} Restart: {DateTime.Now.Add(configSettings.RetryTimeSpan)}{VirtualCodes.NormalizeColor}");
        }

        await Task.Delay(configSettings.RetryTimeSpan, token).ConfigureAwait(false);
        var authentication = await SingleUpdateUtility.GetAsync(authenticationHeaderValueHolder, token).ConfigureAwait(false);
        if (authentication is null)
        {
            if (!pipe)
            {
                logger.LogError($"{VirtualCodes.BrightRedColor}Reconnection failed.{VirtualCodes.NormalizeColor}");
            }

            throw new IOException();
        }
        else
        {
            if (!pipe)
            {
                logger.LogInformation($"{VirtualCodes.BrightYellowColor}Reconnect.{VirtualCodes.NormalizeColor}");
            }
        }

        return authentication;
    }

    private async ValueTask<AuthenticationHeaderValue> ReconnectAsync(Exception exception, bool pipe, CancellationToken token)
    {
        if (!pipe)
        {
            logger.LogInformation($"{VirtualCodes.BrightYellowColor}Wait for {configSettings.RetryTimeSpan.TotalSeconds} seconds to reconnect. Time: {DateTime.Now} Restart: {DateTime.Now.Add(configSettings.RetryTimeSpan)}{VirtualCodes.NormalizeColor}");
        }

        await Task.Delay(configSettings.RetryTimeSpan, token).ConfigureAwait(false);
        var authentication = await SingleUpdateUtility.GetAsync(authenticationHeaderValueHolder, token).ConfigureAwait(false);
        if (authentication is null)
        {
            if (!pipe)
            {
                logger.LogError($"{VirtualCodes.BrightRedColor}Reconnection failed. Time: {DateTime.Now}{VirtualCodes.NormalizeColor}");
            }

            ExceptionDispatchInfo.Throw(exception);
        }
        else
        {
            if (!pipe)
            {
                logger.LogInformation($"{VirtualCodes.BrightYellowColor}Reconnect. Time: {DateTime.Now}{VirtualCodes.NormalizeColor}");
            }
        }

        return authentication;
    }

    private async ValueTask<byte[]> RetryGetAsync(string url, AuthenticationHeaderValue authentication, bool pipe, CancellationToken token)
    {
        do
        {
            using HttpRequestMessage request = new(HttpMethod.Get, url);
            AddToHeader(request, authentication);
            using var responseMessage = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, token).ConfigureAwait(false);
            if (responseMessage.IsSuccessStatusCode)
            {
                return await responseMessage.Content.ReadAsByteArrayAsync(token).ConfigureAwait(false);
            }

            if (responseMessage.StatusCode != HttpStatusCode.Forbidden)
            {
                responseMessage.EnsureSuccessStatusCode();
            }

            token.ThrowIfCancellationRequested();
            if (!pipe)
            {
                logger.LogWarning($"{VirtualCodes.BrightYellowColor}Downloading {url} is forbidden. Retry {configSettings.RetrySeconds} seconds later. Time: {DateTime.Now} Restart: {DateTime.Now.Add(configSettings.RetryTimeSpan)}{VirtualCodes.NormalizeColor}");
            }

            await Task.Delay(configSettings.RetryTimeSpan, token).ConfigureAwait(false);
            if (!pipe)
            {
                logger.LogWarning($"{VirtualCodes.BrightYellowColor}Restart. Time: {DateTime.Now}{VirtualCodes.NormalizeColor}");
            }
        } while (true);
    }
}
