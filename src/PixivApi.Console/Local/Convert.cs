namespace PixivApi.Console;

public partial class LocalClient
{
    [Command("convert")]
    public async ValueTask ConvertAsync(
        uint stage = 0,
        int mask = 10
    )
    {
        var token = Context.CancellationToken;
        var input = await databaseFactory.RentAsync(token).ConfigureAwait(false);
        var mask2 = (1 << mask) - 1;
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
                                if ((++tagCount & mask2) == 0)
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
                                if ((++toolCount & mask2) == 0)
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
                                if ((++userCount & mask2) == 0)
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
                                var copied = new Artwork();
                                await CopyAsync(item, copied, input, output);
                                await output.AddOrUpdateAsync(item.Id, _ => ValueTask.FromResult(item), static (_, _) => throw new NotImplementedException(), token);
                                if ((++artworkCount & mask2) == 0)
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

    private static async ValueTask CopyAsync(Artwork source, Artwork destination, IDatabase input, IDatabase output)
    {
        destination.Id = source.Id;
        destination.UserId = source.UserId;
        destination.TotalView = source.TotalView;
        destination.TotalBookmarks = source.TotalBookmarks;

        destination.PageCount = source.PageCount;
        destination.Width = source.Width;
        destination.Height = source.Height;

        destination.Type = source.Type;
        destination.Extension = source.Extension;
        destination.ExtraHideReason = source.ExtraHideReason;

        destination.IsOfficiallyRemoved = source.IsOfficiallyRemoved;
        destination.IsXRestricted = source.IsXRestricted;
        destination.IsBookmarked = source.IsBookmarked;
        destination.IsVisible = source.IsVisible;
        destination.IsMuted = source.IsMuted;

        destination.CreateDate = source.CreateDate;
        destination.FileDate = source.FileDate;

        destination.Tags = new uint[source.Tags.Length];
        for (var i = 0; i < source.Tags.Length; i++)
        {
            var item = source.Tags[i];
            var text = await input.GetTagAsync(item, CancellationToken.None).ConfigureAwait(false);
            if (text is null)
            {
                throw new Exception($"{source.Id} of {i}");
            }

            var id = await output.FindTagAsync(text, CancellationToken.None).ConfigureAwait(false);
            if (id == null)
            {
                id = await output.RegisterTagAsync(text, CancellationToken.None).ConfigureAwait(false);
            }

            destination.Tags[i] = id.Value;
        }

        if (source.ExtraFakeTags is { Length: > 0 })
        {
            destination.ExtraFakeTags = new uint[source.ExtraFakeTags.Length];
            for (var i = 0; i < source.ExtraFakeTags.Length; i++)
            {
                var item = source.ExtraFakeTags[i];
                var text = await input.GetTagAsync(item, CancellationToken.None).ConfigureAwait(false);
                if (text is null)
                {
                    throw new Exception($"{source.Id} of {i} of Fake");
                }

                var id = await output.FindTagAsync(text, CancellationToken.None).ConfigureAwait(false);
                if (id == null)
                {
                    id = await output.RegisterTagAsync(text, CancellationToken.None).ConfigureAwait(false);
                }

                destination.ExtraFakeTags[i] = id.Value;
            }
        }

        if (source.ExtraTags is { Length: > 0 })
        {
            destination.ExtraTags = new uint[source.ExtraTags.Length];
            for (var i = 0; i < source.ExtraTags.Length; i++)
            {
                var item = source.ExtraTags[i];
                var text = await input.GetTagAsync(item, CancellationToken.None).ConfigureAwait(false);
                if (text is null)
                {
                    throw new Exception($"{source.Id} of {i} of Extra");
                }

                var id = await output.FindTagAsync(text, CancellationToken.None).ConfigureAwait(false);
                if (id == null)
                {
                    id = await output.RegisterTagAsync(text, CancellationToken.None).ConfigureAwait(false);
                }

                destination.ExtraTags[i] = id.Value;
            }
        }

        destination.Tools = new uint[source.Tools.Length];
        for (var i = 0; i < source.Tools.Length; i++)
        {
            var item = source.Tools[i];
            var text = await input.GetToolAsync(item, CancellationToken.None).ConfigureAwait(false);
            if (text is null)
            {
                throw new Exception($"{source.Id} of {i} of tool");
            }

            var id = await output.FindToolAsync(text, CancellationToken.None).ConfigureAwait(false);
            if (id == null)
            {
                id = await output.RegisterToolAsync(text, CancellationToken.None).ConfigureAwait(false);
            }

            destination.Tools[i] = id.Value;
        }

        destination.Title = source.Title;
        destination.Caption = source.Caption;
        destination.ExtraMemo = source.ExtraMemo;
        destination.ExtraPageHideReasonDictionary = source.ExtraPageHideReasonDictionary;
        destination.UgoiraFrames = source.UgoiraFrames;
    }
}
