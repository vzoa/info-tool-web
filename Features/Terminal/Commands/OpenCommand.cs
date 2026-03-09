using ZoaReference.Features.Terminal.Services;

namespace ZoaReference.Features.Terminal.Commands;

public class OpenCommand : ITerminalCommand
{
    public string Name => "open";
    public string[] Aliases => ["vis", "tdls", "strips"];
    public string Summary => "Open external tools (vis, tdls, strips) in viewer";
    public string Usage => "vis      — Open Airspace Visualizer\n" +
                           "    tdls     — Open TDLS\n" +
                           "    strips   — Open Flight Strips\n" +
                           "    open <name>  — Open a tool by name";

    private static readonly Dictionary<string, (string Label, string Path)> Tools = new(StringComparer.OrdinalIgnoreCase)
    {
        ["vis"] = ("Airspace Visualizer", "/airspaceviz"),
        ["tdls"] = ("TDLS", "/tdls"),
        ["strips"] = ("Flight Strips", "/strips"),
    };

    public Task<CommandResult> ExecuteAsync(CommandArgs args)
    {
        // If invoked via alias (vis, tdls, strips), use the alias as the tool name
        var toolName = args.CommandName;
        if (toolName == "open" && args.Positional.Length > 0)
        {
            toolName = args.Positional[0];
        }

        if (Tools.TryGetValue(toolName, out var tool))
        {
            var text = $"  Opening: {TextFormatter.Colorize(tool.Label, AnsiColor.Green)}";
            return Task.FromResult(CommandResult.FromUrl(text, tool.Path));
        }

        return Task.FromResult(CommandResult.FromError(
            $"Unknown tool: '{toolName}'. Available: {string.Join(", ", Tools.Keys)}"));
    }

    public IEnumerable<string> GetCompletions(string partial, int argIndex)
    {
        if (argIndex <= 1)
        {
            return Tools.Keys
                .Where(k => k.StartsWith(partial, StringComparison.OrdinalIgnoreCase));
        }
        return [];
    }
}
