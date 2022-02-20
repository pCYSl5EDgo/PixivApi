using System.Security.Cryptography;
using System.Text;
using OpenQA.Selenium.Chrome;
using WebDriverManager;
using WebDriverManager.DriverConfigs.Impl;

namespace PixivApi;

public static partial class AccessTokenUtility
{
    public static async ValueTask<string?> AuthAsync(HttpClient client, ConfigSettings config, CancellationToken token)
    {
        var (verifier, code) = await FirstAuthAsync(token).ConfigureAwait(false);
        if (string.IsNullOrWhiteSpace(code))
        {
            return null;
        }

        using var request = new HttpRequestMessage(HttpMethod.Post, "https://oauth.secure.pixiv.net/auth/token");
        request.Headers.TryAddWithoutValidation("user-agent", config.UserAgent);
        request.Headers.TryAddWithoutValidation("app-os-version", config.AppOSVersion);
        request.Headers.TryAddWithoutValidation("app-os", config.AppOS);

        var data = new OAuthRefreshTokenPostRequestData(config.ClientId, config.ClientSecret, code, verifier);
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

        return GetToken(json, RefreshToken());
    }

    private static string? ProcessLog(ChromeDriver driver)
    {
        string? code = null;
        var logs = driver.Manage().Logs.GetLog("performance");
        foreach (var log in logs)
        {
            var message = log.Message;
            try
            {
                var logJson = JsonSerializer.Deserialize<ChromeLogJson>(message);
                const string head = "pixiv://";
                if (logJson is not { Message: { Method: "Network.requestWillBeSent", Params.DocumentUrl: string documentUrl } } || !documentUrl.StartsWith(head))
                {
                    continue;
                }

                code = GetCode(documentUrl)!;
                if (code is not null)
                {
                    break;
                }
            }
            catch
            {
            }
        }

        driver.Close();
        return code;
    }

    private static (ChromeDriver Driver, string Verifier) SetUpChromeDriverAndNavigateToLoginPage()
    {
        new DriverManager().SetUpDriver(new ChromeConfig());
        var chromeOptions = new ChromeOptions();
        chromeOptions.SetLoggingPreference("performance", OpenQA.Selenium.LogLevel.All);

        var driver = new ChromeDriver(chromeOptions);
        var (verifier, challenge) = GeneratePkce();

        var url = $"https://app-api.pixiv.net/web/v1/login?code_challenge={challenge}&code_challenge_method=S256&client=pixiv-android";
        driver.Navigate().GoToUrl(url);
        return (driver, verifier);
    }

    private static async ValueTask<(string CodeVerfier, string? Code)> FirstAuthAsync(CancellationToken token)
    {
        var (driver, verifier) = SetUpChromeDriverAndNavigateToLoginPage();
        var wait = TimeSpan.FromSeconds(1);
        do
        {
            await Task.Delay(wait, token).ConfigureAwait(false);
        } while (!driver.Url.StartsWith("https://accounts.pixiv.net/post-redirect"));
        return (verifier, ProcessLog(driver));
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

    private static (string CodeVerifier, string CodeChallenge) GeneratePkce()
    {
        using var segment = ArraySegmentFromPool.Rent(32);
        var verifier = GenerateRandomDataBase64url(segment.AsSpan());
        var challenge = Base64UrlEncodeNoPadding(Sha256Ascii(verifier));
        return (verifier, challenge);
    }

    [StringLiteral.Utf8(@"""access_token"":")]
    private static partial ReadOnlySpan<byte> AccessToken();

    [StringLiteral.Utf8(@"""refresh_token"":")]
    private static partial ReadOnlySpan<byte> RefreshToken();

    public static async ValueTask<string?> GetAccessTokenAsync(HttpClient client, ConfigSettings config, CancellationToken token)
    {
        token.ThrowIfCancellationRequested();
        using var request = new HttpRequestMessage(HttpMethod.Post, "https://oauth.secure.pixiv.net/auth/token");
        if (!request.TryAddToHeader(config.HashSecret, "oauth.secure.pixiv.net"))
        {
            return null;
        }

        var data = new AccessTokenPostRequestData(config.ClientId, config.ClientSecret, config.RefreshToken);
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

        return GetToken(json, AccessToken());
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
