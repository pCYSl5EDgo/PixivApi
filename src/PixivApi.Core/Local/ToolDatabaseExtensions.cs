namespace PixivApi.Core.Local;

public static class ToolDatabaseExtensions
{
    public static async ValueTask<uint[]> CalculateToolsAsync(this IToolDatabase database, string[]? array, CancellationToken token)
    {
        if (array is not { Length: > 0 })
        {
            return Array.Empty<uint>();
        }

        var answer = new uint[array.Length];
        await Parallel.ForEachAsync(Enumerable.Range(0, array.Length), token, async (index, token) => answer[index] = await database.RegisterToolAsync(array[index], token).ConfigureAwait(false)).ConfigureAwait(false);
        return answer;
    }
}
