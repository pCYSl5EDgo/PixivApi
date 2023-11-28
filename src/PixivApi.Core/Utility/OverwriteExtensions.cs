namespace PixivApi.Core;

public static class OverwriteExtensions
{
  public static void Overwrite<T>([NotNullIfNotNull("value"), NotNullIfNotNull("source")] ref T? destination, T? source) where T : class
  {
    if (source is null)
    {
      return;
    }

    destination = source;
  }
}
