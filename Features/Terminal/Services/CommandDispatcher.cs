using System.Diagnostics;

namespace ZoaReference.Features.Terminal.Services;

public class CommandDispatcher
{
    private readonly Dictionary<string, ITerminalCommand> _commands = new(StringComparer.OrdinalIgnoreCase);
    private readonly List<ITerminalCommand> _allCommands;
    private readonly Dictionary<int, Func<Task<CommandResult>>> _pendingSelections = new();
    private readonly ILogger<CommandDispatcher> _logger;

    public CommandDispatcher(IEnumerable<ITerminalCommand> commands, ILogger<CommandDispatcher> logger)
    {
        _logger = logger;
        _allCommands = commands.ToList();
        foreach (var cmd in _allCommands)
        {
            _commands[cmd.Name] = cmd;
            foreach (var alias in cmd.Aliases)
            {
                _commands[alias] = cmd;
            }
        }
    }

    public IReadOnlyList<ITerminalCommand> AllCommands => _allCommands;

    public void RegisterPendingSelections(Dictionary<int, Func<Task<CommandResult>>> selections)
    {
        _pendingSelections.Clear();
        foreach (var (key, value) in selections)
        {
            _pendingSelections[key] = value;
        }
    }

    public void ClearPendingSelections() => _pendingSelections.Clear();

    public async Task<CommandResult> DispatchAsync(string rawInput)
    {
        var trimmed = rawInput.Trim();
        if (string.IsNullOrEmpty(trimmed))
        {
            return CommandResult.FromText("");
        }

        // Check for pending number selection
        if (int.TryParse(trimmed, out var selection) && _pendingSelections.TryGetValue(selection, out var callback))
        {
            _pendingSelections.Clear();
            return await ExecuteWithTimingAsync(callback);
        }

        // Clear pending selections on any non-number input
        _pendingSelections.Clear();

        var args = CommandArgs.Parse(trimmed);

        if (!_commands.TryGetValue(args.CommandName, out var command))
        {
            return CommandResult.FromError($"Unknown command: '{args.CommandName}'. Type 'help' for available commands.");
        }

        var result = await ExecuteWithTimingAsync(() => command.ExecuteAsync(args));

        // Register any pending selections from the result
        if (result.PendingSelections is { Count: > 0 })
        {
            RegisterPendingSelections(result.PendingSelections);
        }

        return result;
    }

    public IEnumerable<string> GetCompletions(string partial)
    {
        var trimmed = partial.TrimStart();
        var spaceIndex = trimmed.IndexOf(' ');

        if (spaceIndex < 0)
        {
            // Completing command name
            return _allCommands
                .SelectMany(c => new[] { c.Name }.Concat(c.Aliases))
                .Where(n => n.StartsWith(trimmed, StringComparison.OrdinalIgnoreCase))
                .Order();
        }

        // Completing command arguments
        var cmdName = trimmed[..spaceIndex];
        var argPart = trimmed[(spaceIndex + 1)..].TrimStart();
        var argIndex = argPart.Split(' ', StringSplitOptions.RemoveEmptyEntries).Length;

        if (_commands.TryGetValue(cmdName, out var command))
        {
            return command.GetCompletions(argPart, argIndex);
        }

        return [];
    }

    private async Task<CommandResult> ExecuteWithTimingAsync(Func<Task<CommandResult>> action)
    {
        var sw = Stopwatch.StartNew();
        try
        {
            var result = await action();
            sw.Stop();
            var timing = TextFormatter.Colorize($"({sw.ElapsedMilliseconds}ms)", AnsiColor.Gray);
            return result with { Text = $"{result.Text}\r\n{timing}" };
        }
        catch (Exception ex)
        {
            sw.Stop();
            _logger.LogError(ex, "Command execution failed");
            return CommandResult.FromError($"Command failed: {ex.Message}");
        }
    }
}
