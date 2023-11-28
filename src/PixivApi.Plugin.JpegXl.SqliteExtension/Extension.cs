using PixivApi.Core;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;

public static unsafe class Extension
{
  private static sqlite3_api_routines* ApiRoutines;
  private const int SQLITE_DETERMINISTIC = 0x000000800;
  private const int SQLITE_DIRECTONLY = 0x000080000;

  private static byte* ToBytePointer(this ReadOnlySpan<byte> span) => (byte*)Unsafe.AsPointer(ref MemoryMarshal.GetReference(span));
  private static readonly Encoding Encoding = Encoding.ASCII;

  private static void WriteError(sqlite3_context* context, ReadOnlySpan<char> text)
  {
    byte* output = stackalloc byte[text.Length];
    Encoding.GetBytes((char*)Unsafe.AsPointer(ref MemoryMarshal.GetReference(text)), text.Length, output, text.Length);
    ApiRoutines->result_error(context, output, text.Length);
  }

  private static void WriteError(sqlite3_context* context, ReadOnlySpan<byte> text) => ApiRoutines->result_error(context, (byte*)Unsafe.AsPointer(ref MemoryMarshal.GetReference(text)), text.Length);

  [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)], EntryPoint = "sqlite3_extension_init")]
  public static int Init(sqlite3* database, byte** errorMessage, sqlite3_api_routines* api)
  {
    ApiRoutines = api;

    int answer;
    answer = api->create_function_v2(database, "exists_all_original"u8.ToBytePointer(), 3, SQLITE_DETERMINISTIC | SQLITE_DIRECTONLY, null, &ExistsAllOriginal, null, null, null);
    answer = api->create_function_v2(database, "exists_min_original"u8.ToBytePointer(), 4, SQLITE_DETERMINISTIC | SQLITE_DIRECTONLY, null, &ExistsMinOriginal, null, null, null);
    answer = api->create_function_v2(database, "exists_max_original"u8.ToBytePointer(), 4, SQLITE_DETERMINISTIC | SQLITE_DIRECTONLY, null, &ExistsMaxOriginal, null, null, null);
    answer = api->create_function_v2(database, "exists_ugoira_zip"u8.ToBytePointer(), 2, SQLITE_DETERMINISTIC | SQLITE_DIRECTONLY, null, &ExistsUgoiraZip, null, null, null);
    if (answer != 0)
    {
      return answer;
    }

    return 0;
  }

  private const string OriginalFolder = "Original";

  [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
  private static void ExistsAllOriginal(sqlite3_context* context, int argLen, sqlite3_value** args)
  {
    if (argLen != 3)
    {
      WriteError(context, $"Invalid Argument Error. Length: {argLen}");
      return;
    }

    if (args == null)
    {
      WriteError(context, $"Null Argument Error");
      return;
    }

    var id = (ulong)ApiRoutines->value_int64(args[0]);
    var type = (ArtworkType)(byte)ApiRoutines->value_int(args[1]);
    var pageCount = ApiRoutines->value_int(args[2]);
    ExistsAll(OriginalFolder, context, id, type, pageCount);
  }

  private static void ExistsAll(string folder, sqlite3_context* context, ulong id, ArtworkType type, int pageCount)
  {
    var lower = (byte)(id & 0xffUL);
    var higher = (byte)((id >>> 8) & 0xffUL);
    switch (type)
    {
      case ArtworkType.Illust:
      case ArtworkType.Manga:
        var basePath = $"{folder}/{lower:X2}/{higher:X2}/{id}_";
        for (var i = 0; i < pageCount; i++)
        {
          var path = $"{basePath}{i}.jxl";
          if (!File.Exists(path))
          {
            ApiRoutines->result_int(context, 0);
            return;
          }
        }

        ApiRoutines->result_int(context, 1);
        return;
      case ArtworkType.Ugoira:
        var ugoiraFilePath = $"{folder}/{lower:X2}/{higher:X2}/{id}.jxl";
        ApiRoutines->result_int(context, File.Exists(ugoiraFilePath) ? 1 : 0);
        return;
      default:
        WriteError(context, "Invalid ArtworkType");
        return;
    }
  }

  [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
  private static void ExistsMaxOriginal(sqlite3_context* context, int argLen, sqlite3_value** args)
  {
    if (argLen != 4)
    {
      WriteError(context, $"Invalid Argument Error. Length: {argLen}");
      return;
    }

    if (args == null)
    {
      WriteError(context, $"Null Argument Error");
      return;
    }

    var pageCount = ApiRoutines->value_int(args[2]);
    var maxCount = ApiRoutines->value_int(args[3]);
    if (maxCount >= pageCount)
    {
      ApiRoutines->result_int(context, 1);
      return;
    }

    if (maxCount < 0)
    {
      maxCount += pageCount;
    }

    var id = (ulong)ApiRoutines->value_int64(args[0]);
    var type = (ArtworkType)(byte)ApiRoutines->value_int(args[1]);
    ExistsMax(OriginalFolder, context, id, type, pageCount, maxCount);
  }

  private static void ExistsMax(string folder, sqlite3_context* context, ulong id, ArtworkType type, int pageCount, int maxCount)
  {
    var lower = (byte)(id & 0xffUL);
    var higher = (byte)((id >>> 8) & 0xffUL);
    var count = 0;
    switch (type)
    {
      case ArtworkType.Illust:
      case ArtworkType.Manga:
        var basePath = $"{folder}/{lower:X2}/{higher:X2}/{id}_";
        for (var i = 0; i < pageCount; i++)
        {
          var path = $"{basePath}{i}.jxl";
          if (File.Exists(path))
          {
            if (++count > maxCount)
            {
              ApiRoutines->result_int(context, 0);
              return;
            }
          }
        }

        ApiRoutines->result_int(context, 1);
        return;
      case ArtworkType.Ugoira:
        var ugoiraFilePath = $"{folder}/{lower:X2}/{higher:X2}/{id}.jxl";
        ApiRoutines->result_int(context, maxCount != 0 || !File.Exists(ugoiraFilePath) ? 1 : 0);
        return;
      default:
        WriteError(context, "Invalid ArtworkType");
        return;
    }
  }

  [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
  private static void ExistsMinOriginal(sqlite3_context* context, int argLen, sqlite3_value** args)
  {
    if (argLen != 4)
    {
      WriteError(context, $"Invalid Argument Error. Length: {argLen}");
      return;
    }

    if (args == null)
    {
      WriteError(context, $"Null Argument Error");
      return;
    }

    var pageCount = ApiRoutines->value_int(args[2]);
    var minCount = ApiRoutines->value_int(args[3]);
    if (minCount > pageCount)
    {
      ApiRoutines->result_int(context, 0);
      return;
    }
    else if (minCount <= 0)
    {
      ApiRoutines->result_int(context, 1);
      return;
    }

    var id = (ulong)ApiRoutines->value_int64(args[0]);
    var type = (ArtworkType)(byte)ApiRoutines->value_int(args[1]);
    ExistsMin(OriginalFolder, context, id, type, pageCount, minCount);
  }

  private static void ExistsMin(string folder, sqlite3_context* context, ulong id, ArtworkType type, int pageCount, int minCount)
  {
    var lower = (byte)(id & 0xffUL);
    var higher = (byte)((id >>> 8) & 0xffUL);
    var count = 0;
    switch (type)
    {
      case ArtworkType.Illust:
      case ArtworkType.Manga:
        var basePath = $"{folder}/{lower:X2}/{higher:X2}/{id}_";
        for (var i = 0; i < pageCount; i++)
        {
          var path = $"{basePath}{i}.jxl";
          if (File.Exists(path))
          {
            if (++count == minCount)
            {
              ApiRoutines->result_int(context, 1);
              return;
            }
          }
        }

        ApiRoutines->result_int(context, 0);
        return;
      case ArtworkType.Ugoira:
        var ugoiraFilePath = $"{folder}/{lower:X2}/{higher:X2}/{id}.jxl";
        ApiRoutines->result_int(context, File.Exists(ugoiraFilePath) ? 1 : 0);
        return;
      default:
        WriteError(context, "Invalid ArtworkType");
        return;
    }
  }

  [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
  private static void ExistsUgoiraZip(sqlite3_context* context, int argLen, sqlite3_value** args)
  {
    if (argLen != 2)
    {
      WriteError(context, $"Invalid Argument Error. Length: {argLen}");
      return;
    }

    if (args == null)
    {
      WriteError(context, $"Null Argument Error");
      return;
    }

    var type = (ArtworkType)(byte)ApiRoutines->value_int(args[1]);
    if (type != ArtworkType.Ugoira)
    {
      ApiRoutines->result_int(context, 0);
      return;
    }

    var id = (ulong)ApiRoutines->value_int64(args[0]);
    var lower = (byte)(id & 0xffUL);
    var higher = (byte)((id >>> 8) & 0xffUL);
    var ugoiraFilePath = $"Ugoira/{lower:X2}/{higher:X2}/{id}_ugoira600x600.zip";
    ApiRoutines->result_int(context, File.Exists(ugoiraFilePath) ? 1 : 0);
    return;
  }
}
