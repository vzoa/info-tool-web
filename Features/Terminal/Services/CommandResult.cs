namespace ZoaReference.Features.Terminal.Services;

public enum CommandResultType
{
    Text,
    OpenUrl,
    OpenTab,
    Error,
    CloseViewer
}

public record CommandResult(string Text, CommandResultType Type = CommandResultType.Text, string? Url = null)
{
    /// <summary>
    /// Numbered selection callbacks for disambiguation lists.
    /// When set, the dispatcher registers these so a bare number input triggers the callback.
    /// </summary>
    public Dictionary<int, Func<Task<CommandResult>>>? PendingSelections { get; init; }

    public static CommandResult FromText(string text) => new(text);

    public static CommandResult FromError(string message) =>
        new(TextFormatter.Colorize(message, AnsiColor.Red), CommandResultType.Error);

    public static CommandResult FromUrl(string text, string url) =>
        new(text, CommandResultType.OpenUrl, url);

    public static CommandResult FromTab(string text, string url) =>
        new(text, CommandResultType.OpenTab, url);
}
