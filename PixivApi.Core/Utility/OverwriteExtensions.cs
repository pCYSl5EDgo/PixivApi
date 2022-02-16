namespace PixivApi;

public static class OverwriteExtensions
{
    public static void Overwrite<T>([NotNullIfNotNull("value"), NotNullIfNotNull("source")] ref T? value, T? source) where T : class
    {
        if (source is null)
        {
            return;
        }

        if (value is IOverwrite<T> overwrite)
        {
            overwrite.Overwrite(source);
        }
        else
        {
            value = source;
        }
    }
}
