using PixivApi.Core;

namespace PixivApi.Console;

[Command("local")]
public sealed partial class LocalClient : ConsoleAppBase
{
    private readonly ILogger<LocalClient> logger;
    private readonly ConfigSettings configSettings;
    private readonly FinderFacade finder;
    private readonly ConverterFacade converter;

    public LocalClient(ILogger<LocalClient> logger, ConfigSettings configSettings, FinderFacade finderFacade, ConverterFacade converterFacade)
    {
        this.logger = logger;
        this.configSettings = configSettings;
        finder = finderFacade;
        converter = converterFacade;
    }
}
