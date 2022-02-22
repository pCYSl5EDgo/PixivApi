namespace PixivApi.Console;

public static class ConsoleUtility
{
    public const string ESC = "\u001b";

    public const string ErrorColor = $"{ESC}[91m";
    public const string WarningColor = $"{ESC}[33m";
    public const string SuccessColor = $"{ESC}[94m";
    public const string ReverseColor = $"{ESC}[7m";
    public const string NormalizeColor = $"{ESC}[0m";
}
