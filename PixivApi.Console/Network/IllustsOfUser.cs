namespace PixivApi.Console;

public partial class NetworkClient
{
    [Command("illusts")]
    public ValueTask DownloadIllustsOfUserAsync
    (
        [Option(0, $"output {ArgumentDescriptions.DatabaseDescription}")] string output,
        [Option(1)] ulong id,
        [Option("a", ArgumentDescriptions.AddKindDescription)] bool addBehaviour = false,
        bool pipe = false
    )
    {
        if (string.IsNullOrWhiteSpace(output))
        {
            return ValueTask.CompletedTask;
        }

        var url = $"https://{ApiHost}/v1/user/illusts?user_id={id}";
        return DownloadArtworkResponses(output, addBehaviour, pipe, url, Context.CancellationToken);
    }
}
