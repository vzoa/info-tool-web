using Microsoft.AspNetCore.Mvc;
using ZoaReference.Features.Terminal.Services;

namespace ZoaReference.Features.Terminal;

[ApiController]
[Route("api/v1/terminal")]
public class TerminalApiController(
    IEnumerable<ITerminalCommand> commands,
    ILoggerFactory loggerFactory,
    IWebHostEnvironment env) : ControllerBase
{
    /// <summary>
    /// Executes a terminal command and returns its plain-text output (ANSI codes stripped).
    /// Only available in the Development environment; returns 404 otherwise.
    /// Used by the CLI parity comparison script in docs/scripts/.
    /// </summary>
    [HttpPost("run")]
    public async Task<IActionResult> Run([FromBody] TerminalRunRequest request, CancellationToken ct)
    {
        if (!env.IsDevelopment())
            return NotFound();

        if (string.IsNullOrWhiteSpace(request.Command))
            return BadRequest("Command is required.");

        var dispatcher = new CommandDispatcher(commands, loggerFactory.CreateLogger<CommandDispatcher>());
        var result = await dispatcher.DispatchAsync(request.Command);

        var plain = AnsiStripper.Strip(result.Text ?? "");
        return Ok(new { output = plain });
    }
}

public record TerminalRunRequest(string Command);

internal static class AnsiStripper
{
    private static readonly System.Text.RegularExpressions.Regex AnsiRegex =
        new(@"\x1b\[[0-9;]*m", System.Text.RegularExpressions.RegexOptions.Compiled);

    public static string Strip(string text) => AnsiRegex.Replace(text, "");
}
