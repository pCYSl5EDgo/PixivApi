using Cysharp.Diagnostics;
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
        if (artworkFilter is null)
        {
            foreach (var artwork in database.ArtworkDictionary.Values)
            {
                VirtualCodes.SetTitle($"{artwork.Id}");
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
        else
        {
            foreach (var artwork in await FilterExtensions.CreateEnumerableWithoutFileExistanceFilterAsync(database, artworkFilter, token).ConfigureAwait(false))
            {
                VirtualCodes.SetTitle($"{artwork.Id}");
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
    }
}
