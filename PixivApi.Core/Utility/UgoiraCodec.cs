namespace PixivApi.Core;

public enum UgoiraCodec
{
    av1,
    h264
}

public static class UgoiraCodecExtensions
{
#pragma warning disable IDE0060
    public static string GetExtension(this UgoiraCodec ugoiraCodec) => ".mp4";
#pragma warning restore IDE0060

    public static string GetCodecTextForFfmpeg(this UgoiraCodec ugoiraCodec) => ugoiraCodec switch
    {
        UgoiraCodec.av1 => "libaom-av1",
        UgoiraCodec.h264 => "libx264",
        _ => throw new InvalidDataException(),
    };

    public static bool TryParse(string value, out UgoiraCodec codec)
    {
        switch (value)
        {
            case "av1":
                codec = UgoiraCodec.av1;
                return true;
            case "h264":
                codec = UgoiraCodec.h264;
                return true;
            default:
                Unsafe.SkipInit(out codec);
                return false;
        }
    }
}
