using System.Runtime.CompilerServices;

namespace PixivApi.Plugin.JpegXl;

internal static class Utility
{
    private static string CreateArgument(string input, string output)
    {
        DefaultInterpolatedStringHandler handler = $@"""";
        AppendChangeToSlash(ref handler, input.AsSpan());
        handler.AppendLiteral("\" \"");
        AppendChangeToSlash(ref handler, output.AsSpan());
        handler.AppendLiteral("\" --effort=7 --num_threads=");
        handler.AppendFormatted(Environment.ProcessorCount);
        return handler.ToStringAndClear();
    }

    private static void AppendChangeToSlash(ref DefaultInterpolatedStringHandler handler, ReadOnlySpan<char> span)
    {
        var enumerator = span.EnumerateRunes();
        while (enumerator.MoveNext())
        {
            var rune = enumerator.Current;
            if (rune.Value == '\\')
            {
                handler.AppendLiteral("/");
            }
            else
            {
                handler.AppendFormatted(rune);
            }
        }
    }

    public static async ValueTask ExecuteAsync(ILogger? logger, string exePath, string input, string output, string workingDirectory)
    {
        logger?.LogInformation($"Start processing. Input: {input}    Output: {output}  @  {workingDirectory}");
        await PluginUtility.ExecuteAsync(logger, exePath, CreateArgument(input, output), workingDirectory).ConfigureAwait(false);

        if (input.EndsWith(".jpg"))
        {
            logger?.LogInformation($"Delete jpeg file: {input}");
            File.Delete(input);
        }
    }
}
