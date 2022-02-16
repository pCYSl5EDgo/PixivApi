namespace PixivApi.Console;

[Command("local")]
public sealed partial class LocalClient : ConsoleAppBase
{
    private readonly ILogger<LocalClient> logger;
    private readonly ConfigSettings configSettings;

    public LocalClient(ILogger<LocalClient> logger, ConfigSettings configSettings)
    {
        this.logger = logger;
        this.configSettings = configSettings;
    }
}
