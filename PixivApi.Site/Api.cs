using Microsoft.AspNetCore.Mvc;
using System.Text.Json;

namespace PixivApi.Site;

public class Api
{
    private readonly ConfigSettings configSettings;
    private readonly HttpClient client;
    private readonly DatabaseFile database;
    private readonly FinderFacade finderFacade;
    private readonly ConverterFacade converterFacade;
    private readonly JsonSerializerOptions jsonSerializerOptions;

    public Api(ConfigSettings configSettings, HttpClient client, DatabaseFile database, FinderFacade finderFacade, ConverterFacade converterFacade, JsonSerializerOptions jsonSerializerOptions)
    {
        this.configSettings = configSettings;
        this.client = client;
        this.database = database;
        this.finderFacade = finderFacade;
        this.converterFacade = converterFacade;
        this.jsonSerializerOptions = jsonSerializerOptions;
    }

    private static ArtworkFilter? ParseFilter(string? filter)
    {
        ArtworkFilter? artworkFilter = null;
        if (!string.IsNullOrWhiteSpace(filter))
        {
            try
            {
                artworkFilter = IOUtility.JsonDeserialize<ArtworkFilter>(filter);
            }
            catch
            {
            }
        }

        return artworkFilter;
    }

    public async Task<IResult> CountAsync([FromQuery] string? filter, CancellationToken token)
    {
        var artworkFilter = ParseFilter(filter);

        if (artworkFilter is null)
        {
            return Results.Ok((ulong)database.ArtworkDictionary.Count);
        }

        await artworkFilter.InitializeAsync(finderFacade, database.UserDictionary, database.TagSet, token).ConfigureAwait(false);

        var count = 0UL;
        await Parallel.ForEachAsync(database.ArtworkDictionary.Values, token, (artwork, token) =>
        {
            if (token.IsCancellationRequested)
            {
                return ValueTask.FromCanceled(token);
            }

            if (artworkFilter.Filter(artwork))
            {
                _ = Interlocked.Increment(ref count);
            }

            return ValueTask.CompletedTask;
        }).ConfigureAwait(false);
        return Results.Ok(count);
    }

    public Task<IResult> MapAsync([FromQuery] string? filter, [FromQuery(Name = "to-string")] bool? toString, bool? ugoira, bool? thumbnail, bool? original, CancellationToken token)
    {
        var artworkFilter = ParseFilter(filter);
        if (artworkFilter is null)
        {
            return Task.FromResult(Results.BadRequest("empty filter error"));
        }

        var enumerable = FilterExtensions.CreateAsyncEnumerable(finderFacade, database, artworkFilter, token);
        var shouldStringify = (!toString.HasValue || toString.Value) ? database : null;
        var ugoiraZipFinder = ugoira == true ? finderFacade.UgoiraZipFinder : null;
        var ugoiraThumbnailFinder = thumbnail == true ? finderFacade.UgoiraThumbnailFinder : null;
        var ugoiraOriginalFinder = original == true ? finderFacade.UgoiraOriginalFinder : null;
        var thumbnailFinder = thumbnail == true ? finderFacade.IllustThumbnailFinder : null;
        var originalFinder = original == true ? finderFacade.IllustOriginalFinder : null;
        var response = new ArtworkWithFileUrlAsyncResponses(enumerable, shouldStringify, ugoiraZipFinder, ugoiraThumbnailFinder, ugoiraOriginalFinder, thumbnailFinder, originalFinder, jsonSerializerOptions);
        return Task.FromResult<IResult>(response);
    }

    public Task<IResult> HideAsync([FromRoute] ulong id, [FromQuery(Name = "reason")] string reasonText, HttpContext context)
    {
        var token = context.RequestAborted;
        if (token.IsCancellationRequested)
        {
            return Task.FromCanceled<IResult>(token);
        }

        if (context.Request.Method != HttpMethod.Patch.Method)
        {
            return Task.FromResult(Results.BadRequest("Http method must be PATCH."));
        }

        if (!database.ArtworkDictionary.TryGetValue(id, out var artwork))
        {
            return Task.FromResult(Results.NotFound());
        }

        if (!HideReasonConverter.TryParse(reasonText, out var reason))
        {
            return Task.FromResult(Results.BadRequest(reasonText + " is not supported reason."));
        }

        artwork.ExtraHideReason = reason;
        return Task.FromResult(Results.Ok());
    }
}
