using ZoaReference.Features.Terminal.Services;

namespace ZoaReference.Features.Terminal.Commands;

public class OpenCommand : ITerminalCommand
{
    public string Name => "open";
    public string[] Aliases => ["tdls", "strips"];
    public string Summary => "Open external tools in a new tab";
    public string Usage => "tdls     — Open TDLS in a new tab\n" +
                           "    strips   — Open Flight Strips in a new tab\n" +
                           "    open <name>  — Open a tool by name";

    private static readonly Dictionary<string, (string Label, string Url)> Tools = new(StringComparer.OrdinalIgnoreCase)
    {
        ["tdls"]   = ("TDLS",          "https://tdls.virtualnas.net"),
        ["strips"] = ("Flight Strips", "https://strips.virtualnas.net"),
    };

    public Task<CommandResult> ExecuteAsync(CommandArgs args)
    {
        var toolName = args.CommandName;
        if (toolName == "open" && args.Positional.Length > 0)
        {
            toolName = args.Positional[0];
        }

        if (Tools.TryGetValue(toolName, out var tool))
        {
            var text = $"  Opening: {TextFormatter.Colorize(tool.Label, AnsiColor.Green)}";
            return Task.FromResult(CommandResult.FromTab(text, tool.Url));
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
