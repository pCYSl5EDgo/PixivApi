using Cysharp.Collections;
using System.Collections.Immutable;
using System.Text.Encodings.Web;
using System.Text.Unicode;

namespace PixivApi.Core;

public static class IOUtility
{
    static IOUtility()
    {
        messagePackSerializerOptions = MessagePackSerializerOptions.Standard;
        var byteTexts = new string[256];
        for (var i = 0; i < byteTexts.Length; i++)
        {
            byteTexts[i] = string.Intern(i.ToString("X2"));
        }

        ByteTexts = ImmutableArray.Create(byteTexts);
    }

    public static readonly ImmutableArray<string> ByteTexts;

    public static string GetHashPath(ulong id)
    {
        DefaultInterpolatedStringHandler handler = new();
        AppendHashPath(ref handler, id);
        return handler.ToStringAndClear();
    }

    public static void AppendHashPath(ref DefaultInterpolatedStringHandler handler, ulong id)
    {
        handler.AppendLiteral(ByteTexts[(int)(id & 255UL)]);
        handler.AppendLiteral("/");
        handler.AppendLiteral(ByteTexts[(int)((id >> 8) & 255UL)]);
        handler.AppendLiteral("/");
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
    public static readonly JsonSerializerOptions JsonSerializerOptionsWithIndent = new()
    {
        Encoder = javaScriptEncoder,
        IncludeFields = true,
        WriteIndented = true,
        Converters =
        {
            CamelCaseArtworkOrderKindConverter.Instance,
            new JsonStringEnumConverter(JsonNamingPolicy.CamelCase),
        },
    };

    public static readonly JsonSerializerOptions JsonSerializerOptionsNoIndent = new()
    {
        Encoder = javaScriptEncoder,
        IncludeFields = true,
        WriteIndented = false,
        Converters =
        {
            CamelCaseArtworkOrderKindConverter.Instance,
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

        return JsonSerializer.Deserialize<T>(ref reader, JsonSerializerOptionsWithIndent);
    }

    public static T? JsonDeserialize<T>(ReadOnlySequence<byte> sequence) where T : notnull
    {
        var reader = new Utf8JsonReader(sequence, new JsonReaderOptions
        {
            AllowTrailingCommas = true,
            CommentHandling = JsonCommentHandling.Skip,
        });

        return JsonSerializer.Deserialize<T>(ref reader, JsonSerializerOptionsWithIndent);
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

    public static async ValueTask JsonSerializeAsync<T>(string path, T value, FileMode mode, bool indentation = true)
    {
        using var stream = new FileStream(path, mode, FileAccess.Write, FileShare.Read, 8192, FileOptions.Asynchronous);
        await JsonSerializer.SerializeAsync(stream, value, indentation ? JsonSerializerOptionsWithIndent : JsonSerializerOptionsNoIndent, default).ConfigureAwait(false);
    }

    public static Utf8ValueStringBuilder JsonUtf8Serialize<T>(T value) where T : ITransformAppend
    {
        var builder = ZString.CreateUtf8StringBuilder();
        value.TransformAppend(ref builder);
        return builder;
    }

    public static string JsonStringSerialize<T>(T value, bool indentation = true) => JsonSerializer.Serialize(value, indentation ? JsonSerializerOptionsWithIndent : JsonSerializerOptionsNoIndent);

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
