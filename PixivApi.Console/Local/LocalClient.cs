using PixivApi.Core;

namespace PixivApi.Console;

[Command("local")]
public sealed partial class LocalClient : ConsoleAppBase
{
    private readonly ILogger<LocalClient> logger;
    private readonly ConfigSettings configSettings;
    private readonly FinderFacade finder;

    public LocalClient(ILogger<LocalClient> logger, ConfigSettings configSettings, FinderFacade finderFacade)
    {
        this.logger = logger;
        this.configSettings = configSettings;
        finder = finderFacade;
    }
}
