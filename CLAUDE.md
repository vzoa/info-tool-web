# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

**ZOA Reference** — an ASP.NET Core Blazor Server app providing aviation reference data (ATIS, charts, routes, ICAO codes, procedures, scratchpads) for the ZOA air traffic control facility on VATSIM.

**Stack**: .NET 10 / C# 13, Blazor Interactive Server, Tailwind CSS 3, Coravel (scheduling), Sentry (monitoring).

## Build & Run Commands

```bash
# Restore and build (Tailwind CSS compiles automatically via BeforeTargets)
dotnet build

# Run dev server (HTTP: localhost:5063, HTTPS: localhost:7180)
dotnet run

# Docker
docker compose up --build

# Tailwind CSS only
npm run css:build
```

There are no tests in this project.

## Architecture

### Modular Feature Pattern

Each feature lives in `Features/<Name>/` and is self-contained with its own services, models, pages, and scheduled jobs. Features register themselves via two interfaces:

- `IServiceConfigurator` — registers DI services
- `ISchedulerConfigurator` — sets up Coravel background jobs

**Auto-discovery**: `FeatureUtilities/ServiceExtensions.cs` uses reflection to find all implementations at startup. Entry points are named `*Module.cs` or `*Feature.cs`.

### Feature Map

| Feature | Path | Schedule | Key Service |
|---------|------|----------|-------------|
| DigitalAtis | `/atis` | Every minute | DigitalAtisRepository |
| Routes | `/routes` | Hourly | FlightAwareRouteService, CskoRouteService |
| Charts | `/charts` | Hourly | AviationApiChartService |
| IcaoReference | `/codes` | Daily 7 AM | AirlineRepository, AircraftTypeRepository, AirportRepository |
| Docs (Procedures) | `/procedures` | Hourly | DocumentRepository |
| Scratchpads | `/scratchpads` | Hourly | ScratchpadsRepository |
| VnasData | `/positions`, `/videomaps` | Hourly | CachedVnasDataService |
| Nasr | — | On AIRAC cycle | NasrDataService |
| Terminal | `/terminal` | — | CommandDispatcher (CLI-style command interface) |
| Healthcheck | `/healthcheck` | — | Controller-based |
| AirspaceVisualizer | `/airspaceviz` | — | UI only |

### Terminal Command System

The Terminal feature (`Features/Terminal/`) implements a CLI-style interface in the browser. Commands implement `ITerminalCommand` and are auto-registered via DI. `CommandDispatcher` routes input to commands by name/alias, with implicit chart lookup as fallback for unknown commands. To add a new terminal command: create a class implementing `ITerminalCommand` in `Features/Terminal/Commands/` and register it in `TerminalModule.cs`.

### Key Conventions

- **Naming**: `*Repository.cs` for data access, `*Service.cs` for business logic, `*ScheduledJob.cs`/`Fetch*.cs` for background tasks
- **Feature internal structure**: `Models/`, `Services/` or `Repositories/`, `ScheduledJobs/`, `UI/Pages/`
- **Navigation**: defined in `Components/Layout/NavItems.cs`, reorderable via localStorage + JS interop (`wwwroot/scripts/navorder.js`)
- **Data storage**: in-memory caching (`IMemoryCache`) with configurable TTLs; no database
- **External APIs**: configured in `appsettings.json` under `AppSettings` — strongly typed via `AppSettings.cs`
- **Rendering**: `@rendermode InteractiveServer` on all pages
- **Styling**: dark slate theme via Tailwind (custom fonts: Public Sans, IBM Plex Mono)
- **Scheduled jobs use randomized start times** (`HourlyAt(rnd.Next(60))`) to prevent thundering herd against external APIs

### Adding a New Feature

1. Create `Features/<Name>/` directory with `<Name>Module.cs` or `<Name>Feature.cs`
2. Implement `IServiceConfigurator` and optionally `ISchedulerConfigurator`
3. Add page in `Features/<Name>/UI/Pages/` with `@rendermode InteractiveServer`
4. Add nav entry in `Components/Layout/NavItems.cs`

### Entry Points

- `Program.cs` — host setup, DI, middleware pipeline
- `AppSettings.cs` — strongly-typed configuration models
- `Components/App.razor` — root Blazor component
