using PixivApi.Console;
using System.Runtime.CompilerServices;

namespace PixivApi.Plugin.JpegXl;

internal static class ConverterUtility
{
    private static void CreateArgument(ref DefaultInterpolatedStringHandler handler, string input, string output)
    {
        handler.AppendLiteral("\"");
        AppendChangeToSlash(ref handler, input.AsSpan());
        handler.AppendLiteral("\" \"");
        AppendChangeToSlash(ref handler, output.AsSpan());
        handler.AppendLiteral("\" --effort=7 --num_threads=");
        handler.AppendFormatted(Environment.ProcessorCount);
    }

    private static string CreateArgument(string input, string output)
    {
        DefaultInterpolatedStringHandler handler = new();
        CreateArgument(ref handler, input, output);
        return handler.ToStringAndClear();
    }

    private static string CreateArgumentLosslessJpeg(string input, string output)
    {
        DefaultInterpolatedStringHandler handler = new();
        CreateArgument(ref handler, input, output);
        handler.AppendLiteral(" -j");
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

    public static string GetJxlName(ulong id, uint? index) => index.HasValue ? $"{id}_{index.Value}.jxl" : $"{id}.jxl";

    public static async ValueTask<bool> ExecuteAsync(ILogger? logger, string exePath, string input, long inputSize, string output, string workingDirectory, bool deleteWhenFailure)
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
            if (e.ExitCode != 3)
            {
                logger?.LogError(e, $"{VirtualCodes.BrightRedColor}Error. Input: {input} @ {workingDirectory} {VirtualCodes.NormalizeColor}");
                return false;
            }

            try
            {
                if (logger is null)
                {
                    await PluginUtility.ExecuteAsync(exePath, CreateArgumentLosslessJpeg(input, output), workingDirectory).ConfigureAwait(false);
                }
                else
                {
                    await PluginUtility.ExecuteAsync(logger, exePath, CreateArgumentLosslessJpeg(input, output), workingDirectory, VirtualCodes.BrightBlueColor, VirtualCodes.NormalizeColor, VirtualCodes.BrightYellowColor, VirtualCodes.NormalizeColor).ConfigureAwait(false);
                }
            }
            catch (ProcessErrorException e2)
            {
                if (deleteWhenFailure && e2.ExitCode == 3)
                {
                    File.Delete(Path.Combine(workingDirectory, input));
                    logger?.LogError(e, $"{VirtualCodes.BrightRedColor}Error. Delete Input: {input} @ {workingDirectory} {VirtualCodes.NormalizeColor}");
                }
                else
                {
                    logger?.LogError(e, $"{VirtualCodes.BrightRedColor}Error. Input: {input} @ {workingDirectory} {VirtualCodes.NormalizeColor}");
                }

                return false;
            }
        }

        var outputSize = new FileInfo(Path.Combine(workingDirectory, output)).Length;
        logger?.LogInformation($"{VirtualCodes.BrightGreenColor}Success. Input: {input} Size: {ByteAmountUtility.ToDisplayable((ulong)inputSize)}  -  Output: {output} Size: {ByteAmountUtility.ToDisplayable((ulong)outputSize)} @ {workingDirectory}  Compression Ratio: {(uint)(100d * outputSize / inputSize),3}{VirtualCodes.NormalizeColor}");
        return true;
    }

    public static async Task<SpecificConfigSettings> CreateSpecificConfigSettinsAsync(string dllPath, CancellationToken cancellationToken)
    {
        var filterPath = Directory.EnumerateFiles(Path.GetDirectoryName(dllPath) ?? string.Empty, "filter.json*").SingleOrDefault();
        var specificConfigSettings = string.IsNullOrWhiteSpace(filterPath) ? new() : await IOUtility.JsonDeserializeAsync<SpecificConfigSettings>(filterPath, cancellationToken).ConfigureAwait(false) ?? new();
        return specificConfigSettings;
    }
}
