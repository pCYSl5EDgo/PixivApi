namespace PixivApi.Core;

public sealed class FinderFacade
{
    private FinderFacade(IFinder ugoiraZipFinderPlugin, IFinder ugoiraThumbnailFinderPlugin, IFinder ugoiraOriginalFinderPlugin, IFinderWithIndex illustThumbnailFinderPlugin, IFinderWithIndex illustOriginalFinderPlugin, IFinderWithIndex mangaThumbnailFinderPlugin, IFinderWithIndex mangaOriginalFinderPlugin)
    {
        UgoiraZipFinderPlugin = ugoiraZipFinderPlugin;
        UgoiraThumbnailFinderPlugin = ugoiraThumbnailFinderPlugin;
        UgoiraOriginalFinderPlugin = ugoiraOriginalFinderPlugin;
        IllustThumbnailFinderPlugin = illustThumbnailFinderPlugin;
        IllustOriginalFinderPlugin = illustOriginalFinderPlugin;
        MangaThumbnailFinderPlugin = mangaThumbnailFinderPlugin;
        MangaOriginalFinderPlugin = mangaOriginalFinderPlugin;
    }

    public readonly IFinder UgoiraZipFinderPlugin;
    public readonly IFinder UgoiraThumbnailFinderPlugin;
    public readonly IFinder UgoiraOriginalFinderPlugin;
    public readonly IFinderWithIndex IllustThumbnailFinderPlugin;
    public readonly IFinderWithIndex IllustOriginalFinderPlugin;
    public readonly IFinderWithIndex MangaThumbnailFinderPlugin;
    public readonly IFinderWithIndex MangaOriginalFinderPlugin;

    private static async ValueTask<IFinder> GetFinderAsync<TDefaultImpl>(string? plugin, ConfigSettings configSettings, object boxedCancellationToken, CancellationToken cancellationToken)
        where TDefaultImpl : class, IFinder
    {
        var answer = await PluginUtility.LoadPluginAsync(plugin, configSettings, boxedCancellationToken).ConfigureAwait(false) as IFinder;
        if (answer is null)
        {
            answer = await TDefaultImpl.CreateAsync(Environment.ProcessPath ?? throw new NullReferenceException(), configSettings, cancellationToken).ConfigureAwait(false) as IFinder;
        }

        return answer ?? throw new NullReferenceException();
    }

    private static async ValueTask<IFinderWithIndex> GetFinderWithIndexAsync<TDefaultImpl>(string? plugin, ConfigSettings configSettings, object boxedCancellationToken, CancellationToken cancellationToken)
        where TDefaultImpl : class, IFinderWithIndex
    {
        var answer = await PluginUtility.LoadPluginAsync(plugin, configSettings, boxedCancellationToken).ConfigureAwait(false) as IFinderWithIndex;
        if (answer is null)
        {
            answer = await TDefaultImpl.CreateAsync(Environment.ProcessPath ?? throw new NullReferenceException(), configSettings, cancellationToken).ConfigureAwait(false) as IFinderWithIndex;
        }

        return answer ?? throw new NullReferenceException();
    }

    public static async ValueTask<FinderFacade> CreateAsync(ConfigSettings configSettings, CancellationToken token)
    {
        object boxedToken = token;
        var ugoiraZipFinderPlugin = await GetFinderAsync<DefaultUgoiraZipFinder>(configSettings.UgoiraZipFinderPlugin, configSettings, boxedToken, token).ConfigureAwait(false);
        var ugoiraThumbnailFinderPlugin = await GetFinderAsync<DefaultUgoiraThumbnailFinder>(configSettings.UgoiraThumbnailFinderPlugin, configSettings, boxedToken, token).ConfigureAwait(false);
        var ugoiraOriginalFinderPlugin = await GetFinderAsync<DefaultUgoiraOriginalFinder>(configSettings.UgoiraOriginalFinderPlugin, configSettings, boxedToken, token).ConfigureAwait(false);
        var illustThumbnailFinderPlugin = await GetFinderWithIndexAsync<DefaultNotUgoiraThumbnailFinder>(configSettings.IllustThumbnailFinderPlugin, configSettings, boxedToken, token).ConfigureAwait(false);
        var illustOriginalFinderPlugin = await GetFinderWithIndexAsync<DefaultNotUgoiraOriginalFinder>(configSettings.IllustOriginalFinderPlugin, configSettings, boxedToken, token).ConfigureAwait(false);
        var mangaThumbnailFinderPlugin = await GetFinderWithIndexAsync<DefaultNotUgoiraThumbnailFinder>(configSettings.MangaThumbnailFinderPlugin, configSettings, boxedToken, token).ConfigureAwait(false);
        var mangaOriginalFinderPlugin = await GetFinderWithIndexAsync<DefaultNotUgoiraOriginalFinder>(configSettings.MangaOriginalFinderPlugin, configSettings, boxedToken, token).ConfigureAwait(false);
        return new(ugoiraZipFinderPlugin, ugoiraThumbnailFinderPlugin, ugoiraOriginalFinderPlugin, illustThumbnailFinderPlugin, illustOriginalFinderPlugin, mangaThumbnailFinderPlugin, mangaOriginalFinderPlugin);
    }
}
