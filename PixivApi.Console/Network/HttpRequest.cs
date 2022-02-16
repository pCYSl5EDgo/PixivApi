namespace PixivApi.Console;

partial class NetworkClient : ConsoleAppBase
{
    [Command("http-get")]
    public async ValueTask GetAsync([Option(0)] string url)
    {
        if (!await Connect().ConfigureAwait(false))
        {
            return;
        }

        using HttpRequestMessage request = new(HttpMethod.Get, $"https://{ApiHost}/{url}");
        AddToHeader(request);
        var token = Context.CancellationToken;
        using var responseMessage = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, token).ConfigureAwait(false);
        responseMessage.EnsureSuccessStatusCode();
        var json = await responseMessage.Content.ReadAsStringAsync(token).ConfigureAwait(false);
        logger.LogInformation(json);
    }

    [Command("http-post")]
    public async ValueTask PostAsync(string url, string content)
    {
        if (!await Connect().ConfigureAwait(false))
        {
            return;
        }

        using HttpRequestMessage request = new(HttpMethod.Post, $"https://{ApiHost}/{url}");
        AddToHeader(request);
        var token = Context.CancellationToken;
        request.Content = new StringContent($"get_secure_url=1&{content}", new System.Text.UTF8Encoding(false));
        request.Content.Headers.ContentType = new("application/x-www-form-urlencoded");
        using var responseMessage = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, token).ConfigureAwait(false);
        responseMessage.EnsureSuccessStatusCode();
        var json = await responseMessage.Content.ReadAsStringAsync(token).ConfigureAwait(false);
        logger.LogInformation(json);
    }

    [Command("http-get-image")]
    public async ValueTask GetImageAsync([Option(0)] string url)
    {
        if (!await Connect().ConfigureAwait(false))
        {
            return;
        }

        using HttpRequestMessage request = new(HttpMethod.Get, $"https://i.pximg.net/{url}");
        request.Headers.Referrer = referer;
        var token = Context.CancellationToken;
        using var responseMessage = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, token).ConfigureAwait(false);
        responseMessage.EnsureSuccessStatusCode();
        var array = await responseMessage.Content.ReadAsByteArrayAsync(token).ConfigureAwait(false);
        File.WriteAllBytes(url.Replace('/', '_'), array);
    }
}
