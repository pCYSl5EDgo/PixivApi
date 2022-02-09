namespace PixivApi;

public interface IAsyncInitailizable
{
    ValueTask InitializeAsync(string? directory, CancellationToken token);
}
