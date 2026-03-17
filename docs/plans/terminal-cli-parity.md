# Terminal CLI Parity Plan

Align the web terminal commands with the ZOA Reference CLI (`zoa-reference-cli`) in functionality, output format, and behavior.

## File Reference

### CLI (`C:\Users\Leftos\source\repos\zoa-reference-cli\src\zoa_ref\`)

| File | Contents |
|------|----------|
| `cli.py` | Click command definitions (args, flags, help text references) |
| `commands.py` | All `do_*` command handler functions |
| `display.py` | All `display_*` output formatting functions |
| `cli_utils.py` | `COMMAND_HELP` dict, constants, `ATIS_AIRPORTS`, utilities |
| `atis.py` | ATIS data fetching (Playwright scraping) |
| `charts.py` | Chart lookup, PDF handling, rotation |
| `routes.py` | Route lookup (FlightAware, CSKO) |
| `icao.py` | ICAO code lookups (airline, aircraft, airport) |
| `navaids.py` | Navaid search |
| `airways.py` | Airway data and fix sequences |
| `mea.py` | MEA/MOCA analysis |
| `cifp.py` | CIFP procedure parsing |
| `metar.py` | METAR parsing and decoding |
| `descent.py` | Descent calculations |
| `positions.py` | ATC position lookups |
| `scratchpads.py` | Scratchpad data |
| `procedures.py` | SOP/procedure PDF handling |
| `approaches.py` | Approach chart matching |
| `waypoints.py` | Fix/waypoint/navaid coordinate lookups |

### Web Terminal (`X:\dev\info-tool-web\Features\Terminal\`)

**Services (shared infrastructure):**

| File | Contents |
|------|----------|
| `Services/ITerminalCommand.cs` | Command interface |
| `Services/CommandArgs.cs` | Input parsing (positional args, flags) |
| `Services/CommandResult.cs` | Result types (Text, OpenUrl, Error, CloseViewer) |
| `Services/TextFormatter.cs` | ANSI table formatting (headers, rows, colors) |
| `Services/AirportIdHelper.cs` | ICAO/FAA code normalization |
| `Services/CommandDispatcher.cs` | Command routing and execution |

**Commands (one file per command):**

| File | CLI equivalent |
|------|----------------|
| `Commands/AtisCommand.cs` | `atis` |
| `Commands/AircraftCommand.cs` | `aircraft`/`ac` |
| `Commands/AirlineCommand.cs` | `airline`/`al` |
| `Commands/AirportCommand.cs` | `airport`/`ap` |
| `Commands/AirportsCommand.cs` | `airports` |
| `Commands/RouteCommand.cs` | `route`/`rt` |
| `Commands/DescentCommand.cs` | `descent`/`desc` |
| `Commands/ApproachesCommand.cs` | `approaches`/`apps` |
| `Commands/NavaidCommand.cs` | `navaid`/`nav` |
| `Commands/AirwayCommand.cs` | `airway`/`aw` |
| `Commands/MeaCommand.cs` | `mea` |
| `Commands/CifpCommand.cs` | `cifp` |
| `Commands/ChartCommand.cs` | `chart`/`charts` |
| `Commands/ListCommand.cs` | `list`/`ls` |
| `Commands/ScratchpadCommand.cs` | `scratchpad`/`scratch` |
| `Commands/ProcedureCommand.cs` | `sop`/`proc` |
| `Commands/PositionCommand.cs` | `position`/`pos` |
| `Commands/OpenCommand.cs` | `vis`/`tdls`/`strips` |
| `Commands/HelpCommand.cs` | `help` |
| `Commands/ClearCommand.cs` | `clear`/`cls` |
| `Commands/CloseCommand.cs` | `close` |

**Backend services used by commands** (in `Features/` subdirectories):

| Service | Used by |
|---------|---------|
| `DigitalAtis/Repositories/DigitalAtisRepository.cs` | AtisCommand |
| `Charts/Services/AviationApiChartService.cs` | ChartCommand, ListCommand |
| `Routes/Services/FlightAwareRouteService.cs` | RouteCommand |
| `Routes/Services/CskoRouteService.cs` | RouteCommand |
| `IcaoReference/Repositories/Airline*.cs, Aircraft*.cs, Airport*.cs` | Airline/Aircraft/AirportCommand |
| `Scratchpads/Repositories/ScratchpadsRepository.cs` | ScratchpadCommand |
| `Docs/Repositories/DocumentRepository.cs` | ProcedureCommand |
| `VnasData/Services/CachedVnasDataService.cs` | PositionCommand, NavaidCommand, AirwayCommand, etc. |

## Missing Commands

- [x] **metar** — Implemented `MetarCommand.cs` fetching from aviationweather.gov API with decoded fields (flight category, wind, altimeter, visibility, clouds, temperature, weather) and color-coded categories

## Alias Differences

- [x] **descent**: Added `des` as alias alongside `desc`

## Command-by-Command Comparison

For each command: read the web `Commands/*.cs` file and the CLI `commands.py` handler + `display.py` formatter. Check arguments, flags, output columns, and edge cases.

### Data Reference Commands

- [x] **atis** — Added `-a` shorthand (cross-cutting fix in `CommandArgs.cs`), `atis all` positional support, available airports hint on empty input. Table format kept (better than CLI's raw text dump)
- [x] **aircraft** — Updated columns to match CLI: Type/Manufacturer-Model/Eng/Wt/CWT/SRS/LAHSO (was missing CWT, SRS, LAHSO)
- [x] **airline** — Renamed "Callsign" column to "Telephony" to match CLI's ICAO terminology
- [x] **airport** — No changes needed. Web shows more info (IATA + FIR) than CLI (Local + Name)
- [x] **airports** — No changes needed. Web's Bravo/Charlie/Delta/Other grouping is more informative than CLI's Major/Other

### Navigation Commands

- [x] **route** — Added Dep Rwy/Arr Rwy columns to preferred routes, reordered LOA columns (Route/RNAV/Notes), reordered real-world columns (Freq/Route/Altitude), defaulted to top 5, added `-a`/`--all` and `-n` flags
- [x] **descent** — Added `des` alias. Output format differs (web is more detailed/labeled) but functionality matches CLI's 3 modes
- [x] **approaches** — Added optional runway filtering (`approaches RNO SCOLA1 17`) matching CLI behavior
- [x] **navaid** — No changes needed. Web table format is more detailed than CLI's one-line format
- [x] **airway** — No changes needed. Web table + MEA/MOCA is more detailed than CLI's inline chain
- [x] **mea** — Web takes two fixes (different from CLI's full route string). Kept as-is — full route parsing would be a large feature addition
- [x] **cifp** — No changes needed. Both show procedure legs + route diagram

### Chart Commands

- [x] **chart** — No changes needed. Fuzzy match works well. PDF rotation/continuation merging are CLI-specific (Playwright PDF manipulation) — not applicable in web viewer
- [x] **list** — No changes needed. Grouping, filtering, and type aliases (SID/APP/TAXI) all match CLI

### Reference Commands

- [x] **scratchpad** — Added `--list` flag to show available facilities. Added available facilities hint on empty input
- [x] **sop** — No changes needed. Web's category listing (no args) + search matches CLI's `--list` behavior
- [x] **position** — Added TCP code and frequency search to match CLI (was only searching name/callsign/radioName)

### External Tools

- [x] **open/vis/tdls/strips** — No changes needed. Web's `vis`/`tdls`/`strips` aliases work the same as CLI's separate commands

### General

- [x] **help** — No changes needed. Shows commands with aliases and detailed per-command help
- [x] **Implicit chart lookup** — Added to `CommandDispatcher.cs`: unknown commands fall through to chart search (e.g., `OAK CNDEL5` → `chart OAK CNDEL5`)
