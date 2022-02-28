namespace PixivApi.Plugin.JpegXl;

internal static class Utility
{
    public static async ValueTask ExecuteAsync(ILogger? logger, string exePath, string input, string output)
    {
        var enumerable = ProcessX.StartAsync(exePath, arguments: $"'{input}' '{output}' --effort=9");
        if (logger is null)
        {
            await enumerable.WaitAsync(default).ConfigureAwait(false);
        }
        else
        {
            await foreach (var item in enumerable)
            {
                logger.LogInformation(item);
            }
        }
    }
}
