namespace PixivApi;

partial class NetworkClient
{
    [Command("detail")]
    public async ValueTask<int> DetailAsync
    (
        [Option(0, "user id")] ulong id
    )
    {
        if (!await Connect().ConfigureAwait(false))
        {
            return -3;
        }

        var token = Context.CancellationToken;
        var url = $"https://{ApiHost}/v1/user/detail?user_id={id}";
        using HttpRequestMessage request = new(HttpMethod.Get, url);
        if (!request.TryAddToHeader(config.HashSecret, ApiHost))
        {
            throw new InvalidOperationException();
        }

        using var responseMessage = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, token).ConfigureAwait(false);
        var json = await responseMessage.Content.ReadAsStringAsync(token).ConfigureAwait(false);
        logger.LogInformation(json);
        return 0;
    }
}
