namespace PixivApi.Console;

public static class ByteAmountUtility
{
  public static string ToDisplayable(ulong byteCount)
  {
    if (byteCount < (1 << 10))
    {
      return $"{byteCount} B";
    }
    else if (byteCount < (1 << 20))
    {
      return $"{byteCount >> 10} KB + {byteCount & 1023} B";
    }
    else if (byteCount < (1 << 30))
    {
      return $"{byteCount >> 20} MB + {(byteCount >> 10) & 1023} KB + {byteCount & 1023} B";
    }
    else
    {
      return $"{byteCount >> 30} GB + {(byteCount >> 20) & 1023} MB + {(byteCount >> 10) & 1023} KB + {byteCount & 1023} B";
    }
  }
}
