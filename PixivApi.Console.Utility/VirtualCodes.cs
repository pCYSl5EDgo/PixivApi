namespace PixivApi.Console;

public static class VirtualCodes
{
    public const string ESC = "\u001b";

    public const string BrightRedColor = $"{ESC}[91m";
    public const string BrightGreenColor = $"{ESC}[92m";
    public const string BrightYellowColor = $"{ESC}[33m";
    public const string BrightBlueColor = $"{ESC}[94m";
    public const string ReverseColor = $"{ESC}[7m";
    public const string NormalizeColor = $"{ESC}[0m";

    public const string DeleteLine1 = $"{ESC}[1M";

    public const string UseAltDisplayBuffer = $"{ESC}[?1049h";
    public const string UseMainDisplayBuffer = $"{ESC}[?1049l";
}
