using PixivApi.Core;
using PixivApi.Core.Local;

namespace PixivApi.Console;

public partial class LocalClient
{
    [Command("encode", "")]
    public async ValueTask EncodeAsync(
        [Option(0, ArgumentDescriptions.DatabaseDescription)] string path,
        [Option(1, ArgumentDescriptions.FilterDescription)] string? filter = null,
        [Option("o")] bool original = false,
        [Option("t")] bool thumbanil = false,
        [Option("u")] bool ugoira = false
    )
    {
        if (!original && !thumbanil && !ugoira)
        {
            return;
        }

        var token = Context.CancellationToken;
        var database = await IOUtility.MessagePackDeserializeAsync<DatabaseFile>(path, token).ConfigureAwait(false);
        if (database is null)
        {
            return;
        }

        var artworkFilter = string.IsNullOrWhiteSpace(filter) ? null : await IOUtility.JsonDeserializeAsync<ArtworkFilter>(filter, token).ConfigureAwait(false);
        var artworks = artworkFilter is null ? database.ArtworkDictionary.Values : await FilterExtensions.CreateEnumerableWithoutFileExistanceFilterAsync(database, artworkFilter, token).ConfigureAwait(false);
        foreach (var artwork in artworks)
        {
            if (token.IsCancellationRequested)
            {
                return;
            }

            if (original && converter.OriginalConverter is { } originalConverter)
            {
                _ = await originalConverter.TryConvertAsync(artwork, logger, token).ConfigureAwait(false);
            }

            if (thumbanil && converter.ThumbnailConverter is { } thumbnailConverter)
            {
                _ = await thumbnailConverter.TryConvertAsync(artwork, logger, token).ConfigureAwait(false);
            }

            if (ugoira && converter.UgoiraZipConverter is { } ugoiraZipConverter)
            {
                _ = await ugoiraZipConverter.TryConvertAsync(artwork, logger, token).ConfigureAwait(false);
            }
        }
    }

    [Command("delete-unneccessary-encoded", "")]
    public async ValueTask DeleteUnneccessaryEncodedAsync(
        [Option(0, ArgumentDescriptions.DatabaseDescription)] string path,
        [Option(1, ArgumentDescriptions.FilterDescription)] string? filter = null,
        [Option("o")] bool original = false,
        [Option("t")] bool thumbanil = false,
        [Option("u")] bool ugoira = false
    )
    {
        if (!original && !thumbanil && !ugoira)
        {
            return;
        }

        var token = Context.CancellationToken;
        var database = await IOUtility.MessagePackDeserializeAsync<DatabaseFile>(path, token).ConfigureAwait(false);
        if (database is null)
        {
            return;
        }

        var artworkFilter = string.IsNullOrWhiteSpace(filter) ? null : await IOUtility.JsonDeserializeAsync<ArtworkFilter>(filter, token).ConfigureAwait(false);
        var artworks = artworkFilter is null ? database.ArtworkDictionary.Values : await FilterExtensions.CreateEnumerableWithoutFileExistanceFilterAsync(database, artworkFilter, token).ConfigureAwait(false);
        await Parallel.ForEachAsync(artworks, token, (artwork, token) =>
        {
            if (token.IsCancellationRequested)
            {
                return ValueTask.FromCanceled(token);
            }

            if (original && converter.OriginalConverter is { } originalConverter)
            {
                originalConverter.DeleteUnneccessaryOriginal(artwork, logger);
            }

            if (thumbanil && converter.ThumbnailConverter is { } thumbnailConverter)
            {
                thumbnailConverter.DeleteUnneccessaryOriginal(artwork, logger);
            }

            if (ugoira && converter.UgoiraZipConverter is { } ugoiraZipConverter)
            {
                ugoiraZipConverter.DeleteUnneccessaryOriginal(artwork, logger);
            }

            return ValueTask.CompletedTask;
        }).ConfigureAwait(false);
    }
}
