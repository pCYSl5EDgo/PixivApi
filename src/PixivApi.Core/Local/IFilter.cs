namespace PixivApi.Core.Local;

public interface IFilter<T>
{
  bool HasSlowFilter { get; }

  bool FastFilter(T value);

  ValueTask<bool> SlowFilter(T value, CancellationToken token);
}
