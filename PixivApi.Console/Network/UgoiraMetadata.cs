namespace PixivApi;

partial class NetworkClient : ConsoleAppBase
{
    private async ValueTask GetUgoiraMetadataAsync(ulong id)
    {
        using HttpRequestMessage request = new(HttpMethod.Get, $"https://{ApiHost}/v1/ugoira/metadata?illust_id={id}");
        if (!request.TryAddToHeader(config.HashSecret, ApiHost))
        {
            throw new InvalidOperationException();
        }

        var token = Context.CancellationToken;
        using var responseMessage = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, token).ConfigureAwait(false);
        responseMessage.EnsureSuccessStatusCode();
        var json = await responseMessage.Content.ReadAsStringAsync(token).ConfigureAwait(false);
        logger.LogInformation(json);
    }
}
