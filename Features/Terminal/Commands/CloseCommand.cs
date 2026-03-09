using ZoaReference.Features.Terminal.Services;

namespace ZoaReference.Features.Terminal.Commands;

public class CloseCommand : ITerminalCommand
{
    public string Name => "close";
    public string[] Aliases => [];
    public string Summary => "Close the viewer panel";
    public string Usage => "close  — Hide the right-side viewer panel";

    public Task<CommandResult> ExecuteAsync(CommandArgs args)
    {
        return Task.FromResult(new CommandResult(
            TextFormatter.Colorize("  Viewer closed.", AnsiColor.Gray),
            CommandResultType.CloseViewer));
    }

    public IEnumerable<string> GetCompletions(string partial, int argIndex) => [];
}
