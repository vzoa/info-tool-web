namespace ZoaReference.Features.Terminal.Services;

public interface ITerminalCommand
{
    string Name { get; }
    string[] Aliases { get; }
    string Summary { get; }
    string Usage { get; }
    Task<CommandResult> ExecuteAsync(CommandArgs args);
    IEnumerable<string> GetCompletions(string partial, int argIndex);
}
