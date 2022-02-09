namespace PixivApi;

partial class NetworkClient
{
    [Command("unfollow")]
    public async ValueTask<int> UploadUnfollowAsync
    (
        [Option(0, IOUtility.UserIdDescription)] ulong id,
        string? reason = "Dislike"
    )
    {
        var token = Context.CancellationToken;

        bool anyContains = false;
        bool Cancel(string name)
        {            
            anyContains = true;
            logger.LogWarning($"{IOUtility.WarningColor}Are you sure to unfollow {name} - {id}? yes or no{IOUtility.NormalizeColor}");
            return Console.ReadLine() != "yes";
        }

        var artworks = Directory.GetFiles(".", $"*{IOUtility.ArtworkDatabaseFileExtension}");
        foreach (var file in artworks)
        {
            var array = await IOUtility.MessagePackDeserializeAsync<ArtworkDatabaseInfo[]>(file, token).ConfigureAwait(false);
            if (array is not { Length : > 0 })
            {
                continue;
            }

            for (int i = 0; i < array.Length; ++i)
            {
                if (array[i].User.Id != id)
                {
                    continue;
                }

                array[i].User.IsFollowed = false;
                if (!anyContains)
                {
                    if (Cancel(array[i].User.Name))
                    {
                        return 0;
                    }
                }

                await IOUtility.MessagePackSerializeAsync(file, array, FileMode.Create).ConfigureAwait(false);
                break;
            }
        }

        var users = Directory.GetFiles(".", $"*{IOUtility.UserDatabaseFileExtension}");
        foreach (var file in users)
        {
            var array = await IOUtility.MessagePackDeserializeAsync<UserDatabaseInfo[]>(file, token).ConfigureAwait(false);
            if (array is not { Length : > 0 })
            {
                continue;
            }

            for (int i = 0; i < array.Length; ++i)
            {
                if (array[i].User.Id != id)
                {
                    continue;
                }

                array[i].User.IsFollowed = false;
                static void Unfollow([NotNull]ref UserExtraInfo? extraInfo)
                {
                    extraInfo ??= new();
                    extraInfo.HideReason = HideReason.Unfollow;
                }

                Unfollow(ref array[i].ExtraInfo);
                if (!anyContains)
                {
                    if (Cancel(array[i].User.Name))
                    {
                        return 0;
                    }
                }

                await IOUtility.MessagePackSerializeAsync(file, array, FileMode.Create).ConfigureAwait(false);
                break;
            }
        }

        if (anyContains)
        {
            await PostAsync("v1/user/follow/delete", $"user_id={id}").ConfigureAwait(false);
        }

        return 0;
    }
}
