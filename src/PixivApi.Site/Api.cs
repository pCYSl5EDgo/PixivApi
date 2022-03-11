using Microsoft.AspNetCore.Mvc;
using System.Text.Json;

namespace PixivApi.Site;

public static class Api
{
    public static async Task<IResult> CountAsync([FromQuery] string? filter, [FromServices] IDatabaseFactory databaseFactory, [FromServices] FinderFacade finderFacade, [FromServices] IArtworkFilterFactory<ReadOnlyMemory<char>> filterFactory, CancellationToken token)
    {
        var database = await databaseFactory.RentAsync(token).ConfigureAwait(false);
        try
        {
            var artworkFilter = string.IsNullOrWhiteSpace(filter) ? null : await filterFactory.CreateAsync(database, filter.AsMemory(), token).ConfigureAwait(false);
            if (artworkFilter is null)
            {
                return Results.Ok(await database.CountArtworkAsync(token).ConfigureAwait(false));
            }

            var count = await database.CountArtworkAsync(artworkFilter, token).ConfigureAwait(false);
            return Results.Ok(count);
        }
        finally
        {
            databaseFactory.Return(ref database);
        }
    }

    public static async Task<IResult> MapAsync([FromQuery] string? filter, [FromQuery(Name = "to-string")] bool? toString, bool? ugoira, bool? thumbnail, bool? original, [FromServices] IDatabaseFactory databaseFactory, [FromServices] FinderFacade finderFacade, [FromServices] JsonSerializerOptions jsonSerializerOptions, [FromServices] IArtworkFilterFactory<ReadOnlyMemory<char>> filterFactory, CancellationToken token)
    {
        var database = await databaseFactory.RentAsync(token).ConfigureAwait(false);
        try
        {
            var artworkFilter = await filterFactory.CreateAsync(database, filter.AsMemory(), token).ConfigureAwait(false);
            if (artworkFilter is null)
            {
                return Results.BadRequest("empty filter error");
            }

            var enumerable = database.FastArtworkFilterAsync(artworkFilter, token);
            var shouldStringify = (!toString.HasValue || toString.Value) ? database : null;
            var ugoiraZipFinder = ugoira == true ? finderFacade.UgoiraZipFinder : null;
            var ugoiraThumbnailFinder = thumbnail == true ? finderFacade.UgoiraThumbnailFinder : null;
            var ugoiraOriginalFinder = original == true ? finderFacade.UgoiraOriginalFinder : null;
            var thumbnailFinder = thumbnail == true ? finderFacade.IllustThumbnailFinder : null;
            var originalFinder = original == true ? finderFacade.IllustOriginalFinder : null;
            var response = new ArtworkWithFileUrlAsyncResponses(enumerable, shouldStringify, ugoiraZipFinder, ugoiraThumbnailFinder, ugoiraOriginalFinder, thumbnailFinder, originalFinder, jsonSerializerOptions);
            return response;
        }
        finally
        {
            databaseFactory.Return(ref database);
        }
    }

    public static async Task<IResult> HideAsync([FromRoute] ulong id, [FromQuery(Name = "reason")] string reasonText, HttpContext context, [FromServices] IDatabaseFactory databaseFactory)
    {
        var token = context.RequestAborted;
        token.ThrowIfCancellationRequested();
        if (context.Request.Method != HttpMethod.Patch.Method)
        {
            return Results.BadRequest("Http method must be PATCH.");
        }

        var database = await databaseFactory.RentAsync(token).ConfigureAwait(false);
        try
        {
            var artwork = await database.GetArtworkAsync(id, token).ConfigureAwait(false);
            if (artwork is null)
            {
                return Results.NotFound();
            }

            if (!HideReasonConverter.TryParse(reasonText, out var reason))
            {
                return Results.BadRequest(reasonText + " is not supported reason.");
            }

            artwork.ExtraHideReason = reason;
            return Results.Ok();
        }
        finally
        {
            databaseFactory.Return(ref database);
        }
    }
}
