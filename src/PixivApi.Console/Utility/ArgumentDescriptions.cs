namespace PixivApi.Console;

public static class ArgumentDescriptions
{
  public const string FilterDescription = "artwork filter *.json file";

  public const string DatabaseDescription = "artwork database file path";

  public const string AddKindDescription = "false: Append new data to existing file.\ntrue: Download everything and add to existing file.";

  public const string RankingDescription = "ranking type\nday, week, month, day_male, day_female, week_original, week_rookie, day_manga, day_r18, day_male_r18, day_female_r18, week_r18, week_r18g";
}
