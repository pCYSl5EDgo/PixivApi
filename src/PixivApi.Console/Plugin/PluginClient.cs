﻿using System.Reflection;

namespace PixivApi.Console;

[Command("plugin")]
public class PluginClient : ConsoleAppBase
{
    private async ValueTask<ICommand?> PrepareCommandAsync(string dllPath, string commandName, CancellationToken token)
    {
        if (token.IsCancellationRequested || !File.Exists(dllPath))
        {
            return null;
        }

        Assembly assembly;
        try
        {
            assembly = Assembly.LoadFile(dllPath);
        }
        catch (Exception e)
        {
            Context.Logger.LogError(e, $"Failed to load assembly. Path: {dllPath}");
            return null;
        }

        Type? type = null;
        foreach (var module in assembly.Modules)
        {
            if (token.IsCancellationRequested)
            {
                return null;
            }

            var types = module.FindTypes(static (type, name) => type.IsClass && type.IsAssignableTo(typeof(ICommand)) && type.Name.Equals(name as string, StringComparison.Ordinal), commandName);
            if (types.Length > 0)
            {
                type = types[0];
                break;
            }
        }

        if (token.IsCancellationRequested || type is null)
        {
            return null;
        }

        var configSettings = Context.ServiceProvider.GetRequiredService<ConfigSettings>();
        if ((type.GetMethod(nameof(IPlugin.CreateAsync), BindingFlags.Static | BindingFlags.Public)?.Invoke(null, new object[] { dllPath, configSettings, token })) is not Task<IPlugin?> task)
        {
            return null;
        }

        return await task.ConfigureAwait(false) as ICommand;
    }

    [Command("help")]
    public async ValueTask Help(
        [Option(0)] string dllPath,
        [Option(1)] string command
    )
    {
        await using var commandObject = await PrepareCommandAsync(dllPath, command, Context.CancellationToken).ConfigureAwait(false);
        if (commandObject is null)
        {
            return;
        }

        Context.Logger.LogInformation(commandObject.GetHelp());
    }

    [Command("execute")]
    public async ValueTask ExecuteAsync(
        [Option(0)] string dllPath,
        [Option(1)] string command
    )
    {
        await using var commandObject = await PrepareCommandAsync(dllPath, command, Context.CancellationToken).ConfigureAwait(false);
        if (commandObject is null)
        {
            return;
        }

        var args = Environment.GetCommandLineArgs();
        var index = 2;
        for (; index < args.Length; index++)
        {
            if (args[index] == "--")
            {
                index++;
                break;
            }
        }

        await commandObject.ExecuteAsync(args.Skip(index), new(Context.Logger, Context.ServiceProvider), Context.CancellationToken).ConfigureAwait(false);
    }
}
