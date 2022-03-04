namespace PixivApi.Plugin.JpegXl;

public sealed record class OriginalConverter(string ExePath, ConfigSettings ConfigSettings) : IConverter
{
    public static Task<IPlugin?> CreateAsync(string dllPath, ConfigSettings configSettings, CancellationToken cancellationToken)
    {
        if (cancellationToken.IsCancellationRequested)
        {
            return Task.FromCanceled<IPlugin?>(cancellationToken);
        }

        var exePath = PluginUtility.Find(dllPath, "cjxl");
        return Task.FromResult<IPlugin?>(exePath is null ? null : new OriginalConverter(exePath, configSettings));
    }

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;

    private static bool TryParse(ReadOnlySpan<char> fileNameWithoutExtension, out ulong id, out uint? index)
    {
        index = null;
        if (fileNameWithoutExtension.EndsWith("_ugoira0"))
        {
            return ulong.TryParse(fileNameWithoutExtension[0..^"_ugoira0".Length], out id);
        }

        var pIndex = fileNameWithoutExtension.IndexOf("_p");
        if (pIndex <= 0)
        {
            id = 0;
            return false;
        }

        if (!ulong.TryParse(fileNameWithoutExtension[0..pIndex], out id))
        {
            return false;
        }

        if (!uint.TryParse(fileNameWithoutExtension[(pIndex + 2)..], out var indexValue))
        {
            return false;
        }

        index = indexValue;
        return true;
    }

    public async ValueTask<bool> TryConvertAsync(FileInfo file, ILogger? logger, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var name = file.Name;
        if (name.EndsWith(".jxl"))
        {
            return false;
        }

        if (!TryParse(Path.GetFileNameWithoutExtension(name.AsSpan()), out var id, out var index))
        {
            return false;
        }

        var workingDirectory = file.DirectoryName;
        var jxlName = ConverterUtility.GetJxlName(id, index);
        var jxlFile = new FileInfo(workingDirectory is null ? jxlName : Path.Combine(workingDirectory, jxlName));
        if (jxlFile.Exists)
        {
            return false;
        }

        return await ConverterUtility.ExecuteAsync(logger, ExePath, name, file.Length, jxlName, workingDirectory ?? string.Empty).ConfigureAwait(false);
    }
}
