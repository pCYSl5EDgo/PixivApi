using Microsoft.Extensions.DependencyInjection;
using PixivApi.Core;
using PixivApi.Core.Local;
using PixivApi.Core.Network;
using PixivApi.Core.Plugin;

namespace CollectAllUserInfoPlugin;

public sealed class Collector : ICommand
{
    private readonly ConfigSettings configSettings;
    private readonly string path;

    public Collector(ConfigSettings configSettings, string path)
    {
        this.configSettings = configSettings;
        this.path = path;
    }

    public static Task<IPlugin?> CreateAsync(string dllPath, ConfigSettings configSettings, CancellationToken cancellationToken)
    {
        var path = configSettings.DatabaseFilePath;
        return Task.FromResult<IPlugin?>(string.IsNullOrWhiteSpace(path) ? null : new Collector(configSettings, path));
    }

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;

    public async ValueTask ExecuteAsync(IEnumerable<string> commandLineArguments, CommandArgument argument, CancellationToken token)
    {
        if (token.IsCancellationRequested)
        {
            return;
        }

        var databaseTask = IOUtility.MessagePackDeserializeAsync<DatabaseFile>(path, token);

        var provider = argument.ServiceProvider;
        var client = provider.GetRequiredService<HttpClient>();
        client.AddToDefaultHeader(configSettings);

        using var holder = new AuthenticationHeaderValueHolder(configSettings, client, configSettings.ReconnectLoopIntervalTimeSpan);
        _ = await holder.ConnectAsync(token).ConfigureAwait(false);

        var database = await databaseTask.ConfigureAwait(false);
        if (database is null)
        {
            return;
        }

        var logger = argument.Logger;
        var any = false;
        try
        {
            foreach (var user in database.UserDictionary.Values)
            {
                if (token.IsCancellationRequested)
                {
                    return;
                }

                await UpdateDetailAsync(holder, user, token).ConfigureAwait(false);
                any = true;
                if (token.IsCancellationRequested)
                {
                    return;
                }

                if (!user.IsOfficiallyRemoved)
                {
                    await CollectArtworksAsync(holder, logger, user.Id, database, token).ConfigureAwait(false);
                }
            }
        }
        finally
        {
            if (any)
            {
                await IOUtility.MessagePackSerializeAsync(path, database, FileMode.Create).ConfigureAwait(false);
            }
        }
    }

    private const string ApiHost = "app-api.pixiv.net";

    private async ValueTask UpdateDetailAsync(AuthenticationHeaderValueHolder holder, User user, CancellationToken token)
    {
        if (token.IsCancellationRequested)
        {
            return;
        }

        var url = $"https://{ApiHost}/v1/user/detail?user_id={user.Id}";
        using HttpRequestMessage request = new(HttpMethod.Get, url);
        var authentication = await holder.GetAsync(token).ConfigureAwait(false);
        request.Headers.Authorization = authentication;
        if (!request.TryAddToHeader(configSettings.HashSecret, ApiHost))
        {
            throw new InvalidOperationException();
        }

        using var response = await holder.HttpClient.SendAsync(request, token).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();
        var bytes = await response.Content.ReadAsByteArrayAsync(CancellationToken.None).ConfigureAwait(false);
        var data = IOUtility.JsonDeserialize<UserDetailResponseData>(bytes);
        LocalNetworkConverter.Overwrite(user, data);
    }

    private async ValueTask CollectArtworksAsync(AuthenticationHeaderValueHolder holder, Microsoft.Extensions.Logging.ILogger logger, ulong id, DatabaseFile database, CancellationToken token)
    {
        if (token.IsCancellationRequested)
        {
            return;
        }

        var url = $"https://{ApiHost}/v1/user/illusts?user_id={id}";
        await ValueTask.CompletedTask;
    }

    public string GetHelp() => "Collect all of the artworks of every user.";
}
