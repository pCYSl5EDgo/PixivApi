namespace PixivApi.Core.Plugin;

public sealed class FinderFacade : IAsyncDisposable
{
    private FinderFacade(IFinder ugoiraZipFinder, IFinder ugoiraThumbnailFinder, IFinder ugoiraOriginalFinder, IFinderWithIndex illustThumbnailFinder, IFinderWithIndex illustOriginalFinder, IFinderWithIndex mangaThumbnailFinder, IFinderWithIndex mangaOriginalFinder, IFinder ugoiraZipFinderDefault, IFinder ugoiraThumbnailFinderDefault, IFinder ugoiraOriginalFinderDefault, IFinderWithIndex illustThumbnailFinderDefault, IFinderWithIndex illustOriginalFinderDefault, IFinderWithIndex mangaThumbnailFinderDefault, IFinderWithIndex mangaOriginalFinderDefault)
    {
        UgoiraZipFinder = ugoiraZipFinder;
        UgoiraThumbnailFinder = ugoiraThumbnailFinder;
        UgoiraOriginalFinder = ugoiraOriginalFinder;
        IllustThumbnailFinder = illustThumbnailFinder;
        IllustOriginalFinder = illustOriginalFinder;
        MangaThumbnailFinder = mangaThumbnailFinder;
        MangaOriginalFinder = mangaOriginalFinder;
        DefaultUgoiraZipFinder = ugoiraZipFinderDefault;
        DefaultUgoiraThumbnailFinder = ugoiraThumbnailFinderDefault;
        DefaultUgoiraOriginalFinder = ugoiraOriginalFinderDefault;
        DefaultIllustThumbnailFinder = illustThumbnailFinderDefault;
        DefaultIllustOriginalFinder = illustOriginalFinderDefault;
        DefaultMangaThumbnailFinder = mangaThumbnailFinderDefault;
        DefaultMangaOriginalFinder = mangaOriginalFinderDefault;
    }

    public readonly IFinder UgoiraZipFinder;
    public readonly IFinder UgoiraThumbnailFinder;
    public readonly IFinder UgoiraOriginalFinder;
    public readonly IFinderWithIndex IllustThumbnailFinder;
    public readonly IFinderWithIndex IllustOriginalFinder;
    public readonly IFinderWithIndex MangaThumbnailFinder;
    public readonly IFinderWithIndex MangaOriginalFinder;

    public readonly IFinder DefaultUgoiraZipFinder;
    public readonly IFinder DefaultUgoiraThumbnailFinder;
    public readonly IFinder DefaultUgoiraOriginalFinder;
    public readonly IFinderWithIndex DefaultIllustThumbnailFinder;
    public readonly IFinderWithIndex DefaultIllustOriginalFinder;
    public readonly IFinderWithIndex DefaultMangaThumbnailFinder;
    public readonly IFinderWithIndex DefaultMangaOriginalFinder;

    private static async ValueTask<IFinder?> GetFinderAsync(string? plugin, ConfigSettings configSettings, IServiceProvider provider, object boxedCancellationToken) => await PluginUtility.LoadPluginAsync(plugin, configSettings, provider, boxedCancellationToken).ConfigureAwait(false) as IFinder;

    private static async ValueTask<IFinderWithIndex?> GetFinderWithIndexAsync(string? plugin, ConfigSettings configSettings, IServiceProvider provider, object boxedCancellationToken) => await PluginUtility.LoadPluginAsync(plugin, configSettings, provider, boxedCancellationToken).ConfigureAwait(false) as IFinderWithIndex;

    public static async Task<FinderFacade> CreateAsync(ConfigSettings configSettings, IServiceProvider provider, CancellationToken token)
    {
        var ugoiraZipFinderPluginDefault = new DefaultUgoiraZipFinder(configSettings.UgoiraFolder);
        var ugoiraThumbnailFinderPluginDefault = new DefaultUgoiraThumbnailFinder(configSettings.ThumbnailFolder);
        var ugoiraOriginalFinderPluginDefault = new DefaultUgoiraOriginalFinder(configSettings.OriginalFolder);
        var illustThumbnailFinderPluginDefault = new DefaultNotUgoiraThumbnailFinder(configSettings.ThumbnailFolder);
        var illustOriginalFinderPluginDefault = new DefaultNotUgoiraOriginalFinder(configSettings.OriginalFolder);
        var mangaThumbnailFinderPluginDefault = new DefaultNotUgoiraThumbnailFinder(configSettings.ThumbnailFolder);
        var mangaOriginalFinderPluginDefault = new DefaultNotUgoiraOriginalFinder(configSettings.OriginalFolder);
        object boxedToken = token;
        var ugoiraZipFinderPlugin = await GetFinderAsync(configSettings.UgoiraZipFinderPlugin, configSettings, provider, boxedToken).ConfigureAwait(false) ?? ugoiraZipFinderPluginDefault;
        var ugoiraThumbnailFinderPlugin = await GetFinderAsync(configSettings.UgoiraThumbnailFinderPlugin, configSettings, provider, boxedToken).ConfigureAwait(false) ?? ugoiraThumbnailFinderPluginDefault;
        var ugoiraOriginalFinderPlugin = await GetFinderAsync(configSettings.UgoiraOriginalFinderPlugin, configSettings, provider, boxedToken).ConfigureAwait(false) ?? ugoiraOriginalFinderPluginDefault;
        var illustThumbnailFinderPlugin = await GetFinderWithIndexAsync(configSettings.IllustThumbnailFinderPlugin, configSettings, provider, boxedToken).ConfigureAwait(false) ?? illustThumbnailFinderPluginDefault;
        var illustOriginalFinderPlugin = await GetFinderWithIndexAsync(configSettings.IllustOriginalFinderPlugin, configSettings, provider, boxedToken).ConfigureAwait(false) ?? illustOriginalFinderPluginDefault;
        var mangaThumbnailFinderPlugin = await GetFinderWithIndexAsync(configSettings.MangaThumbnailFinderPlugin, configSettings, provider, boxedToken).ConfigureAwait(false) ?? mangaThumbnailFinderPluginDefault;
        var mangaOriginalFinderPlugin = await GetFinderWithIndexAsync(configSettings.MangaOriginalFinderPlugin, configSettings, provider, boxedToken).ConfigureAwait(false) ?? mangaOriginalFinderPluginDefault;
        return new(ugoiraZipFinderPlugin, ugoiraThumbnailFinderPlugin, ugoiraOriginalFinderPlugin, illustThumbnailFinderPlugin, illustOriginalFinderPlugin, mangaThumbnailFinderPlugin, mangaOriginalFinderPlugin, ugoiraZipFinderPluginDefault, ugoiraThumbnailFinderPluginDefault, ugoiraOriginalFinderPluginDefault, illustThumbnailFinderPluginDefault, illustOriginalFinderPluginDefault, mangaThumbnailFinderPluginDefault, mangaOriginalFinderPluginDefault);
    }

    public async ValueTask DisposeAsync()
    {
        await UgoiraZipFinder.DisposeAsync().ConfigureAwait(false);
        await UgoiraThumbnailFinder.DisposeAsync().ConfigureAwait(false);
        await UgoiraOriginalFinder.DisposeAsync().ConfigureAwait(false);
        await IllustThumbnailFinder.DisposeAsync().ConfigureAwait(false);
        await IllustOriginalFinder.DisposeAsync().ConfigureAwait(false);
        await MangaThumbnailFinder.DisposeAsync().ConfigureAwait(false);
        await MangaOriginalFinder.DisposeAsync().ConfigureAwait(false);
    }
}
