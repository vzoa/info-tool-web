using System.Text;
using ZoaReference.Features.Terminal.Services;

namespace ZoaReference.Features.Terminal.Commands;

public class HelpCommand(IServiceProvider serviceProvider) : ITerminalCommand
{
    public string Name => "help";
    public string[] Aliases => ["?"];
    public string Summary => "List available commands";
    public string Usage => "help [command]  — Show help for a specific command, or list all commands";

    private List<ITerminalCommand>? _commands;

    private List<ITerminalCommand> Commands =>
        _commands ??= serviceProvider.GetServices<ITerminalCommand>().ToList();

    public Task<CommandResult> ExecuteAsync(CommandArgs args)
    {
        if (args.Positional.Length > 0)
        {
            return Task.FromResult(ShowCommandHelp(args.Positional[0]));
        }

        return Task.FromResult(ShowAllCommands());
    }

    private CommandResult ShowAllCommands()
    {
        var sb = new StringBuilder();
        var widths = new[] { 20, 60 };
        sb.Append(TextFormatter.FormatTableHeader("Available Commands", ["Command", "Description"], widths));

        foreach (var cmd in Commands.OrderBy(c => c.Name))
        {
            var nameWithAliases = cmd.Aliases.Length > 0
                ? $"{cmd.Name} ({string.Join(", ", cmd.Aliases)})"
                : cmd.Name;
            sb.AppendLine(TextFormatter.FormatTableRow([nameWithAliases, cmd.Summary], widths));
        }

        sb.AppendLine();
        sb.AppendLine($"  Type {TextFormatter.Colorize("help <command>", AnsiColor.Cyan)} for detailed usage.");

        return CommandResult.FromText(sb.ToString());
    }

    private CommandResult ShowCommandHelp(string commandName)
    {
        var cmd = Commands.FirstOrDefault(c =>
            c.Name.Equals(commandName, StringComparison.OrdinalIgnoreCase) ||
            c.Aliases.Any(a => a.Equals(commandName, StringComparison.OrdinalIgnoreCase)));

        if (cmd is null)
        {
            return CommandResult.FromError($"Unknown command: '{commandName}'");
        }

        var sb = new StringBuilder();
        sb.AppendLine(TextFormatter.Colorize($"  {cmd.Name}", AnsiColor.Orange));
        if (cmd.Aliases.Length > 0)
        {
            sb.AppendLine($"  Aliases: {TextFormatter.Colorize(string.Join(", ", cmd.Aliases), AnsiColor.Cyan)}");
        }
        sb.AppendLine();
        sb.AppendLine($"  {cmd.Summary}");
        sb.AppendLine();
        sb.AppendLine(TextFormatter.Colorize("  Usage:", AnsiColor.Yellow));
        sb.AppendLine($"    {cmd.Usage}");

        return CommandResult.FromText(sb.ToString());
    }

    public IEnumerable<string> GetCompletions(string partial, int argIndex)
    {
        if (argIndex <= 1)
        {
            return Commands
                .Select(c => c.Name)
                .Where(n => n.StartsWith(partial, StringComparison.OrdinalIgnoreCase));
        }
        return [];
    }
}
