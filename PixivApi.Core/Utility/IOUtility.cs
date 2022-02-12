using System.Text.Encodings.Web;
using System.Text.Unicode;

namespace PixivApi;

public static class IOUtility
{
    public static string? GetFileNameFromUri(string uri)
    {
        if (!Uri.TryCreate(uri, UriKind.Absolute, out var uriObject))
        {
            uriObject = new Uri("https://pixiv.net");
        }

        return Path.GetFileName(uriObject.LocalPath);
    }

    public static async ValueTask<ArraySegmentFromPool> ReadFromFileAsync(string path, CancellationToken token)
    {
        token.ThrowIfCancellationRequested();
        using var handle = File.OpenHandle(path, FileMode.Open, FileAccess.Read, FileShare.Read, FileOptions.Asynchronous);
        var length = RandomAccess.GetLength(handle);
        var buffer = ArrayPool<byte>.Shared.Rent((int)length);
        var actual = await RandomAccess.ReadAsync(handle, buffer, 0, token).ConfigureAwait(false);
        return new(buffer, actual);
    }

    public static string? FindArtworkDatabase(string? path, bool returnNullWhenNotExist)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return null;
        }

        if (File.Exists(path))
        {
            return path;
        }

        if (!path.EndsWith(ArtworkDatabaseFileExtension))
        {
            path += ArtworkDatabaseFileExtension;
            if (File.Exists(path))
            {
                return path;
            }
        }

        return returnNullWhenNotExist ? null : path;
    }

    public static string? FindUserDatabase(string? path, bool returnNullWhenNotExist)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return null;
        }

        if (File.Exists(path))
        {
            return path;
        }

        if (!path.EndsWith(UserDatabaseFileExtension))
        {
            path += UserDatabaseFileExtension;
            if (File.Exists(path))
            {
                return path;
            }
        }

        return returnNullWhenNotExist ? null : path;
    }

    public const string ArtworkDatabaseFileExtension = ".arts";
    public const string UserDatabaseFileExtension = ".usrs";

    public const string UserIdDescription = "user id";

    public const string FilterDescription = "artwork filter *.json file or json expression";

    public const string ArtworkDatabaseDescription = $"artwork database *{ArtworkDatabaseFileExtension} file or directory path";
    public const string UserDatabaseDescription = $"user database *{UserDatabaseFileExtension} file or directory path";

    public const string OverwriteKindDescription = "add: Append new data to existing file.\nadd-search: Download everything and add to existing file.\nadd-clear: Delete the file and then download everything and write to the file.";

    public const string ErrorColor = "\u001b[31m";
    public const string WarningColor = "\u001b[33m";
    public const string SuccessColor = "\u001b[34m";
    public const string ReverseColor = "\u001b[7m";
    public const string NormalizeColor = "\u001b[0m";

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

    public static async ValueTask<T?> JsonDeserializeAsync<T>(string path, CancellationToken token) where T : notnull
    {
        using var segment = await ReadFromFileAsync(path, token).ConfigureAwait(false);
        return JsonDeserialize<T>(segment.AsReadOnlySpan());
    }

    public static async ValueTask<T?> JsonDeserializeAsync<T>(HttpContent content, CancellationToken token) where T : notnull
    {
        var array = await content.ReadAsByteArrayAsync(token).ConfigureAwait(false);
        return JsonDeserialize<T>(array);
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

    public static async ValueTask<T?> JsonParseAsync<T>(string? value, CancellationToken token) where T : notnull
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return default;
        }

        string? directory;
        T? obj;
        if (value[0] == '{')
        {
            obj = JsonSerializer.Deserialize<T>(value);
            directory = null;
        }
        else if (File.Exists(value))
        {
            obj = await JsonDeserializeAsync<T>(value, token).ConfigureAwait(false);
            directory = Path.GetDirectoryName(value);
        }
        else
        {
            value += ".json";
            if (!File.Exists(value))
            {
                return default;
            }

            obj = await JsonDeserializeAsync<T>(value, token).ConfigureAwait(false);
            directory = Path.GetDirectoryName(value);
        }

        if (obj is IAsyncInitailizable initailizable)
        {
            await initailizable.InitializeAsync(directory, token).ConfigureAwait(false);
        }

        return obj;
    }

    private static readonly MessagePackSerializerOptions messagePackSerializerOptions = MessagePackSerializerOptions.Standard;

    public static async ValueTask<T?> MessagePackDeserializeAsync<T>(string path, CancellationToken token) where T : notnull
    {
        using var handle = File.OpenHandle(path, FileMode.Open, FileAccess.Read, FileShare.Read, FileOptions.Asynchronous);
        var length = RandomAccess.GetLength(handle);
        const long Size32K = 1L << 15;

        if (length <= Size32K)
        {
            using var segment = ArraySegmentFromPool.Rent((int)length);
            var actual = await RandomAccess.ReadAsync(handle, segment.AsMemory(), 0, token).ConfigureAwait(false);
            return MessagePackSerializer.Deserialize<T>(segment.AsReadOnlyMemory()[..actual], null, token);
        }
        else
        {
            using var sequence = new SequenceFromPool(length, 15);
            var actual = await RandomAccess.ReadAsync(handle, sequence, 0, token).ConfigureAwait(false);
            if (actual != length)
            {
                throw new InvalidDataException();
            }

            return MessagePackSerializer.Deserialize<T>(sequence.AsSequence(), messagePackSerializerOptions, token);
        }
    }

    public static async ValueTask MessagePackSerializeAsync<T>(string path, T value, FileMode mode)
    {
        using var stream = new FileStream(path, mode, FileAccess.Write, FileShare.Read, 8192, FileOptions.Asynchronous);
        await MessagePackSerializer.SerializeAsync(stream, value, messagePackSerializerOptions, default).ConfigureAwait(false);
    }

    public static readonly HashSet<char> PathInvalidChars = new(Path.GetInvalidPathChars());
}
