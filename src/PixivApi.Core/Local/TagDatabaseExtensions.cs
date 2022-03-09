using PixivApi.Core.Network;

namespace PixivApi.Core.Local;

public static class TagDatabaseExtensions
{
    public static async ValueTask<uint[]> CalculateTagsAsync(this ITagDatabase database, string[]? array, CancellationToken token)
    {
        if (array is not { Length: > 0 })
        {
            return Array.Empty<uint>();
        }

        var answer = new uint[array.Length];
        await Parallel.ForEachAsync(Enumerable.Range(0, array.Length), token, async (index, token) => answer[index] = await database.RegisterTagAsync(array[index], token).ConfigureAwait(false)).ConfigureAwait(false);
        return answer;
    }

    public static async ValueTask<uint[]> CalculateTagsAsync(this ITagDatabase database, Tag[]? array, CancellationToken token)
    {
        if (array is not { Length: > 0 })
        {
            return Array.Empty<uint>();
        }

        var answer = new uint[array.Length];
        await Parallel.ForEachAsync(Enumerable.Range(0, array.Length), token, async (index, token) => answer[index] = await database.RegisterTagAsync(array[index].Name, token).ConfigureAwait(false)).ConfigureAwait(false);
        return answer;
    }
}
