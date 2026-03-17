using ZoaReference.Features.Terminal.Services;

namespace ZoaReference.Features.Terminal.Commands;

public class ClearCommand : ITerminalCommand
{
    public string Name => "clear";
    public string[] Aliases => ["cls"];
    public string Summary => "Clear the terminal screen";
    public string Usage => "clear  — Clear the terminal (also: Ctrl+L)";

    public Task<CommandResult> ExecuteAsync(CommandArgs args)
    {
        // Return special escape sequence to clear screen
        return Task.FromResult(CommandResult.FromText("\x1b[2J\x1b[H"));
    }

    public IEnumerable<string> GetCompletions(string partial, int argIndex) => [];
}
