namespace PixivApi.Core;

public static class PluginFindExecutableUtility
{
    public static string? Find(string dllPath, string exeBasicName)
    {
        var dllDirectory = Path.GetDirectoryName(dllPath);
        var exeName = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? $"{exeBasicName}.exe" : exeBasicName;
        var exePath = string.IsNullOrWhiteSpace(dllDirectory) ? exeName : Path.Combine(dllDirectory, exeName);
        if (File.Exists(exePath))
        {
            return exePath;
        }

        exePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), exeName);
        if (File.Exists(exePath))
        {
            return exePath;
        }

        exePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), exeName);
        if (File.Exists(exePath))
        {
            return exePath;
        }

        return null;
    }
}
