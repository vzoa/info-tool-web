# Terminal CLI Parity v2 — Test-Driven Alignment Plan

Use the comparison harness in `docs/scripts/compare_commands.py` to drive every fix.
Run it against a locally running web app and the CLI venv to verify before and after each change.

```
python3 docs/scripts/compare_commands.py --web-url http://localhost:5000
```

---

## How to Read This Document

Each command section lists:
1. **Test commands** — strings to add to the harness (already included in the script)
2. **Differences** — concrete gaps between CLI and webapp output
3. **Action** — what to change in the webapp (or "No action" if intentional)

Differences are classified:
- `[FIX]` — functional gap; web is missing output the CLI provides
- `[SKIP]` — intentional difference; not applicable in web context
- `[VERIFY]` — need to run harness to confirm behavior matches

---

## Commands

### airway / aw

**Test commands:**
```
airway V25
airway J80
airway T217
airway V25 SJC MOD
airway SUNOL
airway FMG
airway MZB
airway INVALID999
```

**Differences:**

| # | CLI | Webapp | Action |
|---|-----|--------|--------|
| 1 | Header: `AIRWAY V25 (W to E) — 22 fixes` (includes cardinal direction) | Header: `Airway V25 — 22 fixes` (no direction) | `[FIX]` Add direction computation |
| 2 | When query doesn't match `[VJQT]\d+` pattern, does reverse fix lookup: `FIX SUNOL — found on 3 airway(s)` with one line per airway showing snippet | Returns "No airway found" error | `[FIX]` Implement reverse fix lookup |
| 3 | Each line in reverse lookup: `V25    (W to E)        22 fixes   ..PGARY..[SUNOL]..MOD..` | Not implemented | `[FIX]` Part of reverse lookup |
| 4 | Navaid fixes display inline name: `MZB (Morgan Hill VOR)` | Shows bare fix ID: `MZB` | `[FIX]` Expand navaid names using NASR navaid data |
| 5 | Highlighted fixes wrapped in brackets with yellow color: `[SUNOL]` | Highlighted in orange, no brackets | `[FIX]` Use yellow + brackets to match CLI |

**Files to change:** `AirwayCommand.cs`, `NasrDataService.cs` (already has `FindAirwaysContainingFix`)

**Implementation notes:**
- Direction: compute from first/last fix coordinates using the same W-to-E / N-to-S preference as the CLI's `_compute_direction_and_should_reverse()`. NASR `AirwayFix` records already have `Latitude`/`Longitude`.
- Reverse lookup: call `nasrDataService.FindAirwaysContainingFix(fixId)`, then for each airway call `GetAirwayFixes()`, find the fix index, show ±2 context fixes.
- Navaid names: call `nasrDataService.SearchNavaids(fixId)` and look for an exact ID match; if found, append `(Name)`.
- Fix ordering after direction: for airway lookup, optionally reverse the fix list when direction calls for it.

---

### mea

**Test commands:**
```
mea SJC MOD
mea SJC MOD --a 50
mea SJC MOD -a 50
mea FMG MZB
mea SUNOL MZB --a 80
mea INVALID1 INVALID2
```

**Differences:**

| # | CLI | Webapp | Action |
|---|-----|--------|--------|
| 1 | Flag is `--altitude` / `-a` (standard short form) | Flag is `--a` only (no short form `-a`) | `[FIX]` Add `-a` as short-flag alias in `CommandArgs.Parse` or handle in `MeaCommand` |
| 2 | When no altitude: prints `Maximum MEA: 14,000 ft` summary line before segment list | No summary line | `[FIX]` Add max MEA summary |
| 3 | When altitude provided + safe: prints green `SAFE: 15000 ft meets MEA requirement of 14,000 ft` | No safety status | `[FIX]` Add SAFE/WARNING status line |
| 4 | When altitude provided + unsafe: prints yellow `WARNING: 10000 ft is BELOW required MEA of 14,000 ft` | No safety status | `[FIX]` Part of above |
| 5 | Segments sorted by MEA descending | Table in airway/database order | `[FIX]` Sort by MEA descending |
| 6 | CLI takes full route string `mea V25 SJC MOD J80 RNO` (segments across multiple airways) | Takes exactly 2 fixes | `[SKIP]` Full route parsing is a large feature; 2-fix interface is acceptable |

**Files to change:** `MeaCommand.cs`, possibly `CommandArgs.cs`

---

### airway (MEA section — already shown in airway table output)

The inline MEA/MOCA table the webapp appends to `airway V25` output is a webapp addition not present in the CLI. Keep it.

---

### navaid / nav

**Test commands:**
```
navaid SJC
navaid MZB
navaid VORTAC
navaid oakland
navaid DME
navaid INVALID_XYZ
```

**Differences:**

| # | CLI | Webapp | Action |
|---|-----|--------|--------|
| 1 | One-line format per result: `SJC - San Jose VOR/DME (San Jose, CA) [37.3626, -121.9244]` | Table with ID/Name/Type/Freq/Coordinates columns | `[VERIFY]` Web table is more detailed; no change needed |
| 2 | CLI includes city/state in output | Webapp shows only coordinates | `[SKIP]` NASR data doesn't include city/state; acceptable |

**Action:** No changes needed. Run harness to confirm data matches.

---

### airport / ap

**Test commands:**
```
airport KSFO
airport SFO
airport san francisco
airport OAK
airport KOAK
airport INVALID
```

**Differences:**

| # | CLI | Webapp | Action |
|---|-----|--------|--------|
| 1 | Columns: ICAO / Local / Name (3 columns) | Columns: ICAO / IATA / Name / FIR (4 columns) | `[VERIFY]` Web has more info; no change needed |

**Action:** No changes needed.

---

### airports

**Test commands:**
```
airports
```

**Differences:**

| # | CLI | Webapp | Action |
|---|-----|--------|--------|
| 1 | Groups as Major / Other | Groups as Bravo / Charlie / Delta / Other | `[VERIFY]` Web grouping is more informative; no change needed |

**Action:** No changes needed.

---

### aircraft / ac

**Test commands:**
```
aircraft B738
aircraft A320
aircraft boeing
aircraft cessna 172
aircraft INVALID
```

**Differences:**

| # | CLI | Webapp | Action |
|---|-----|--------|--------|
| 1 | No result cap (shows all matches) | Caps at 25 results | `[VERIFY]` Check if cap causes useful results to be cut off on common queries |
| 2 | `--browser` / `--no-cache` flags | Not supported | `[SKIP]` Browser mode not applicable |

**Action:** No changes needed (cap is fine for web UX).

---

### airline / al

**Test commands:**
```
airline UAL
airline united
airline DAL
airline southwest
airline INVALID
```

**Differences:**

| # | CLI | Webapp | Action |
|---|-----|--------|--------|
| 1 | No result cap | Caps at 25 results | `[VERIFY]` Same as aircraft |
| 2 | `--browser` / `--no-cache` flags | Not supported | `[SKIP]` |

**Action:** No changes needed.

---

### route / rt

**Test commands:**
```
route KSFO KLAX
route SFO LAX
route OAK LAS
route OAK LAS -a
route OAK LAS -n 3
route KSFO KJFK
route INVALID KSFO
```

**Differences:**

| # | CLI | Webapp | Action |
|---|-----|--------|--------|
| 1 | `--flights` / `-f` flag shows recent ASDI flights | Not implemented | `[SKIP]` Not available in web data |
| 2 | `--export-lc` exports to LCTrainer cache | Not implemented | `[SKIP]` LCTrainer-specific |
| 3 | `--browser` flag | Not supported | `[SKIP]` |
| 4 | CLI prints `Searching routes: SFO → LAX...` progress line | No progress line | `[VERIFY]` Progress line is CLI UX; stripped by harness normalization |

**Action:** No changes needed.

---

### descent / desc / des

**Test commands:**
```
descent 350 100
descent 350 25
descent SJC MOD
descent 240 KSFO
descent INVALID 100
```

**Differences:**

| # | CLI | Webapp | Action |
|---|-----|--------|--------|
| 1 | Fix-to-fix mode: computes Haversine distance, then descent calc | Same | `[VERIFY]` |
| 2 | Altitude/distance format | Webapp formats as `FL350 (35,000 ft)` | `[VERIFY]` More readable; no change needed |

**Action:** No changes needed.

---

### approaches / apps

**Test commands:**
```
approaches OAK CNDEL5
approaches RNO SCOLA1
approaches RNO SCOLA1 17
approaches SFO BDEGA4 28L
approaches OAK SUNOL
approaches KSFO ILS28R
```

**Differences:**

| # | CLI | Webapp | Action |
|---|-----|--------|--------|
| 1 | Runway filtering: strips leading zeros before comparing (`17` matches `17L`, `017L`) | Simple `Contains()` match | `[VERIFY]` Both should behave the same on normal inputs |

**Action:** No changes needed.

---

### atis

**Test commands:**
```
atis SFO
atis OAK
atis SJC
atis --all
atis all
atis INVALID
atis
```

**Differences:**

| # | CLI | Webapp | Action |
|---|-----|--------|--------|
| 1 | Shows raw ATIS text | Shows parsed table (Type/Info/Altimeter/Status) | `[VERIFY]` Web is more structured; no change needed |

**Action:** No changes needed.

---

### chart / charts

**Test commands:**
```
chart OAK ILS 28L
chart SFO
chart OAK CNDEL5
chart RNO ILS 17L
chart SFO DYAMD5
chart INVALID
```

**Differences:**

| # | CLI | Webapp | Action |
|---|-----|--------|--------|
| 1 | `--rotate` / `-r` / `--no-rotate` PDF rotation | Not applicable | `[SKIP]` |
| 2 | `--link` shows URL without opening | Web always returns URL in result | `[SKIP]` |
| 3 | CLI does fuzzy token matching with number-word expansion (e.g., `dyamd5` → `DYAMD FIVE`) | Webapp expands digits too | `[VERIFY]` |

**Action:** No changes needed.

---

### list / ls

**Test commands:**
```
list OAK
list SFO DP
list SFO STAR
list OAK IAP
list OAK IAP ILS
list SJC
list INVALID
```

**Differences:**

| # | CLI | Webapp | Action |
|---|-----|--------|--------|
| 1 | Type aliases: SID→DP, APP→IAP, TAXI→APD | Same | `[VERIFY]` |

**Action:** No changes needed.

---

### cifp

**Test commands:**
```
cifp KSFO ILS28R
cifp OAK CNDEL5
cifp RNO SCOLA1
cifp SFO BDEGA4
cifp KSFO INVALID
```

**Differences:**

| # | CLI | Webapp | Action |
|---|-----|--------|--------|
| 1 | Dot notation for transition: `cifp SFO LEGGS.BDEGA4` | Unknown if supported | `[VERIFY]` Check CifpCommand.cs |

**Action:** Verify transition dot notation support; no other known gaps.

---

### metar

**Test commands:**
```
metar KSFO
metar SFO OAK SJC
metar KRNO
metar KOAK KSFO KSJC KLAX
metar INVALID
```

**Differences:**

| # | CLI | Webapp | Action |
|---|-----|--------|--------|
| 1 | Fetches from internal module | Fetches from aviationweather.gov API | `[VERIFY]` Same data source; results should match |

**Action:** No changes needed.

---

### position / pos

**Test commands:**
```
position NorCal
position TWR
position OAK
position 132.35
position INVALID
```

**Differences:**

| # | CLI | Webapp | Action |
|---|-----|--------|--------|
| 1 | `--browser` flag | Not supported | `[SKIP]` |
| 2 | Result cap: CLI unknown, webapp 30 | `[VERIFY]` |

**Action:** No changes needed.

---

### scratchpad / scratch

**Test commands:**
```
scratchpad SFO
scratchpad OAK
scratchpad --list
scratchpad INVALID
scratchpad
```

**Differences:**

| # | CLI | Webapp | Action |
|---|-----|--------|--------|
| 1 | `--no-cache` flag | Not supported | `[SKIP]` |

**Action:** No changes needed.

---

### sop / proc

**Test commands:**
```
sop
sop OAK
sop IFR
sop NorCal
sop INVALID_DOCUMENT
```

**Differences:**

| # | CLI | Webapp | Action |
|---|-----|--------|--------|
| 1 | Multi-step lookup: `sop OAK 2-2` opens specific section; `sop SJC "IFR Departures"` searches within doc | Web only searches by document name | `[SKIP]` PDF text search integration is large scope; web list+search is acceptable |
| 2 | `--list` flag | Web shows categories at zero args (same behavior) | `[VERIFY]` |

**Action:** No changes needed.

---

### help

**Test commands:**
```
help
help airway
help mea
help route
help INVALID
```

**Differences:**

| # | CLI | Webapp | Action |
|---|-----|--------|--------|
| 1 | Uses Click's help formatting | Custom formatted | `[VERIFY]` Content should match |

**Action:** No changes needed.

---

## Implementation Status

### Phase 1 — Functional Gaps ✅ Complete

1. ✅ **airway: reverse fix lookup** — `airway SUNOL` shows all airways with ±2 fix context snippet
2. ✅ **airway: direction in header** — `Airway V25 (W to E) — 22 fixes`
3. ✅ **airway: navaid name expansion** — `MZB (Morgan Hill VOR)` in fix column (via `GetNavaidById`)
4. ✅ **airway: highlighted fix style** — yellow + brackets `[SUNOL]`
5. ✅ **mea: `-a` short flag** — already worked via `CommandArgs` parsing; also added `--altitude` alias
6. ✅ **mea: SAFE/WARNING summary** — green SAFE / yellow WARNING status line when altitude provided
7. ✅ **mea: max MEA summary** — `Maximum MEA: X ft` line when no altitude; segments sorted MEA-desc
8. ✅ **metar: LIFR color** — changed from Yellow to Magenta to match CLI; added `AnsiColor.Magenta`
9. ✅ **metar: altimeter conversion** — API returns mb, was displaying raw mb value; now converts to inHg via `* 0.02953`
10. ✅ **airway: navaid ID reverse lookup** — NASR AWY stores navaid station names (e.g. "MISSION" for MZB), not short IDs; when `FindAirwaysContainingFix` fails, look up the navaid by ID and retry with first word of name (`airway MZB`, `airway FMG` now work)
11. ✅ **approaches: list available STARs on not-found** — when a STAR is not found, list all available STARs at the airport

### Phase 2 — Verified (all other commands reviewed against CLI source)

| Command | Status | Notes |
|---------|--------|-------|
| aircraft | ✅ No change | Columns match CLI exactly |
| airline | ✅ No change | Telephony column name correct |
| airport | ✅ No change | Web adds FIR column (improvement) |
| airports | ✅ No change | Bravo/Charlie/Delta grouping better than Major/Other |
| approaches | ✅ Fixed (item 11) | STAR not-found now lists available STARs |
| atis | ✅ No change | Web table format intentionally richer than CLI raw text |
| chart | ✅ No change | Fuzzy matching with digit-word expansion matches CLI |
| cifp | ✅ No change | Dot-notation transition supported via join |
| descent | ✅ No change | Same 318 ft/nm formula; web output more labeled |
| help | ✅ No change | Shows all commands and per-command detail |
| list | ✅ No change | Type aliases (SID/APP/TAXI), search, grouping all match |
| navaid | ✅ No change | Web table format richer than CLI one-liner |
| position | ✅ No change | All 5 columns match CLI |
| route | ✅ No change | 3 sections match; `--flights` skipped (ASDI not available) |
| scratchpad | ✅ No change | Column names differ (Entry/Description vs Code/Meaning) but data is same |
| sop | ✅ No change | Multi-step PDF section lookup skipped (large scope) |

### Phase 3 — Update Parity Plan

`terminal-cli-parity.md` should be updated to reflect the new fixes in this document.

---

## Harness Limitations

The comparison harness strips ANSI codes and timing lines but cannot account for:
- Live data differences (real-world routes, ATIS content changes between runs)
- Formatting differences where both sides show the same data differently (table vs inline)
- Commands that open URLs (chart, sop, cifp) — only text output is compared

For live-data commands (atis, route, metar), run harness at the same time for both CLI and web to minimize data drift. Focus on structural differences (missing columns, missing sections) rather than exact value matches.
