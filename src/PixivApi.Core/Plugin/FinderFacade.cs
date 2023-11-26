namespace PixivApi.Core.Plugin;

public sealed class FinderFacade : IAsyncDisposable
{
    private FinderFacade(IFinder ugoiraZipFinder, IFinder ugoiraOriginalFinder, IFinderWithIndex illustOriginalFinder, IFinderWithIndex mangaOriginalFinder, IFinder ugoiraZipFinderDefault, IFinder ugoiraOriginalFinderDefault, IFinderWithIndex illustOriginalFinderDefault, IFinderWithIndex mangaOriginalFinderDefault)
    {
        UgoiraZipFinder = ugoiraZipFinder;
        UgoiraOriginalFinder = ugoiraOriginalFinder;
        IllustOriginalFinder = illustOriginalFinder;
        MangaOriginalFinder = mangaOriginalFinder;
        DefaultUgoiraZipFinder = ugoiraZipFinderDefault;
        DefaultUgoiraOriginalFinder = ugoiraOriginalFinderDefault;
        DefaultIllustOriginalFinder = illustOriginalFinderDefault;
        DefaultMangaOriginalFinder = mangaOriginalFinderDefault;
    }

    public readonly IFinder UgoiraZipFinder;
    public readonly IFinder UgoiraOriginalFinder;
    public readonly IFinderWithIndex IllustOriginalFinder;
    public readonly IFinderWithIndex MangaOriginalFinder;

    public readonly IFinder DefaultUgoiraZipFinder;
    public readonly IFinder DefaultUgoiraOriginalFinder;
    public readonly IFinderWithIndex DefaultIllustOriginalFinder;
    public readonly IFinderWithIndex DefaultMangaOriginalFinder;

    private static async ValueTask<IFinder?> GetFinderAsync(string? plugin, ConfigSettings configSettings, IServiceProvider provider, object boxedCancellationToken) => await PluginUtility.LoadPluginAsync(plugin, configSettings, provider, boxedCancellationToken).ConfigureAwait(false) as IFinder;

    private static async ValueTask<IFinderWithIndex?> GetFinderWithIndexAsync(string? plugin, ConfigSettings configSettings, IServiceProvider provider, object boxedCancellationToken) => await PluginUtility.LoadPluginAsync(plugin, configSettings, provider, boxedCancellationToken).ConfigureAwait(false) as IFinderWithIndex;

    public static async Task<FinderFacade> CreateAsync(ConfigSettings configSettings, IServiceProvider provider, CancellationToken token)
    {
        var ugoiraZipFinderPluginDefault = new DefaultUgoiraZipFinder(configSettings.UgoiraFolder);
        var ugoiraOriginalFinderPluginDefault = new DefaultUgoiraOriginalFinder(configSettings.OriginalFolder);
        var illustOriginalFinderPluginDefault = new DefaultNotUgoiraOriginalFinder(configSettings.OriginalFolder);
        var mangaOriginalFinderPluginDefault = new DefaultNotUgoiraOriginalFinder(configSettings.OriginalFolder);
        object boxedToken = token;
        var ugoiraZipFinderPlugin = await GetFinderAsync(configSettings.UgoiraZipFinderPlugin, configSettings, provider, boxedToken).ConfigureAwait(false) ?? ugoiraZipFinderPluginDefault;
        var ugoiraOriginalFinderPlugin = await GetFinderAsync(configSettings.UgoiraOriginalFinderPlugin, configSettings, provider, boxedToken).ConfigureAwait(false) ?? ugoiraOriginalFinderPluginDefault;
        var illustOriginalFinderPlugin = await GetFinderWithIndexAsync(configSettings.IllustOriginalFinderPlugin, configSettings, provider, boxedToken).ConfigureAwait(false) ?? illustOriginalFinderPluginDefault;
        var mangaOriginalFinderPlugin = await GetFinderWithIndexAsync(configSettings.MangaOriginalFinderPlugin, configSettings, provider, boxedToken).ConfigureAwait(false) ?? mangaOriginalFinderPluginDefault;
        return new(ugoiraZipFinderPlugin, ugoiraOriginalFinderPlugin, illustOriginalFinderPlugin, mangaOriginalFinderPlugin, ugoiraZipFinderPluginDefault, ugoiraOriginalFinderPluginDefault, illustOriginalFinderPluginDefault, mangaOriginalFinderPluginDefault);
    }

    public async ValueTask DisposeAsync()
    {
        await UgoiraZipFinder.DisposeAsync().ConfigureAwait(false);
        await UgoiraOriginalFinder.DisposeAsync().ConfigureAwait(false);
        await IllustOriginalFinder.DisposeAsync().ConfigureAwait(false);
        await MangaOriginalFinder.DisposeAsync().ConfigureAwait(false);
    }
}
