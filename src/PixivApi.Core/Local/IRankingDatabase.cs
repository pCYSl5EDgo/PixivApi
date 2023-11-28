namespace PixivApi.Core.Local;

public interface IRankingDatabase
{
  ValueTask<ulong> CountRankingAsync(CancellationToken token);

  ValueTask<ulong[]?> GetRankingAsync(DateOnly date, RankingKind kind, CancellationToken token);

  ValueTask AddOrUpdateRankingAsync(DateOnly date, RankingKind kind, ulong[] values, CancellationToken token);
}
