using System.Text;

namespace ZoaReference.Features.Terminal.Services;

public enum AnsiColor
{
    Orange,
    White,
    Red,
    Cyan,
    Green,
    Yellow,
    Gray
}

public static class TextFormatter
{
    private static string ColorCode(AnsiColor color) => color switch
    {
        AnsiColor.Orange => "\x1b[38;5;208m",
        AnsiColor.White => "\x1b[37m",
        AnsiColor.Red => "\x1b[31m",
        AnsiColor.Cyan => "\x1b[36m",
        AnsiColor.Green => "\x1b[32m",
        AnsiColor.Yellow => "\x1b[33m",
        AnsiColor.Gray => "\x1b[90m",
        _ => ""
    };

    private const string Reset = "\x1b[0m";

    public static string Colorize(string text, AnsiColor color) =>
        $"{ColorCode(color)}{text}{Reset}";

    public static string FormatTableHeader(string title, string[] columns, int[] widths)
    {
        var sb = new StringBuilder();
        var totalWidth = 80;

        sb.AppendLine(Colorize(new string('=', totalWidth), AnsiColor.Orange));
        sb.AppendLine(Colorize($"  {title}", AnsiColor.Orange));
        sb.AppendLine(Colorize(new string('=', totalWidth), AnsiColor.Orange));

        var headerRow = new StringBuilder("  ");
        for (var i = 0; i < columns.Length; i++)
        {
            var col = columns[i];
            var width = i < widths.Length ? widths[i] : 12;
            headerRow.Append(col.PadRight(width));
        }
        sb.AppendLine(Colorize(headerRow.ToString(), AnsiColor.Yellow));
        sb.AppendLine(Colorize(new string('-', totalWidth), AnsiColor.Gray));

        return sb.ToString();
    }

    public static string FormatTableRow(string[] values, int[] widths)
    {
        var sb = new StringBuilder("  ");
        for (var i = 0; i < values.Length; i++)
        {
            var val = values[i] ?? "";
            var width = i < widths.Length ? widths[i] : 12;
            sb.Append(val.PadRight(width));
        }
        return sb.ToString();
    }

    public static string FormatTableEmpty(string title, string message)
    {
        var sb = new StringBuilder();
        sb.AppendLine(Colorize(new string('=', 80), AnsiColor.Orange));
        sb.AppendLine(Colorize($"  {title}", AnsiColor.Orange));
        sb.AppendLine(Colorize(new string('=', 80), AnsiColor.Orange));
        sb.AppendLine();
        sb.AppendLine($"  {Colorize(message, AnsiColor.Gray)}");
        return sb.ToString();
    }

    public static string FormatUrl(string url) =>
        $"{ColorCode(AnsiColor.Cyan)}\x1b[4m{url}\x1b[24m{Reset}";
}
