using Microsoft.AspNetCore.Mvc;
using System.Buffers;
using System.Net.Mime;
using System.Text.Json;

namespace PixivApi.Site;

public class Api
{
    private readonly ConfigSettings configSettings;
    private readonly HttpClient client;
    private readonly DatabaseFile database;
    private readonly FinderFacade finderFacade;
    private readonly ConverterFacade converterFacade;

    public Api(ConfigSettings configSettings, HttpClient client, DatabaseFile database, FinderFacade finderFacade, ConverterFacade converterFacade)
    {
        this.configSettings = configSettings;
        this.client = client;
        this.database = database;
        this.finderFacade = finderFacade;
        this.converterFacade = converterFacade;
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

    public Task<IResult> MapAsync([FromQuery] string? filter, [FromQuery(Name = "to-string")] bool? toString, CancellationToken token)
    {
        var artworkFilter = ParseFilter(filter);
        if (artworkFilter is null)
        {
            return Task.FromResult(Results.BadRequest("empty filter error"));
        }

        var enumerable = FilterExtensions.CreateAsyncEnumerable(finderFacade, database, artworkFilter, token);
        var shouldStringify = (!toString.HasValue || toString.Value) ? database : null;
        var response = new ArtworkAsyncResponses(enumerable, shouldStringify);
        return Task.FromResult<IResult>(response);
    }
}

public sealed record class ArtworkAsyncResponses(IAsyncEnumerable<Artwork> Artworks, DatabaseFile? DatabaseFileToStringify) : IResult
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
                if (DatabaseFileToStringify is not { } database)
                {
                    artwork.IsStringified = false;
                }
                else if (!artwork.IsStringified)
                {
                    artwork.Stringify(database.UserDictionary, database.TagSet, database.ToolSet);
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

                await JsonSerializer.SerializeAsync(body, artwork, IOUtility.JsonSerializerOptionsNoIndent, token).ConfigureAwait(false);
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
