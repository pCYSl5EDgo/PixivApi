namespace PixivApi.Core.Local;

public sealed class TagFilter
{
  [JsonPropertyName("exact")] public string[]? Exacts;
  [JsonPropertyName("partial")] public string[]? Partials;
  [JsonPropertyName("ignore-exact")] public string[]? IgnoreExacts;
  [JsonPropertyName("ignore-partial")] public string[]? IgnorePartials;

  [JsonPropertyName("or")] public bool Or = false;
  [JsonPropertyName("ignore-or")] public bool IgnoreOr = true;

  [JsonIgnore] private Complex complex;
  [JsonIgnore] private bool hasComplex;
  [JsonIgnore] private ITagDatabase? database;

  private struct Complex
  {
    public bool AlwaysFailIntersect;
    public bool AlwaysFailExcept;
    public uint[] IntersectArray;
    public uint[] ExceptArray;
    public uint[][] IntersectArrayArray;
    public uint[][] ExceptArrayArray;

    public Complex()
    {
      AlwaysFailIntersect = false;
      AlwaysFailExcept = false;
      IntersectArray = Array.Empty<uint>();
      ExceptArray = Array.Empty<uint>();
      IntersectArrayArray = Array.Empty<uint[]>();
      ExceptArrayArray = Array.Empty<uint[]>();
    }

    public async ValueTask InitializeAsync(TagFilter filter, CancellationToken token)
    {
      if (filter.database is null)
      {
        throw new NullReferenceException();
      }

      if (filter.Or)
      {
        IntersectArrayArray = await CalculateSetsAsync(filter.database, filter.Partials, filter.Exacts, token).ConfigureAwait(false);
      }
      else
      {
        (AlwaysFailIntersect, IntersectArray) = await CalculateArrayAsync(filter.database, filter.Exacts, token).ConfigureAwait(false);
        IntersectArrayArray = await CalculateSetsAsync(filter.database, filter.Partials, token).ConfigureAwait(false);
      }

      if (filter.IgnoreOr)
      {
        ExceptArrayArray = await CalculateSetsAsync(filter.database, filter.IgnorePartials, filter.IgnoreExacts, token).ConfigureAwait(false);
      }
      else
      {
        (AlwaysFailExcept, ExceptArray) = await CalculateArrayAsync(filter.database, filter.IgnoreExacts, token).ConfigureAwait(false);
        ExceptArrayArray = await CalculateSetsAsync(filter.database, filter.IgnorePartials, token).ConfigureAwait(false);
      }
    }
  }

  public void Initialize(ITagDatabase database) => this.database = database;

  private static async ValueTask<(bool, uint[])> CalculateArrayAsync(ITagDatabase database, string[]? exacts, CancellationToken token)
  {
    if (exacts is not { Length: > 0 })
    {
      return (false, Array.Empty<uint>());
    }

    var rental = ArrayPool<uint>.Shared.Rent(exacts.Length);
    try
    {
      var answerCount = 0;
      for (var i = 0; i < exacts.Length; i++)
      {
        token.ThrowIfCancellationRequested();
        var item = await database.FindTagAsync(exacts[i], token).ConfigureAwait(false);
        if (!item.HasValue)
        {
          return (true, Array.Empty<uint>());
        }

        rental[answerCount++] = item.Value;
      }

      var answer = new uint[answerCount];
      Array.Copy(rental, answer, answerCount);
      return (false, answer);
    }
    finally
    {
      ArrayPool<uint>.Shared.Return(rental);
    }
  }

  private static async ValueTask<uint[][]> CalculateSetsAsync(ITagDatabase database, string[]? partials, string[]? exacts, CancellationToken token)
  {
    if (exacts is not { Length: > 0 } && partials is not { Length: > 0 })
    {
      return Array.Empty<uint[]>();
    }

    var answer = new uint[1][];
    var set = new HashSet<uint>();
    if (exacts is { Length: > 0 })
    {
      foreach (var item in exacts)
      {
        token.ThrowIfCancellationRequested();
        var found = await database.FindTagAsync(item, token).ConfigureAwait(false);
        if (found.HasValue)
        {
          set.Add(found.Value);
        }
      }
    }

    if (partials is { Length: > 0 })
    {
      foreach (var item in partials)
      {
        token.ThrowIfCancellationRequested();
        await foreach (var found in database.EnumeratePartialMatchTagAsync(item, token))
        {
          set.Add(found);
        }
      }
    }

    answer[0] = set.ToArray();
    return answer;
  }

  private static async ValueTask<uint[][]> CalculateSetsAsync(ITagDatabase database, string[]? partials, CancellationToken token)
  {
    if (partials is not { Length: > 0 })
    {
      return Array.Empty<uint[]>();
    }

    var answer = new uint[partials.Length][];
    for (var i = 0; i < answer.Length; i++)
    {
      token.ThrowIfCancellationRequested();

      var item = partials[i];
      answer[i] = await database.EnumeratePartialMatchTagAsync(item, token).ToArrayAsync(token).ConfigureAwait(false);
    }

    return answer;
  }

  public async ValueTask<bool> FilterAsync(Dictionary<uint, uint> dictionary, CancellationToken token)
  {
    if (!hasComplex)
    {
      await complex.InitializeAsync(this, token).ConfigureAwait(false);
      hasComplex = true;
    }

    if (complex.AlwaysFailIntersect)
    {
      return false;
    }

    foreach (var item in complex.IntersectArray)
    {
      if (!dictionary.TryGetValue(item, out var kind) || kind == 0)
      {
        return false;
      }
    }

    foreach (var array in complex.IntersectArrayArray)
    {
      foreach (var item in array)
      {
        if (dictionary.TryGetValue(item, out var kind) && kind != 0)
        {
          goto OK;
        }
      }

      return false;
    OK:;
    }

    if (!complex.AlwaysFailExcept)
    {
      foreach (var item in complex.ExceptArray)
      {
        if (!dictionary.TryGetValue(item, out var kind) || kind == 0)
        {
          goto OK_Except;
        }
      }

      foreach (var array in complex.ExceptArrayArray)
      {
        foreach (var item in array)
        {
          if (dictionary.TryGetValue(item, out var kind) && kind != 0)
          {
            goto NG;
          }
        }

        goto OK_Except;
      NG:;
      }

      return false;
    }

  OK_Except:
    return true;
  }
}
