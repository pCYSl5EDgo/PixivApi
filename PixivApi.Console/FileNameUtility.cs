namespace PixivApi.Console;

public static class FileNameUtility
{
    public static string GetPrefixedFilePath(ReadOnlySpan<char> path, ReadOnlySpan<char> prefix, ReadOnlySpan<char> suffix)
    {
        var dir = Path.GetDirectoryName(path);
        var name = Path.GetFileNameWithoutExtension(path);
        if (prefix.IsEmpty)
        {
            if (suffix.IsEmpty)
            {
                if (dir.IsEmpty)
                {
                    return $"{name}{IOUtility.ArtworkDatabaseFileExtension}";
                }
                else if (dir[^1] == Path.DirectorySeparatorChar || dir[^1] == Path.AltDirectorySeparatorChar)
                {
                    return $"{dir}{name}{IOUtility.ArtworkDatabaseFileExtension}";
                }
                else
                {
                    return $"{dir}{Path.DirectorySeparatorChar}{name}{IOUtility.ArtworkDatabaseFileExtension}";
                }
            }
            else
            {
                if (dir.IsEmpty)
                {
                    return $"{name}{suffix}{IOUtility.ArtworkDatabaseFileExtension}";
                }
                else if (dir[^1] == Path.DirectorySeparatorChar || dir[^1] == Path.AltDirectorySeparatorChar)
                {
                    return $"{dir}{name}{suffix}{IOUtility.ArtworkDatabaseFileExtension}";
                }
                else
                {
                    return $"{dir}{Path.DirectorySeparatorChar}{name}{suffix}{IOUtility.ArtworkDatabaseFileExtension}";
                }
            }
        }
        else
        {
            if (suffix.IsEmpty)
            {
                if (dir.IsEmpty)
                {
                    return $"{prefix}{name}{IOUtility.ArtworkDatabaseFileExtension}";
                }
                else if (dir[^1] == Path.DirectorySeparatorChar || dir[^1] == Path.AltDirectorySeparatorChar)
                {
                    return $"{prefix}{dir}{name}{IOUtility.ArtworkDatabaseFileExtension}";
                }
                else
                {
                    return $"{prefix}{dir}{Path.DirectorySeparatorChar}{name}{IOUtility.ArtworkDatabaseFileExtension}";
                }
            }
            else
            {
                if (dir.IsEmpty)
                {
                    return $"{prefix}{name}{suffix}{IOUtility.ArtworkDatabaseFileExtension}";
                }
                else if (dir[^1] == Path.DirectorySeparatorChar || dir[^1] == Path.AltDirectorySeparatorChar)
                {
                    return $"{prefix}{dir}{name}{suffix}{IOUtility.ArtworkDatabaseFileExtension}";
                }
                else
                {
                    return $"{prefix}{dir}{Path.DirectorySeparatorChar}{name}{suffix}{IOUtility.ArtworkDatabaseFileExtension}";
                }
            }
        }
    }
}
