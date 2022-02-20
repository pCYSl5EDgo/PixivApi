using System.Security.Cryptography;

namespace PixivApi;

public static class HttpRequestMessageUtility
{
    public static bool TryAddToDefaultHeader(this HttpClient client, ConfigSettings config, [NotNullWhen(true)] string? accessToken)
    {
        if (string.IsNullOrEmpty(accessToken))
        {
            return false;
        }

        var headers = client.DefaultRequestHeaders;
        headers.Authorization = new("Bearer", accessToken);

        if (!headers.TryAddWithoutValidation("app-os", config.AppOS))
        {
            return false;
        }

        if (!headers.TryAddWithoutValidation("app-os-version", config.AppOSVersion))
        {
            return false;
        }

        if (!headers.TryAddWithoutValidation("user-agent", config.UserAgent))
        {
            return false;
        }

        return true;
    }

    public static bool TryAddToHeader(this HttpRequestMessage message, string hashSecret, string host)
    {
        var builder = ZString.CreateUtf8StringBuilder(true);
        try
        {
            builder.Append(DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss+00:00"));
            builder.Append(hashSecret);

            var binary = ArrayPool<byte>.Shared.Rent(16);
            try
            {
                var hashLength = MD5.HashData(builder.AsSpan(), binary.AsSpan());
                var headers = message.Headers;
                if (!headers.TryAddWithoutValidation("x-client-time", builder.ToString()))
                {
                    return false;
                }

                if (!headers.TryAddWithoutValidation("x-client-hash", string.Create(hashLength * 2, (binary, hashLength), CreateHashString)))
                {
                    return false;
                }

                if (!headers.TryAddWithoutValidation("host", host))
                {
                    return false;
                }
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(binary);
            }
        }
        finally
        {
            builder.Dispose();
        }

        return true;
    }

    private static void CreateHashString(Span<char> span, (byte[] Array, int Length) pair)
    {
        var source = pair.Array.AsSpan(0, pair.Length);
        foreach (var c in source)
        {
            if (!c.TryFormat(span, out var charsWritten, "x2"))
            {
                throw new InvalidOperationException();
            }

            span = span[charsWritten..];
        }
    }
}
