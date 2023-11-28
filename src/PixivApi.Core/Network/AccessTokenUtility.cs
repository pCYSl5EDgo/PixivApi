using OpenQA.Selenium.Chrome;
using System.Security.Cryptography;
using System.Text;

namespace PixivApi.Core.Network;

public static partial class AccessTokenUtility
{
  public static async ValueTask<string?> AuthAsync(HttpClient client, ConfigSettings config, CancellationToken token)
  {
    var (verifier, code) = await FirstAuthAsync(client, token).ConfigureAwait(false);
    if (string.IsNullOrWhiteSpace(code))
    {
      return null;
    }

    token.ThrowIfCancellationRequested();
    using var request = new HttpRequestMessage(HttpMethod.Post, "https://oauth.secure.pixiv.net/auth/token");
    request.Headers.TryAddWithoutValidation("user-agent", config.UserAgent);
    request.Headers.TryAddWithoutValidation("app-os-version", config.AppOSVersion);
    request.Headers.TryAddWithoutValidation("app-os", config.AppOS);

    var data = new OAuthRefreshTokenPostRequestData(config.ClientId, config.ClientSecret, code, verifier);
    using var buffer = IOUtility.JsonUtf8Serialize(data);
    request.Content = new ReadOnlyMemoryContent(buffer.AsMemory());
    request.Content.Headers.ContentType = new("application/x-www-form-urlencoded");

    token.ThrowIfCancellationRequested();
    using var responseMessage = await client.SendAsync(request, token).ConfigureAwait(false);
    if (!responseMessage.IsSuccessStatusCode)
    {
      return null;
    }

    token.ThrowIfCancellationRequested();
    var json = await responseMessage.Content.ReadAsByteArrayAsync(token).ConfigureAwait(false);
    if (json is null)
    {
      return null;
    }

    return GetToken(json, @"""refresh_token"":"u8);
  }

  private static string? ProcessLog(ChromeDriver driver)
  {
    string? code = null;
    var logs = driver.Manage().Logs.GetLog("performance");
    foreach (var log in logs)
    {
      var message = log.Message;
      var logJson = JsonSerializer.Deserialize<ChromeLogJson?>(message);
      if (logJson is not null)
      {
        code = logJson.Code;
        break;
      }
    }

    driver.Close();
    return code;
  }

  private static async ValueTask<(string CodeVerfier, string? Code)> FirstAuthAsync(HttpClient client, CancellationToken token)
  {
    var (driver, verifier) = await SetUpChromeDriverAndNavigateToLoginPage(client, token).ConfigureAwait(false);
    try
    {
      var wait = TimeSpan.FromSeconds(1);
      do
      {
        token.ThrowIfCancellationRequested();
        await Task.Delay(wait, token).ConfigureAwait(false);
      } while (!driver.Url.StartsWith("https://accounts.pixiv.net/post-redirect"));
      return (verifier, ProcessLog(driver));
    }
    finally
    {
      driver.Dispose();
    }
  }

  private static async ValueTask<(ChromeDriver Driver, string Verifier)> SetUpChromeDriverAndNavigateToLoginPage(HttpClient client, CancellationToken token)
  {
    token.ThrowIfCancellationRequested();
    Directory.CreateDirectory("ChromeDriver");
    await ChromeDriverManager.Installer.InstallLatestAsync(client, "ChromeDriver", true, token).ConfigureAwait(false);
    var chromeOptions = new ChromeOptions();
    chromeOptions.SetLoggingPreference("performance", OpenQA.Selenium.LogLevel.All);

    token.ThrowIfCancellationRequested();
    var driver = new ChromeDriver(chromeOptions);
    var (verifier, challenge) = GeneratePkce();

    token.ThrowIfCancellationRequested();
    var url = $"https://app-api.pixiv.net/web/v1/login?code_challenge={challenge}&code_challenge_method=S256&client=pixiv-android";
    driver.Navigate().GoToUrl(url);
    return (driver, verifier);
  }

  private static (string CodeVerifier, string CodeChallenge) GeneratePkce()
  {
    Span<byte> span = stackalloc byte[32];
    var verifier = GenerateRandomDataBase64url(span);
    var challenge = Base64UrlEncodeNoPadding(Sha256Ascii(verifier));
    return (verifier, challenge);
  }

  public static async ValueTask<string?> GetAccessTokenAsync(HttpClient client, ConfigSettings config, int index, CancellationToken token)
  {
    token.ThrowIfCancellationRequested();
    using var request = new HttpRequestMessage(HttpMethod.Post, "https://oauth.secure.pixiv.net/auth/token");
    if (!request.TryAddToHeader(config.HashSecret, "oauth.secure.pixiv.net"))
    {
      return null;
    }

    var data = new AccessTokenPostRequestData(config.ClientId, config.ClientSecret, config.RefreshTokens[index]);
    using var buffer = IOUtility.JsonUtf8Serialize(data);
    request.Content = new ReadOnlyMemoryContent(buffer.AsMemory());
    request.Content.Headers.ContentType = new("application/x-www-form-urlencoded");

    using var responseMessage = await client.SendAsync(request, token).ConfigureAwait(false);
    if (!responseMessage.IsSuccessStatusCode)
    {
      return null;
    }

    var json = await responseMessage.Content.ReadAsByteArrayAsync(token).ConfigureAwait(false);
    if (json is null)
    {
      return null;
    }

    return GetToken(json, @"""access_token"":"u8);
  }

  private static unsafe string? GetToken(ReadOnlySpan<byte> json, ReadOnlySpan<byte> slice)
  {
    var index = json.IndexOf(slice);
    if (index == -1)
    {
      return null;
    }

    json = json[(index + slice.Length)..];
    index = json.IndexOf((byte)'"');
    if (index == -1)
    {
      return null;
    }

    json = json[(index + 1)..];
    index = json.IndexOf((byte)'"');
    if (index <= 0)
    {
      return null;
    }

    json = json[..index];

    fixed (byte* ptr = json)
    {
      return string.Create(json.Length, (nint)ptr, (span, pointer) =>
      {
        for (var i = 0; i < span.Length; i++)
        {
          span[i] = (char)((byte*)pointer)[i];
        }
      });
    }
  }

  private static string GenerateRandomDataBase64url(Span<byte> span)
  {
    Random.Shared.NextBytes(span);
    return Base64UrlEncodeNoPadding(span);
  }

  private static byte[] Sha256Ascii(string text)
  {
    var bytes = Encoding.ASCII.GetBytes(text);
    using var sha256 = SHA256.Create();
    return sha256.ComputeHash(bytes);
  }

  private static string Base64UrlEncodeNoPadding(ReadOnlySpan<byte> span)
  {
    var base64 = Convert.ToBase64String(span);

    // Converts base64 to base64url.
    base64 = base64.Replace("+", "-");
    base64 = base64.Replace("/", "_");
    // Strips padding.
    base64 = base64.Replace("=", "");
    return base64;
  }
}

public partial class ChromeLogJson
{
  public readonly string Code;

  public ChromeLogJson(string code) => Code = code;

  public sealed class Converter : JsonConverter<ChromeLogJson?>
  {
    public static readonly Converter Instance = new();

    public override ChromeLogJson? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
      if (reader.TokenType != JsonTokenType.StartObject)
      {
        reader.Skip();
        return null;
      }

      var code = default(string);
      while (reader.Read())
      {
        var tokenType = reader.TokenType;
        if (tokenType == JsonTokenType.EndObject)
        {
          return string.IsNullOrWhiteSpace(code) ? null : new ChromeLogJson(code);
        }

        if (tokenType == JsonTokenType.Comment)
        {
          continue;
        }

        if (tokenType != JsonTokenType.PropertyName)
        {
          throw new JsonException();
        }

        if (!reader.ValueTextEquals("message"u8))
        {
          reader.Skip();
          reader.Skip();
          continue;
        }

        if (!reader.Read())
        {
          throw new JsonException();
        }

        code = ReadCode(ref reader);
      }

      throw new JsonException();
    }

    private static string? ReadCode(ref Utf8JsonReader reader)
    {
      if (reader.TokenType != JsonTokenType.StartObject)
      {
        reader.Skip();
        return null;
      }

      var isRequestWillBeSent = false;
      var documentUrl = default(string);
      while (reader.Read())
      {
        var tokenType = reader.TokenType;
        if (tokenType == JsonTokenType.EndObject)
        {
          return isRequestWillBeSent ? GetCode(documentUrl) : default;
        }

        if (tokenType == JsonTokenType.Comment)
        {
          continue;
        }

        if (tokenType != JsonTokenType.PropertyName)
        {
          throw new JsonException();
        }

        if (reader.ValueTextEquals("method"u8))
        {
          if (!reader.Read())
          {
            throw new JsonException();
          }

          isRequestWillBeSent = reader.ValueTextEquals("Network.requestWillBeSent"u8);
        }
        else if (reader.ValueTextEquals("params"u8))
        {
          if (!reader.Read())
          {
            throw new JsonException();
          }

          documentUrl = ReadDocumentUrl(ref reader);
        }
        else
        {
          reader.Skip();
          reader.Skip();
        }
      }

      throw new JsonException();
    }

    private static string? ReadDocumentUrl(ref Utf8JsonReader reader)
    {
      if (reader.TokenType != JsonTokenType.StartObject)
      {
        reader.Skip();
        return null;
      }

      var documentUrl = default(string);
      while (reader.Read())
      {
        var tokenType = reader.TokenType;
        if (tokenType == JsonTokenType.EndObject)
        {
          return documentUrl;
        }

        if (tokenType == JsonTokenType.Comment)
        {
          continue;
        }

        if (tokenType != JsonTokenType.PropertyName)
        {
          throw new JsonException();
        }

        if (!reader.ValueTextEquals("documentURL"u8))
        {
          reader.Skip();
          reader.Skip();
          continue;
        }

        if (!reader.Read())
        {
          throw new JsonException();
        }

        documentUrl = reader.GetString();
      }

      throw new JsonException();
    }

    private static string? GetCode(ReadOnlySpan<char> documentUrl)
    {
      const string codeEqual = "code=";
      var codeEqualIndex = documentUrl.IndexOf(codeEqual);
      if (codeEqualIndex == -1)
      {
        return null;
      }

      var codeSpan = documentUrl[(codeEqualIndex + codeEqual.Length)..];
      var ampersandIndex = codeSpan.IndexOf('&');
      if (ampersandIndex != -1)
      {
        codeSpan = codeSpan[..ampersandIndex];
      }

      if (codeSpan.IsEmpty)
      {
        return null;
      }

      return new(codeSpan);
    }

    public override void Write(Utf8JsonWriter writer, ChromeLogJson? value, JsonSerializerOptions options) => throw new NotSupportedException();
  }
}
