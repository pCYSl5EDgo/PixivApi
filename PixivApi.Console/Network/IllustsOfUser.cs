namespace PixivApi;

partial class NetworkClient
{
    [Command("illusts")]
    public async ValueTask<int> DownloadIllustsOfUserAsync
    (
        [Option(null, IOUtility.OverwriteKindDescription)] string overwrite = "add",
        bool pipe = false
    )
    {
        if (!await Connect().ConfigureAwait(false))
        {
            return -3;
        }
        
        var token = Context.CancellationToken;
        for (var line = Console.ReadLine(); !string.IsNullOrWhiteSpace(line) && ulong.TryParse(line.AsSpan().Trim(), out var id); line = Console.ReadLine())
        {
            var output = $"illusts_{id}{IOUtility.ArtworkDatabaseFileExtension}";
            var url = $"https://{ApiHost}/v1/user/illusts?user_id={id}";
            await OverwriteLoopDownloadAsync<DefaultLoopDownloadHandler<IllustsResponseData, ArtworkDatabaseInfo>, MergeLoopDownloadHandler<IllustsResponseData, ArtworkDatabaseInfo>, IllustsResponseData, ArtworkDatabaseInfo>(
                output, url, OverwriteKindExtensions.Parse(overwrite), pipe,
                new(),
                new(),
                IOUtility.MessagePackDeserializeAsync<ArtworkDatabaseInfo[]>,
                IOUtility.MessagePackSerializeAsync
            ).ConfigureAwait(false);
        }
        
        return 0;
    }
}
