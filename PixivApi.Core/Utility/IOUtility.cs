using Cysharp.Collections;
using System.Text.Encodings.Web;
using System.Text.Unicode;

namespace PixivApi;

public static class IOUtility
{
    static IOUtility()
    {
        messagePackSerializerOptions = MessagePackSerializerOptions.Standard;
    }

    public static string? GetFileNameFromUri(string uri)
    {
        if (!Uri.TryCreate(uri, UriKind.Absolute, out var uriObject))
        {
            uriObject = new Uri("https://pixiv.net");
        }

        return Path.GetFileName(uriObject.LocalPath);
    }

    public static async ValueTask<NativeMemoryArray<byte>> ReadFromFileAsync(string path, CancellationToken token)
    {
        token.ThrowIfCancellationRequested();
        using var handle = File.OpenHandle(path, FileMode.Open, FileAccess.Read, FileShare.Read, FileOptions.Asynchronous);
        var length = RandomAccess.GetLength(handle);
        var array = new NativeMemoryArray<byte>(length, false, false);
        try
        {
            long actual;
            if (length <= Array.MaxLength)
            {
                actual = await RandomAccess.ReadAsync(handle, array.AsMemory(), 0, token).ConfigureAwait(false);
            }
            else
            {
                actual = await RandomAccess.ReadAsync(handle, array.AsMemoryList(), 0, token).ConfigureAwait(false);
            }

            if (length != actual)
            {
                throw new InvalidDataException();
            }

            return array;
        }
        catch
        {
            array.Dispose();
            throw;
        }
    }

    public static async ValueTask WriteToFileAsync(string path, ReadOnlyMemory<byte> memory, CancellationToken token)
    {
        token.ThrowIfCancellationRequested();
        using var handle = File.OpenHandle(path, FileMode.Create, FileAccess.Write, FileShare.Read, FileOptions.Asynchronous);
        await RandomAccess.WriteAsync(handle, memory, 0, token).ConfigureAwait(false);
    }

    private static readonly JavaScriptEncoder javaScriptEncoder = JavaScriptEncoder.Create(UnicodeRanges.All);
    private static readonly JsonSerializerOptions jsonSerializerOptions = new()
    {
        Encoder = javaScriptEncoder,
        IncludeFields = true,
        WriteIndented = true,
        Converters =
        {
            new JsonStringEnumConverter(JsonNamingPolicy.CamelCase),
        },
    };

    public static T? JsonDeserialize<T>(ReadOnlySpan<byte> span) where T : notnull
    {
        var reader = new Utf8JsonReader(span, new JsonReaderOptions
        {
            AllowTrailingCommas = true,
            CommentHandling = JsonCommentHandling.Skip,
        });

        return JsonSerializer.Deserialize<T>(ref reader, jsonSerializerOptions);
    }

    public static T? JsonDeserialize<T>(ReadOnlySequence<byte> sequence) where T : notnull
    {
        var reader = new Utf8JsonReader(sequence, new JsonReaderOptions
        {
            AllowTrailingCommas = true,
            CommentHandling = JsonCommentHandling.Skip,
        });

        return JsonSerializer.Deserialize<T>(ref reader, jsonSerializerOptions);
    }

    public static async ValueTask<T?> JsonDeserializeAsync<T>(string path, CancellationToken token) where T : notnull
    {
        using var array = await ReadFromFileAsync(path, token).ConfigureAwait(false);

        static T? Deserialize(NativeMemoryArray<byte> array)
        {
            if (array.TryGetFullSpan(out var span))
            {
                return JsonDeserialize<T>(span);
            }
            else
            {
                return JsonDeserialize<T>(array.AsReadOnlySequence());
            }
        }

        return Deserialize(array);
    }

    public static async ValueTask JsonSerializeAsync<T>(string path, T value, FileMode mode)
    {
        using var stream = new FileStream(path, mode, FileAccess.Write, FileShare.Read, 8192, FileOptions.Asynchronous);
        await JsonSerializer.SerializeAsync(stream, value, jsonSerializerOptions, default).ConfigureAwait(false);
    }

    public static Utf8ValueStringBuilder JsonUtf8Serialize<T>(T value) where T : ITransformAppend
    {
        var builder = ZString.CreateUtf8StringBuilder();
        value.TransformAppend(ref builder);
        return builder;
    }

    public static string JsonStringSerialize<T>(T value) => JsonSerializer.Serialize(value, jsonSerializerOptions);

    private static readonly MessagePackSerializerOptions messagePackSerializerOptions;

    public static async ValueTask<T?> MessagePackDeserializeAsync<T>(string path, CancellationToken token) where T : notnull
    {
        try
        {
            using var segment = await ReadFromFileAsync(path, token).ConfigureAwait(false);
            if (segment.Length == 0)
            {
                return default;
            }
            else if (segment.Length <= Array.MaxLength)
            {
                return MessagePackSerializer.Deserialize<T>(segment.AsMemory(), messagePackSerializerOptions, token);
            }
            else
            {
                return MessagePackSerializer.Deserialize<T>(segment.AsReadOnlySequence(), messagePackSerializerOptions, token);
            }
        }
        catch (IOException)
        {
            return default;
        }
    }

    public static async ValueTask MessagePackSerializeAsync<T>(string path, T value, FileMode mode)
    {
        using var stream = new FileStream(path, mode, FileAccess.Write, FileShare.Read, 8192, true);
        await MessagePackSerializer.SerializeAsync(stream, value, null, CancellationToken.None).ConfigureAwait(false);
    }

    public static readonly HashSet<char> PathInvalidChars = new(Path.GetInvalidPathChars());
}
