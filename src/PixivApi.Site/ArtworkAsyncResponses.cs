using System.Buffers;
using System.Net.Mime;
using System.Text.Json;

namespace PixivApi.Site;

public sealed record class ArtworkAsyncResponses(IAsyncEnumerable<Artwork> Artworks, IDatabase? DatabaseToStringify, JsonSerializerOptions JsonSerializerOptions) : IResult
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

                await JsonSerializer.SerializeAsync(body, artwork, JsonSerializerOptions, token).ConfigureAwait(false);
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
