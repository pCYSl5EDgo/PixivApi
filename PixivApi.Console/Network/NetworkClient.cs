using PixivApi.Core;
using PixivApi.Core.Network;
using System.Net;
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

    public NetworkClient(ConfigSettings config, ILogger<NetworkClient> logger, HttpClient client, FinderFacade finderFacade, ConverterFacade converterFacade)
    {
        configSettings = config;
        this.logger = logger;
        this.client = client;
        finder = finderFacade;
        converter = converterFacade;
    }

    private void AddToHeader(HttpRequestMessage request)
    {
        if (!request.TryAddToHeader(configSettings.HashSecret, ApiHost))
        {
            throw new InvalidOperationException();
        }
    }

    private async ValueTask<bool> Connect()
    {
        var accessToken = await AccessTokenUtility.GetAccessTokenAsync(client, configSettings, Context.CancellationToken).ConfigureAwait(false);
        if (accessToken is null)
        {
            logger.LogError(VirtualCodes.BrightRedColor + "Failed to get access token." + VirtualCodes.NormalizeColor);
            return false;
        }

        return client.TryAddToDefaultHeader(configSettings, accessToken);
    }

    private async ValueTask<bool> Reconnect()
    {
        var accessToken = await AccessTokenUtility.GetAccessTokenAsync(client, configSettings, Context.CancellationToken).ConfigureAwait(false);
        if (accessToken is null)
        {
            logger.LogError(VirtualCodes.BrightRedColor + "Failed to get access token." + VirtualCodes.NormalizeColor);
            return false;
        }

        var headers = client.DefaultRequestHeaders;
        headers.Authorization = new("Bearer", accessToken);
        return true;
    }

    private async ValueTask ReconnectAsync(Exception exception, bool pipe, CancellationToken token)
    {
        if (!pipe)
        {
            logger.LogInformation(exception, $"{VirtualCodes.BrightYellowColor}Wait for {configSettings.RetryTimeSpan.TotalSeconds} seconds to reconnect. Time: {DateTime.Now} Restart: {DateTime.Now.Add(configSettings.RetryTimeSpan)}{VirtualCodes.NormalizeColor}");
        }

        await Task.Delay(configSettings.RetryTimeSpan, token).ConfigureAwait(false);
        if (await Reconnect().ConfigureAwait(false))
        {
            if (!pipe)
            {
                logger.LogInformation($"{VirtualCodes.BrightYellowColor}Reconnect.{VirtualCodes.NormalizeColor}");
            }
        }
        else
        {
            if (!pipe)
            {
                logger.LogError($"{VirtualCodes.BrightRedColor}Reconnection failed.{VirtualCodes.NormalizeColor}");
            }

            ExceptionDispatchInfo.Throw(exception);
        }
    }

    private async ValueTask<byte[]> RetryGetAsync(string url, bool pipe, CancellationToken token)
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
            catch (HttpRequestException e) when (e.StatusCode == HttpStatusCode.Forbidden)
            {
                token.ThrowIfCancellationRequested();
                if (!pipe)
                {
                    logger.LogWarning($"{VirtualCodes.BrightYellowColor}Downloading {url} is forbidden. Retry {configSettings.RetrySeconds} seconds later. Time: {DateTime.Now}{VirtualCodes.NormalizeColor}");
                }

                await Task.Delay(configSettings.RetryTimeSpan, token).ConfigureAwait(false);
                if (!pipe)
                {
                    logger.LogWarning($"{VirtualCodes.BrightYellowColor}Restart.{VirtualCodes.NormalizeColor}");
                }

                continue;
            }
        } while (true);
    }
}
