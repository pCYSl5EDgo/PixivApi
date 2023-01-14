using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;

namespace PixivApi.Plugin.JpegXl.SqliteExtension;

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

    [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvCdecl) }, EntryPoint = "sqlite3_extension_init")]
    public static int Init(sqlite3* database, byte** errorMessage, sqlite3_api_routines* api)
    {
        ApiRoutines = api;

        int answer;
        answer = api->create_function_v2(database, "exists_all_original_illusts"u8.ToBytePointer(), 3, SQLITE_DETERMINISTIC | SQLITE_DIRECTONLY, null, &ExistsAllOriginalIllusts, null, null, null);
        if (answer != 0)
        {
            return answer;
        }

        return 0;
    }

    [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvCdecl) })]
    private static void ExistsAllOriginalIllusts(sqlite3_context* context, int argLen, sqlite3_value** args)
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
        var lower = id & 0xffUL;
        var higher = (id >>> 8) & 0xffUL;
        var type = (byte)ApiRoutines->value_int(args[1]);
        var pageCount = ApiRoutines->value_int(args[2]);

        var basePath = $"Original/{higher:X2}/{lower:X2}/{id}_";
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
    }
}
