namespace PixivApi.Console;

[Command("local")]
public sealed partial class LocalClient : ConsoleAppBase
{
    private readonly ILogger<LocalClient> logger;
    private readonly ConfigSettings configSettings;
    private readonly IDatabaseFactory databaseFactory;
    private readonly IArtworkFilterFactory<FileInfo> filterFactory;

    public LocalClient(ILogger<LocalClient> logger, ConfigSettings configSettings, IDatabaseFactory databaseFactory, IArtworkFilterFactory<FileInfo> filterFactory)
    {
        this.logger = logger;
        this.configSettings = configSettings;
        this.databaseFactory = databaseFactory;
        this.filterFactory = filterFactory;
    }
}
