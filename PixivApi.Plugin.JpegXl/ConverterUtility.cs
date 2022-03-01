using PixivApi.Console;
using System.Runtime.CompilerServices;

namespace PixivApi.Plugin.JpegXl;

internal static class ConverterUtility
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

    public static async ValueTask<bool> ExecuteAsync(ILogger? logger, string exePath, string input, long inputSize, string output, string workingDirectory)
    {
        logger?.LogInformation($"Start processing. Input: {input} Size: {inputSize}  -  Output: {output}  @  {workingDirectory}");
        try
        {
            if (logger is null)
            {
                await PluginUtility.ExecuteAsync(exePath, CreateArgument(input, output), workingDirectory).ConfigureAwait(false);
            }
            else
            {
                await PluginUtility.ExecuteAsync(logger, exePath, CreateArgument(input, output), workingDirectory, VirtualCodes.BrightBlueColor, VirtualCodes.NormalizeColor, VirtualCodes.BrightYellowColor, VirtualCodes.NormalizeColor).ConfigureAwait(false);
            }
        }
        catch (ProcessErrorException e)
        {
            if (e.ExitCode == 3)
            {
                File.Delete(Path.Combine(workingDirectory, input));
                logger?.LogError(e, $"{VirtualCodes.BrightRedColor}Error. Delete Input: {input} @ {workingDirectory} {VirtualCodes.NormalizeColor}");
                return false;
            }
            else
            {
                logger?.LogError(e, $"{VirtualCodes.BrightRedColor}Error. Input: {input} @ {workingDirectory} {VirtualCodes.NormalizeColor}");
                throw;
            }
        }

        var outputSize = new FileInfo(Path.Combine(workingDirectory, output)).Length;
        logger?.LogInformation($"{VirtualCodes.BrightGreenColor}Success. Input: {input} Size: {ByteAmountUtility.ToDisplayable((ulong)inputSize)}  -  Output: {output} Size: {ByteAmountUtility.ToDisplayable((ulong)outputSize)} @ {workingDirectory}  Compression Ratio: {(uint)(100d * outputSize / inputSize),3}{VirtualCodes.NormalizeColor}");
        return true;
    }
}
