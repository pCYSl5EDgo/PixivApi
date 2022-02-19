namespace PixivApi.Console;

public static class ArgumentDescriptions
{
    public const string UserIdDescription = "user id";

    public const string FilterDescription = "artwork filter *.json file";

    public const string DatabaseDescription = "artwork database file path";

    public const string OverwriteKindDescription = "add: Append new data to existing file.\nadd-search: Download everything and add to existing file.\nadd-clear: Delete the file and then download everything and write to the file.";

    public const string ErrorColor = "\u001b[91m";
    public const string WarningColor = "\u001b[33m";
    public const string SuccessColor = "\u001b[94m";
    public const string ReverseColor = "\u001b[7m";
    public const string NormalizeColor = "\u001b[0m";
}
