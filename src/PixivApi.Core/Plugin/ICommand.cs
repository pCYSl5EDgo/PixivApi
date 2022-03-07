using Microsoft.Extensions.Logging;

namespace PixivApi.Core.Plugin;

public interface ICommand : IPlugin
{
    string GetHelp();

    ValueTask ExecuteAsync(IEnumerable<string> commandLineArguments, CommandArgument argument, CancellationToken token);
}

public record struct CommandArgument(HttpClient Client, ILogger Logger, FinderFacade Finder)
{
}
