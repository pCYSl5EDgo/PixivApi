﻿namespace PixivApi.Console;

public partial class NetworkClient : ConsoleAppBase
{
    [Command("http-get")]
    public async ValueTask GetAsync([Option(0)] string url)
    {
        var token = Context.CancellationToken;
        var authentication = await holder.GetAsync(token).ConfigureAwait(false);
        using HttpRequestMessage request = new(HttpMethod.Get, $"https://{ApiHost}/{url}");
        request.Headers.Authorization = authentication;
        if (!request.TryAddToHeader(configSettings.HashSecret, ApiHost))
        {
            throw new InvalidOperationException();
        }

        using var responseMessage = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, token).ConfigureAwait(false);
        responseMessage.EnsureSuccessStatusCode();
        var json = await responseMessage.Content.ReadAsStringAsync(token).ConfigureAwait(false);
        Context.Logger.LogInformation(json);
    }

    [Command("http-post")]
    public async ValueTask PostAsync([Option(0, "Url Path Name")] string url, [Option(1, "Content")] string content, bool isJson = false)
    {
        var token = Context.CancellationToken;
        var authentication = await holder.GetAsync(token).ConfigureAwait(false);
        using HttpRequestMessage request = new(HttpMethod.Post, $"https://{ApiHost}/{url}");
        request.Headers.Authorization = authentication;
        if (!request.TryAddToHeader(configSettings.HashSecret, ApiHost))
        {
            throw new InvalidOperationException();
        }

        request.Content = new StringContent($"get_secure_url=1&{content}", new System.Text.UTF8Encoding(false), isJson ? "application/json" : "application/x-www-form-urlencoded");
        if (Context.Logger.IsEnabled(LogLevel.Trace))
        {
            Context.Logger.LogTrace(content);
        }

        using var responseMessage = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, token).ConfigureAwait(false);
        if (Context.Logger.IsEnabled(LogLevel.Trace))
        {
            Context.Logger.LogTrace(await responseMessage.Content.ReadAsStringAsync().ConfigureAwait(false));
        }

        responseMessage.EnsureSuccessStatusCode();
        var json = await responseMessage.Content.ReadAsStringAsync(token).ConfigureAwait(false);
        Context.Logger.LogInformation(json);
    }

    [Command("http-get-image")]
    public async ValueTask GetImageAsync([Option(0)] string url)
    {
        var token = Context.CancellationToken;
        var authentication = await holder.GetAsync(token).ConfigureAwait(false);
        using HttpRequestMessage request = new(HttpMethod.Get, $"https://i.pximg.net/{url}");
        var headers = request.Headers;
        headers.Referrer = referer;
        headers.Authorization = authentication;
        using var responseMessage = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, token).ConfigureAwait(false);
        responseMessage.EnsureSuccessStatusCode();
        var array = await responseMessage.Content.ReadAsByteArrayAsync(token).ConfigureAwait(false);
        File.WriteAllBytes(url.Replace('/', '_'), array);
    }
}
