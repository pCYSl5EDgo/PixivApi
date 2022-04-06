namespace PixivApi.Core.Plugin;

public sealed record class ConverterFacade(IConverter? UgoiraZipConverter, IConverter? OriginalConverter, IConverter? ThumbnailConverter) : IAsyncDisposable
{
    private static async ValueTask<IConverter?> GetAsync(string? plugin, ConfigSettings configSettings, IServiceProvider provider, object boxedCancellationToken) => await PluginUtility.LoadPluginAsync(plugin, configSettings, provider, boxedCancellationToken).ConfigureAwait(false) as IConverter;

    public static async Task<ConverterFacade> CreateAsync(ConfigSettings configSettings, IServiceProvider provider, CancellationToken token)
    {
        object boxedToken = token;
        var ugoiraZipConverter = await GetAsync(configSettings.UgoiraZipConverterPlugin, configSettings, provider, boxedToken).ConfigureAwait(false);
        var originalConverter = await GetAsync(configSettings.OriginalConverterPlugin, configSettings, provider, boxedToken).ConfigureAwait(false);
        var thumbnailConverter = await GetAsync(configSettings.ThumbnailConverterPlugin, configSettings, provider, boxedToken).ConfigureAwait(false);
        return new ConverterFacade(ugoiraZipConverter, originalConverter, thumbnailConverter);
    }

    public async ValueTask DisposeAsync()
    {
        if (UgoiraZipConverter is not null)
        {
            await UgoiraZipConverter.DisposeAsync().ConfigureAwait(false);
        }

        if (OriginalConverter is not null)
        {
            await OriginalConverter.DisposeAsync().ConfigureAwait(false);
        }

        if (ThumbnailConverter is not null)
        {
            await ThumbnailConverter.DisposeAsync().ConfigureAwait(false);
        }
    }
}
