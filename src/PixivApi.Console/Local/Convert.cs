namespace PixivApi.Console;

public partial class LocalClient
{
    [Command("convert")]
    public async ValueTask ConvertAsync(
        uint stage = 0
    )
    {
        var token = Context.CancellationToken;
        var input = await databaseFactory.RentAsync(token).ConfigureAwait(false);
        try
        {
            var sqlFactory = Context.ServiceProvider.GetRequiredService<Core.SqliteDatabase.DatabaseFactory>();
            var output = await sqlFactory.RentAsync(token).ConfigureAwait(false);
            try
            {
                for (var stageIndex = stage; stageIndex < 4; stageIndex++)
                {
                    switch (stageIndex)
                    {
                        case 0:
                            logger.LogInformation("Start register tags.");
                            var tagCount = 0;
                            await foreach (var (tag, _) in input.EnumerateTagAsync(token))
                            {
                                await output.RegisterTagAsync(tag, token).ConfigureAwait(false);
                                if ((++tagCount & 1023) == 0)
                                {
                                    System.Console.Write($"{VirtualCodes.DeleteLine1}Count: {tagCount}");
                                }
                            }
                            break;
                        case 1:
                            logger.LogInformation("Start register tools.");
                            var toolCount = 0;
                            await foreach (var (tool, _) in input.EnumerateToolAsync(token))
                            {
                                await output.RegisterToolAsync(tool, token).ConfigureAwait(false);
                                if ((++toolCount & 1023) == 0)
                                {
                                    System.Console.Write($"{VirtualCodes.DeleteLine1}Count: {toolCount}");
                                }
                            }
                            break;
                        case 2:
                            logger.LogInformation("Start register users.");
                            var userCount = 0;
                            await foreach (var item in input.EnumerateUserAsync(token))
                            {
                                await output.AddOrUpdateAsync(item.Id, _ => ValueTask.FromResult(item), static (_, _) => throw new NotImplementedException(), token);
                                if ((++userCount & 1023) == 0)
                                {
                                    System.Console.Write($"{VirtualCodes.DeleteLine1}Count: {userCount}");
                                }
                            }
                            break;
                        case 3:
                            var filterPath = configSettings.ArtworkFilterFilePath;
                            var filter = string.IsNullOrWhiteSpace(filterPath) ? null : await filterFactory.CreateAsync(input, new(filterPath), token).ConfigureAwait(false);
                            logger.LogInformation("Start register artworks.");
                            var artworkCount = 0;
                            await foreach (var item in filter is null ? input.EnumerateArtworkAsync(token) : input.FilterAsync(filter, token))
                            {
                                await output.AddOrUpdateAsync(item.Id, _ => ValueTask.FromResult(item), static (_, _) => throw new NotImplementedException(), token);
                                if ((++artworkCount & 1023) == 0)
                                {
                                    System.Console.Write($"{VirtualCodes.DeleteLine1}Count: {artworkCount}");
                                }
                            }

                            break;
                        default:
                            throw new InvalidOperationException();
                    }
                }
            }
            finally
            {
                sqlFactory.Return(ref output);
            }
        }
        finally
        {
            databaseFactory.Return(ref input);
        }
    }
}
