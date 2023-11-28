using System.IO.Compression;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace PixivApi.ChromeDriverManager;

public static class Installer
{
  private const string Url = "https://chromedriver.storage.googleapis.com/";
  private const string LatestVersionUrl = $"{Url}LATEST_RELEASE";
  private static readonly string ExeName = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? "chromedriver.exe" : "chromedriver";

  public static async ValueTask<Info> InstallLatestAsync(HttpClient httpClient, string destinationDirectoryName, bool overwriteFiles, CancellationToken cancellationToken)
  {
    var versionText = await httpClient.GetStringAsync(LatestVersionUrl, cancellationToken).ConfigureAwait(false);
    return await InstallAsync(httpClient, destinationDirectoryName, overwriteFiles, versionText, cancellationToken).ConfigureAwait(false);
  }

  public static async ValueTask<Info> InstallAsync(HttpClient httpClient, string destinationDirectoryName, bool overwriteFiles, string version, CancellationToken cancellationToken)
  {
    var url = CalcUrl(version);
    using var zipFileStream = await httpClient.GetStreamAsync(url, cancellationToken).ConfigureAwait(false);
    Extract(zipFileStream, destinationDirectoryName, overwriteFiles);
    return new(version, Path.Combine(destinationDirectoryName, ExeName));
  }

  private static void Extract(Stream zipFileStream, string destinationDirectoryName, bool overwriteFiles)
  {
    using var zipArchive = new ZipArchive(zipFileStream, ZipArchiveMode.Read, false);
    zipArchive.ExtractToDirectory(destinationDirectoryName, overwriteFiles);
  }

  private static string CalcUrl(string version)
  {
    DefaultInterpolatedStringHandler handler = $"{Url}{version}";
    AddFileName(ref handler);
    return handler.ToStringAndClear();
  }

  private static void AddFileName(ref DefaultInterpolatedStringHandler handler)
  {
    if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
    {
      handler.AppendLiteral("/chromedriver_win32.zip");
    }
    else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
    {
      if (RuntimeInformation.OSArchitecture == Architecture.Arm64)
      {
        handler.AppendLiteral("/chromedriver_mac64_m1.zip");
      }
      else
      {
        handler.AppendLiteral("/chromedriver_mac64.zip");
      }
    }
    else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
    {
      handler.AppendLiteral("/chromedriver_linux64.zip");
    }
    else
    {
      throw new NotSupportedException();
    }
  }
}

public record struct Info(string Version, string ExecutablePath)
{
}
