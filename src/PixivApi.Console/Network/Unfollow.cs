namespace PixivApi.Console;

public partial class NetworkClient
{
    [Command("follow")]
    public async ValueTask FollowUserAsync(
        [Option("f")] bool follow = true,
        [Option("p")] bool isPrivate = false,
        string? tag = null
    )
    {
        const string MediaType = "application/x-www-form-urlencoded";
        var encoding = new System.Text.UTF8Encoding(false);
        var token = Context.CancellationToken;
        
        var logger = Context.Logger;
        var logTrace = logger.IsEnabled(LogLevel.Trace);

        IDatabase? database = null;
        uint tagId = 0;
        if (!string.IsNullOrWhiteSpace(tag))
        {
            database = await databaseFactory.RentAsync(token).ConfigureAwait(false);
            tagId = await database.RegisterTagAsync(tag, token).ConfigureAwait(false);
            logger.LogInformation($"Tag: {tag} TagId: {tagId}");
        }

        var requestSender = Context.ServiceProvider.GetRequiredService<RequestSender>();
        try
        {
            do
            {
                System.Console.Error.Write("Input User Id:");
                var line = System.Console.ReadLine();
                if (string.IsNullOrWhiteSpace(line))
                {
                    break;
                }

                if (!ulong.TryParse(line.AsSpan().Trim(), out var id))
                {
                    Context.Logger.LogError($"Errornous Input: {line}");
                    continue;
                }

                UserDetailResponseData userDetail;
                do
                {
                    token.ThrowIfCancellationRequested();
                    using var response = await GetUserDetailAsync(requestSender, id, token).ConfigureAwait(false);
                    if (response.IsSuccessStatusCode)
                    {
                        var array = await response.Content.ReadAsByteArrayAsync(token).ConfigureAwait(false);
                        userDetail = IOUtility.JsonDeserialize<UserDetailResponseData>(array);
                        break;
                    }

                    response.EnsureSuccessStatusCode();
                } while (true);

                var isFollowed = userDetail.User.IsFollowed;
                {
                    var authentication = await holder.GetAsync(token).ConfigureAwait(false);
                    var url = follow ? $"https://{ApiHost}/v1/user/follow/add" : $"https://{ApiHost}/v1/user/follow/delete";
                    using var request = new HttpRequestMessage(HttpMethod.Post, url);
                    request.Headers.Authorization = authentication;
                    if (!request.TryAddToHeader(configSettings.HashSecret, ApiHost))
                    {
                        throw new InvalidOperationException();
                    }

                    request.Content = new StringContent(follow ?
                        (isPrivate ?
                            $"get_secure_url=1&user_id={id}&restrict=private" :
                            $"get_secure_url=1&user_id={id}&restrict=public") :
                        $"get_secure_url=1&user_id={id}",
                        encoding, MediaType);
                    using var response = await client.SendAsync(request, token).ConfigureAwait(false);
                    if (logTrace)
                    {
                        logger.LogTrace(await response.Content.ReadAsStringAsync(token).ConfigureAwait(false));
                    }

                    if (!response.IsSuccessStatusCode)
                    {
                        logger.LogError(response.ReasonPhrase);
                        continue;
                    }

                    userDetail.User.IsFollowed = follow;
                }

                if (logTrace)
                {
                    logger.LogTrace("Register to the database.");
                }

                database ??= await databaseFactory.RentAsync(token).ConfigureAwait(false);
                if (database is IExtenededDatabase exteneded)
                {
                    await exteneded.UserAddOrUpdateAsync(userDetail, token).ConfigureAwait(false);
                    if (tagId != 0)
                    {
                        await exteneded.AddTagToUser(id, tagId, token).ConfigureAwait(false);
                    }
                }
                else
                {
                    await database.AddOrUpdateAsync(id,
                        token =>
                        {
                            token.ThrowIfCancellationRequested();
                            var user = userDetail.Convert();
                            if (tagId != 0)
                            {
                                if (user.ExtraTags is { Length: > 0 })
                                {
                                    var index = Array.IndexOf(user.ExtraTags, tagId);
                                    if (index == -1)
                                    {
                                        Array.Resize(ref user.ExtraTags, user.ExtraTags.Length);
                                        user.ExtraTags[^1] = tagId;
                                    }
                                }
                                else
                                {
                                    user.ExtraTags = [tagId];
                                }
                            }

                            return ValueTask.FromResult(user);
                        },
                        (user, token) =>
                        {
                            token.ThrowIfCancellationRequested();
                            user.Overwrite(userDetail);
                            if (tagId != 0)
                            {
                                if (user.ExtraTags is { Length: > 0 })
                                {
                                    var index = Array.IndexOf(user.ExtraTags, tagId);
                                    if (index == -1)
                                    {
                                        Array.Resize(ref user.ExtraTags, user.ExtraTags.Length);
                                        user.ExtraTags[^1] = tagId;
                                    }
                                }
                                else
                                {
                                    user.ExtraTags = [tagId];
                                }
                            }

                            return ValueTask.CompletedTask;
                        },
                        token).ConfigureAwait(false);
                }
            } while (!token.IsCancellationRequested);
        }
        finally
        {
            if (database is not null)
            {
                databaseFactory.Return(ref database);
            }
        }
    }
}
