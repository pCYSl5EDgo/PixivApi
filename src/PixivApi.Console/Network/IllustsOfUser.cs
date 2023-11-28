namespace PixivApi.Console;

public partial class NetworkClient
{
  [Command("illusts")]
  public ValueTask DownloadIllustsOfUserAsync
  (
      [Option(0)] ulong id,
      [Option("a", ArgumentDescriptions.AddKindDescription)] bool addBehaviour = false,
      [Option("d")] bool download = false
  )
  {
    if (string.IsNullOrWhiteSpace(configSettings.DatabaseFilePath))
    {
      return ValueTask.CompletedTask;
    }

    var url = $"https://{ApiHost}/v1/user/illusts?user_id={id}";
    return DownloadArtworkResponses(addBehaviour, download, url, Context.CancellationToken);
  }
}
