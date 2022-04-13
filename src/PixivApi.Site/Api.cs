using Microsoft.AspNetCore.Mvc;
using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;

namespace PixivApi.Site;

public static class Api
{
    public static async Task<IResult> CountAsync([FromQuery] string? filter, [FromServices] IDatabaseFactory databaseFactory, [FromServices] IArtworkFilterFactory<ReadOnlyMemory<char>> filterFactory, CancellationToken token)
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

            var enumerable = database.FilterAsync(artworkFilter, token);
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

    public static async Task<IResult> GetThumbnailAsync([FromRoute] ulong id, [FromRoute] int index, [FromServices] IDatabaseFactory databaseFactory, [FromServices] FinderFacade finder, CancellationToken token)
    {
        if (id == 0 || index < 0)
        {
            goto NOT_FOUND;
        }

        var database = await databaseFactory.RentAsync(token).ConfigureAwait(false);
        try
        {
            var artwork = await database.GetArtworkAsync(id, token).ConfigureAwait(false);
            if (artwork is null || index >= artwork.PageCount)
            {
                goto NOT_FOUND;
            }

            FileInfo info;
            switch (artwork.Type)
            {
                case ArtworkType.Illust:
                    info = finder.IllustThumbnailFinder.Find(id, artwork.Extension, (uint)index);
                    break;
                case ArtworkType.Manga:
                    info = finder.MangaThumbnailFinder.Find(id, artwork.Extension, (uint)index);
                    break;
                case ArtworkType.Ugoira:
                    info = finder.UgoiraThumbnailFinder.Find(id, artwork.Extension);
                    break;
                case ArtworkType.None:
                default:
                    goto NOT_FOUND;
            }

            if (!info.Exists || info.Length == 0)
            {
                goto NOT_FOUND;
            }

            static string CalcPath(ulong id, FileInfo info)
            {
                DefaultInterpolatedStringHandler handler = $"/Thumbnail/";
                IOUtility.AppendHashPath(ref handler, id);
                handler.AppendFormatted(info.Name);
                return handler.ToStringAndClear();
            }

            return Results.Redirect(CalcPath(id, info), true, true);
        }
        finally
        {
            databaseFactory.Return(ref database);
        }

    NOT_FOUND:
        return Results.NotFound();
    }

    public static async Task<IResult> GetOriginalAsync([FromRoute] ulong id, [FromRoute] int index, [FromServices] IDatabaseFactory databaseFactory, [FromServices] FinderFacade finder, CancellationToken token)
    {
        if (id == 0 || index < 0)
        {
            goto NOT_FOUND;
        }

        var database = await databaseFactory.RentAsync(token).ConfigureAwait(false);
        try
        {
            var artwork = await database.GetArtworkAsync(id, token).ConfigureAwait(false);
            if (artwork is null || index >= artwork.PageCount)
            {
                goto NOT_FOUND;
            }

            FileInfo info;
            switch (artwork.Type)
            {
                case ArtworkType.Illust:
                    info = finder.IllustOriginalFinder.Find(id, artwork.Extension, (uint)index);
                    break;
                case ArtworkType.Manga:
                    info = finder.MangaOriginalFinder.Find(id, artwork.Extension, (uint)index);
                    break;
                case ArtworkType.Ugoira:
                    info = finder.UgoiraOriginalFinder.Find(id, artwork.Extension);
                    break;
                case ArtworkType.None:
                default:
                    goto NOT_FOUND;
            }

            if (!info.Exists || info.Length == 0)
            {
                goto NOT_FOUND;
            }

            static string CalcPath(ulong id, FileInfo info)
            {
                DefaultInterpolatedStringHandler handler = $"/Original/";
                IOUtility.AppendHashPath(ref handler, id);
                handler.AppendFormatted(info.Name);
                return handler.ToStringAndClear();
            }

            return Results.Redirect(CalcPath(id, info), true, true);
        }
        finally
        {
            databaseFactory.Return(ref database);
        }

    NOT_FOUND:
        return Results.NotFound();
    }

    //public static async Task<IResult> GetFollowsNewWorkAsync(HttpContext context, [FromServices] HttpClient client, [FromServices] ConfigSettings configSettings, [FromServices] IDatabaseFactory databaseFactory, [FromServices] FinderFacade finderFacade, [FromServices] ConverterFacade converterFacade, CancellationToken token)
    //{
    //    var accessToken = await AccessTokenUtility.GetAccessTokenAsync(client, configSettings, 0, token).ConfigureAwait(false);
    //    if (string.IsNullOrWhiteSpace(accessToken))
    //    {
    //        return Results.BadRequest("Cannot get access token.");
    //    }

    //    var authorization = new AuthenticationHeaderValue("Bearer", accessToken);
    //    const string UrlPublic = "https://app-api.pixiv.net/v2/illust/follow?restrict=public";
    //    const string UrlPrivate = "https://app-api.pixiv.net/v2/illust/follow?restrict=private";
    //    var database = await databaseFactory.RentAsync(token).ConfigureAwait(false);
    //    var transactional = database as ITransactionalDatabase;
    //    try
    //    {
    //        static async ValueTask<(IResult?, string?, ArtworkResponseContent[]?)> RequestAsync(AuthenticationHeaderValue authorization, HttpClient client, string hashSecret, string url, CancellationToken token)
    //        {
    //            using var request = new HttpRequestMessage(HttpMethod.Get, url);
    //            request.Headers.Authorization = authorization;
    //            if (!request.TryAddToHeader(hashSecret, "app-api.pixiv.net"))
    //            {
    //                return (Results.Problem("Error when adding to header."), null, null);
    //            }

    //            using var response = await client.SendAsync(request, token).ConfigureAwait(false);
    //            switch (response.StatusCode)
    //            {
    //                case HttpStatusCode.OK:
    //                    var data = IOUtility.JsonDeserialize<IllustsResponseData>(await response.Content.ReadAsByteArrayAsync(token).ConfigureAwait(false));
    //                    return (null, string.IsNullOrWhiteSpace(data.NextUrl) ? null : data.NextUrl, data.Illusts.Length == 0 ? null : data.Illusts);
    //                case HttpStatusCode.Forbidden:
    //                    return (Results.Ok(), null, null);
    //                default:
    //                    return (Results.Problem("Not supported status code.", statusCode: (int)response.StatusCode), null, null);
    //            }
    //        }
    //    }
    //    finally
    //    {
    //        if (transactional is not null)
    //        {
    //            await transactional.EndTransactionAsync(token).ConfigureAwait(false);
    //        }

    //        databaseFactory.Return(ref database);
    //    }
    //}

    //public static async Task<IResult> EnsureThumbnailAsync(ulong id, [FromServices] IDatabaseFactory databaseFactory, [FromServices] FinderFacade finderFacade, CancellationToken token)
    //{
    //    var database = await databaseFactory.RentAsync(token).ConfigureAwait(false);
    //    try
    //    {
    //        var artwork =  await database.GetArtworkAsync(id, token).ConfigureAwait(false);
    //        switch (artwork?.Type)
    //        {
    //            case ArtworkType.Illust:
    //                break;
    //            case ArtworkType.Manga:
    //                break;
    //            case ArtworkType.Ugoira:
    //                break;
    //            default:
    //                break;
    //        }
    //    }
    //    finally
    //    {
    //        databaseFactory.Return(ref database);
    //    }
    //}

    //public static async Task<IResult> EnsureContentAsync(ulong id, [FromServices] IDatabaseFactory databaseFactory, [FromServices] FinderFacade finderFacade, CancellationToken token)
    //{
    //    var database = await databaseFactory.RentAsync(token).ConfigureAwait(false);
    //    try
    //    {
    //        var artwork = await database.GetArtworkAsync(id, token).ConfigureAwait(false);
    //    }
    //    finally
    //    {
    //        databaseFactory.Return(ref database);
    //    }
    //}
}
