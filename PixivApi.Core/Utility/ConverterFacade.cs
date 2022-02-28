namespace PixivApi.Core;

public sealed record class ConverterFacade(IConverter? UgoiraZipConverter, IConverter? OriginalConverter, IConverter? ThumbnailConverter)
{
    private static async ValueTask<IConverter?> GetAsync(string? plugin, ConfigSettings configSettings, object boxedCancellationToken)
    {
        return await PluginUtility.LoadPluginAsync(plugin, configSettings, boxedCancellationToken).ConfigureAwait(false) as IConverter;
    }

    public static async ValueTask<ConverterFacade> CreateAsync(ConfigSettings configSettings, CancellationToken token)
    {
        object boxedToken = token;
        var ugoiraZipConverter = await GetAsync(configSettings.UgoiraZipConverterPlugin, configSettings, boxedToken).ConfigureAwait(false);
        var originalConverter = await GetAsync(configSettings.OriginalConverterPlugin, configSettings, boxedToken).ConfigureAwait(false);
        var thumbnailConverter = await GetAsync(configSettings.ThumbnailConverterPlugin, configSettings, boxedToken).ConfigureAwait(false);
        return new ConverterFacade(ugoiraZipConverter, originalConverter, thumbnailConverter);
    }
}
