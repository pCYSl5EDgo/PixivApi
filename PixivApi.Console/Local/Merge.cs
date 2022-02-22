using PixivApi.Core;
using PixivApi.Core.Local;

namespace PixivApi.Console;

public partial class LocalClient
{
    [Command("merge")]
    public async ValueTask MergeAsync(
        [Option(0, ArgumentDescriptions.DatabaseDescription)] string outputPath,
        [Option(1, ArgumentDescriptions.DatabaseDescription)] string inputPath
    )
    {
        var token = Context.CancellationToken;
        var outputDatabase = await IOUtility.MessagePackDeserializeAsync<DatabaseFile>(outputPath, token).ConfigureAwait(false);
        var inputDatabase = await IOUtility.MessagePackDeserializeAsync<DatabaseFile>(inputPath, token).ConfigureAwait(false);
        if (outputDatabase is null || inputDatabase is null)
        {
            return;
        }

        var oldOutputLength = outputDatabase.Artworks.Length;
        if (inputDatabase.Artworks.Length == 0)
        {
            goto END;
        }

        if (oldOutputLength == 0)
        {
            File.Copy(inputPath, outputPath, true);
            goto END;
        }

        Dictionary<uint, uint> tagDictionary = new();
        Dictionary<uint, uint> toolDictionary = new();
        var threeTasks = new Task[3];
        threeTasks[0] = Task.Run(() =>
        {
            foreach (var user in inputDatabase.UserDictionary.Values)
            {
                token.ThrowIfCancellationRequested();
                _ = outputDatabase.UserDictionary.AddOrUpdate(user.Id, user, (_, _) => user);
            }
        }, token);
        threeTasks[1] = Task.Run(() =>
        {
            foreach (var (text, number) in inputDatabase.TagSet.Reverses)
            {
                token.ThrowIfCancellationRequested();
                var to = outputDatabase.TagSet.Register(text);
                tagDictionary.Add(number, to);
            }
        }, token);
        threeTasks[2] = Task.Run(() =>
        {
            foreach (var (text, number) in inputDatabase.ToolSet.Reverses)
            {
                token.ThrowIfCancellationRequested();
                var to = outputDatabase.ToolSet.Register(text);
                toolDictionary.Add(number, to);
            }
        }, token);
        await Task.WhenAll(threeTasks).ConfigureAwait(false);
        token.ThrowIfCancellationRequested();

        await Parallel.ForEachAsync(inputDatabase.Artworks, token, (artwork, token) =>
        {
            void Replace(ref uint tag)
            {
                if (tagDictionary.TryGetValue(tag, out var toTag))
                {
                    tag = toTag;
                }
            }

            token.ThrowIfCancellationRequested();
            foreach (ref var tag in artwork.Tags.AsSpan())
            {
                Replace(ref tag);
            }

            token.ThrowIfCancellationRequested();
            foreach (ref var tag in artwork.ExtraTags.AsSpan())
            {
                Replace(ref tag);
            }

            token.ThrowIfCancellationRequested();
            foreach (ref var tag in artwork.ExtraFakeTags.AsSpan())
            {
                Replace(ref tag);
            }

            token.ThrowIfCancellationRequested();
            foreach (ref var tool in artwork.Tools.AsSpan())
            {
                if (toolDictionary.TryGetValue(tool, out var toTool))
                {
                    tool = toTool;
                }
            }

            return ValueTask.CompletedTask;
        }).ConfigureAwait(false);

        var artowrkDictionary = inputDatabase.Artworks.ToDictionary(artwork => artwork.Id);
        foreach (var artwork in outputDatabase.Artworks)
        {
            token.ThrowIfCancellationRequested();
            _ = artowrkDictionary.TryAdd(artwork.Id, artwork);
        }

        outputDatabase.Artworks = artowrkDictionary.Count != 0 ? artowrkDictionary.Values.ToArray() : Array.Empty<Artwork>();
        await IOUtility.MessagePackSerializeAsync(outputPath, outputDatabase, FileMode.CreateNew).ConfigureAwait(false);

    END:
        logger.LogInformation($"Output: {oldOutputLength} Input: {inputDatabase.Artworks.Length} New: {outputDatabase.Artworks.Length}");
    }
}
