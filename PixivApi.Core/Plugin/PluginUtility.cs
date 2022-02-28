using System.Reflection;

namespace PixivApi.Core;

public static class PluginUtility
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

    public static Task<IPlugin?> LoadPluginAsync(string? pluginText, ConfigSettings configSettings, object boxedCancellationToken)
    {
        var token = Unsafe.Unbox<CancellationToken>(boxedCancellationToken);
        if (token.IsCancellationRequested)
        {
            goto CANCEL;
        }

        var plugin = pluginText.AsSpan().Trim();
        var index = plugin.IndexOf('|');
        if (index <= 0 || index == plugin.Length - 1)
        {
            goto NULL;
        }

        var dllPath = ToStringFromSpan(plugin[..index].TrimEnd());
        if (dllPath.Length == 0)
        {
            goto NULL;
        }

        if (!File.Exists(dllPath))
        {
            goto NULL;
        }

        if (token.IsCancellationRequested)
        {
            goto CANCEL;
        }

        Assembly assembly;
        try
        {
            assembly = Assembly.LoadFrom(dllPath);
        }
        catch
        {
            goto NULL;
        }

        if (token.IsCancellationRequested)
        {
            goto CANCEL;
        }

        var typeName = ToStringFromSpan(plugin[(index + 1)..].TrimStart());
        Type? type = null;
        if (string.IsNullOrWhiteSpace(typeName))
        {
            foreach (var item in assembly.ExportedTypes)
            {
                if (!typeof(IFinder).IsAssignableFrom(item))
                {
                    continue;
                }
                
                if (type is not null)
                {
                    goto NULL;
                }

                type = item;
            }
        }
        else
        {
            foreach (var item in assembly.ExportedTypes)
            {
                if (!item.FullName.AsSpan().SequenceEqual(typeName.AsSpan()))
                {
                    continue;
                }

                if (type is not null)
                {
                    goto NULL;
                }

                type = item;
            }
        }

        if (type?.GetMethod(nameof(IPlugin.CreateAsync))?.Invoke(null, new object[] { dllPath, configSettings, boxedCancellationToken }) is Task<IPlugin?> task)
        {
            return task;
        }

    NULL:
        return Task.FromResult<IPlugin?>(null);

    CANCEL:
        return Task.FromCanceled<IPlugin?>(token);
    }


    private static string ToStringFromSpan(ReadOnlySpan<char> span)
    {
        if (span.IsEmpty)
        {
            return string.Empty;
        }

        return new(span);
    }
}
