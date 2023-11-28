using System.Runtime.CompilerServices;

namespace PixivApi.Console;

public static class VirtualCodes
{
  public const string ESC = "\u001b";
  public const string TitleTeminator = "\u001b\u005c";

  public const string BrightRedColor = $"{ESC}[91m";
  public const string BrightGreenColor = $"{ESC}[92m";
  public const string BrightYellowColor = $"{ESC}[33m";
  public const string BrightBlueColor = $"{ESC}[94m";
  public const string ReverseColor = $"{ESC}[7m";
  public const string NormalizeColor = $"{ESC}[0m";

  public const string DeleteLine1 = $"{ESC}[1M";

  public const string UseAltDisplayBuffer = $"{ESC}[?1049h";
  public const string UseMainDisplayBuffer = $"{ESC}[?1049l";

  public const string SavePosition = $"{ESC}7";
  public const string LoadPosition = $"{ESC}8";

  public static void SetTitle(ref DefaultInterpolatedStringHandler handler, ReadOnlySpan<char> title)
  {
    handler.AppendLiteral($"{ESC}]0;");
    handler.AppendFormatted(title);
    handler.AppendLiteral(TitleTeminator);
  }

  public static string SetTitle(ReadOnlySpan<char> title)
  {
    var handler = new DefaultInterpolatedStringHandler();
    SetTitle(ref handler, title);
    return handler.ToStringAndClear();
  }
}
