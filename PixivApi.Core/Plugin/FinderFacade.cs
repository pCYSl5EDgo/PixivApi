namespace PixivApi.Core.Plugin;

public sealed class FinderFacade
{
    private FinderFacade(IFinder ugoiraZipFinder, IFinder ugoiraThumbnailFinder, IFinder ugoiraOriginalFinder, IFinderWithIndex illustThumbnailFinder, IFinderWithIndex illustOriginalFinder, IFinderWithIndex mangaThumbnailFinder, IFinderWithIndex mangaOriginalFinder)
    {
        UgoiraZipFinder = ugoiraZipFinder;
        UgoiraThumbnailFinder = ugoiraThumbnailFinder;
        UgoiraOriginalFinder = ugoiraOriginalFinder;
        IllustThumbnailFinder = illustThumbnailFinder;
        IllustOriginalFinder = illustOriginalFinder;
        MangaThumbnailFinder = mangaThumbnailFinder;
        MangaOriginalFinder = mangaOriginalFinder;
    }

    public readonly IFinder UgoiraZipFinder;
    public readonly IFinder UgoiraThumbnailFinder;
    public readonly IFinder UgoiraOriginalFinder;
    public readonly IFinderWithIndex IllustThumbnailFinder;
    public readonly IFinderWithIndex IllustOriginalFinder;
    public readonly IFinderWithIndex MangaThumbnailFinder;
    public readonly IFinderWithIndex MangaOriginalFinder;

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
