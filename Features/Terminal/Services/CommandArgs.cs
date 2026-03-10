namespace ZoaReference.Features.Terminal.Services;

public record CommandArgs(
    string RawInput,
    string CommandName,
    string[] Positional,
    Dictionary<string, string?> Flags)
{
    private const int MaxInputLength = 500;
    private const int MaxTokenLength = 100;

    public static CommandArgs Parse(string rawInput)
    {
        var trimmed = rawInput.Trim();
        if (string.IsNullOrEmpty(trimmed))
        {
            return new CommandArgs(rawInput, "", [], new Dictionary<string, string?>());
        }

        if (trimmed.Length > MaxInputLength)
        {
            trimmed = trimmed[..MaxInputLength];
        }

        var tokens = Tokenize(trimmed).Select(t => t.Length > MaxTokenLength ? t[..MaxTokenLength] : t).ToList();
        var commandName = tokens.Count > 0 ? tokens[0].ToLowerInvariant() : "";
        var positional = new List<string>();
        var flags = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);

        for (var i = 1; i < tokens.Count; i++)
        {
            var token = tokens[i];
            if (token.StartsWith("--"))
            {
                var flagName = token[2..];
                if (i + 1 < tokens.Count && !tokens[i + 1].StartsWith('-'))
                {
                    flags[flagName] = tokens[i + 1];
                    i++;
                }
                else
                {
                    flags[flagName] = null;
                }
            }
            else if (token.StartsWith('-') && token.Length > 1 && !char.IsDigit(token[1]))
            {
                var flagName = token[1..];
                if (i + 1 < tokens.Count && !tokens[i + 1].StartsWith('-'))
                {
                    flags[flagName] = tokens[i + 1];
                    i++;
                }
                else
                {
                    flags[flagName] = null;
                }
            }
            else
            {
                positional.Add(token);
            }
        }

        return new CommandArgs(rawInput, commandName, [.. positional], flags);
    }

    private static List<string> Tokenize(string input)
    {
        var tokens = new List<string>();
        var current = new System.Text.StringBuilder();
        var inQuote = false;
        var quoteChar = ' ';

        foreach (var ch in input)
        {
            if (inQuote)
            {
                if (ch == quoteChar)
                {
                    inQuote = false;
                }
                else
                {
                    current.Append(ch);
                }
            }
            else if (ch is '"' or '\'')
            {
                inQuote = true;
                quoteChar = ch;
            }
            else if (char.IsWhiteSpace(ch))
            {
                if (current.Length > 0)
                {
                    tokens.Add(current.ToString());
                    current.Clear();
                }
            }
            else
            {
                current.Append(ch);
            }
        }

        if (current.Length > 0)
        {
            tokens.Add(current.ToString());
        }

        return tokens;
    }
}
