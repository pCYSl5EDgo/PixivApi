using System.Buffers;
using System.Net.Mime;
using System.Text.Json;

namespace PixivApi.Site;

public sealed record class ArtworkWithFileUrlAsyncResponses(IAsyncEnumerable<Artwork> Artworks, IDatabase? DatabaseToStringify, IFinder? UgoiraZipFinder, IFinder? UgoiraThumbnailFinder, IFinder? UgoiraOriginalFinder, IFinderWithIndex? ThumbnailFinder, IFinderWithIndex? OriginalFinder, JsonSerializerOptions JsonSerializerOptions) : IResult
{
    public async Task ExecuteAsync(HttpContext httpContext)
    {
        var token = httpContext.RequestAborted;
        httpContext.Response.ContentType = MediaTypeNames.Application.Json;
        httpContext.Response.StatusCode = 200;
        httpContext.Response.ContentLength = null;
        var body = httpContext.Response.Body;
        await httpContext.Response.StartAsync(token).ConfigureAwait(false);
        var array = ArrayPool<byte>.Shared.Rent(1);
        try
        {
            array[0] = (byte)'[';
            await body.WriteAsync(array.AsMemory(0, 1), token).ConfigureAwait(false);
            var notFirst = false;
            await foreach (var artwork in Artworks.WithCancellation(token))
            {
                if (DatabaseToStringify is not { } database)
                {
                    artwork.IsStringified = false;
                }
                else if (!artwork.IsStringified)
                {
                    await artwork.StringifyAsync(database, database, database, token).ConfigureAwait(false);
                }

                if (notFirst)
                {
                    array[0] = (byte)',';
                    await body.WriteAsync(array.AsMemory(0, 1), token).ConfigureAwait(false);
                }
                else
                {
                    notFirst = true;
                }

                await using var writer = new Utf8JsonWriter(body, new JsonWriterOptions
                {
                    SkipValidation = true,
                });

                if (artwork.Type == ArtworkType.Ugoira)
                {
                    var utility = new UgoiraArtworkUtilityStruct(artwork, UgoiraZipFinder, UgoiraThumbnailFinder, UgoiraOriginalFinder);
                    await JsonSerializer.SerializeAsync(body, utility, JsonSerializerOptions, token).ConfigureAwait(false);
                }
                else
                {
                    var utility = new NotUgoiraArtworkUtilityStruct(artwork, ThumbnailFinder, OriginalFinder);
                    await JsonSerializer.SerializeAsync(body, utility, JsonSerializerOptions, token).ConfigureAwait(false);
                }
            }

            array[0] = (byte)']';
            await body.WriteAsync(array.AsMemory(0, 1), token).ConfigureAwait(false);
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(array);
        }

        await httpContext.Response.CompleteAsync().ConfigureAwait(false);
    }
}
