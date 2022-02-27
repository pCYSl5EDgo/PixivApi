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
    };
}
