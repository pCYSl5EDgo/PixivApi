using PixivApi.Core.Network;

namespace PixivApi.Core.Local;

public static class TagDatabaseExtensions
{
    public static async ValueTask<uint[]> CalculateTagsAsync(this ITagDatabase database, Tag[]? array, CancellationToken token)
    {
        if (array is not { Length: > 0 })
        {
            return Array.Empty<uint>();
        }

        var answer = new uint[array.Length];
        if (database.CanRegisterParallel)
        {
            await Parallel.ForEachAsync(Enumerable.Range(0, array.Length), token, async (index, token) => answer[index] = await database.RegisterTagAsync(array[index].Name, token).ConfigureAwait(false)).ConfigureAwait(false);
        }
        else
        {
            for (var index = 0; index < answer.Length; index++)
            {
                answer[index] = await database.RegisterTagAsync(array[index].Name, token).ConfigureAwait(false);
            }
        }

        return answer;
    }
}
