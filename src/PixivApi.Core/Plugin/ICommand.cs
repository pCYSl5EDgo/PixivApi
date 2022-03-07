using Microsoft.Extensions.Logging;

namespace PixivApi.Core.Plugin;

public interface ICommand : IPlugin
{
    string GetHelp();

    ValueTask ExecuteAsync(IEnumerable<string> commandLineArguments, CommandArgument argument, CancellationToken token);
}

public readonly record struct CommandArgument(ILogger Logger, IServiceProvider ServiceProvider)
{
}
