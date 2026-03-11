#!/usr/bin/env python3
"""Compare CLI vs web terminal output for parity testing.

Usage:
    python compare_commands.py [--web-url URL] [--commands-file FILE]

Requirements:
    - The CLI repo must exist at ../../../zoa-reference-cli (relative to this script)
      with a .venv installed: cd zoa-reference-cli && python -m venv .venv && .venv/bin/pip install -e .
    - The web app must be running locally (default: http://localhost:5000)

The script runs each test command through both the CLI and the web terminal API,
strips ANSI codes, and shows a side-by-side diff of the outputs.
"""

import argparse
import difflib
import json
import re
import subprocess
import sys
import textwrap
import urllib.request
from pathlib import Path

# ---------------------------------------------------------------------------
# Configuration
# ---------------------------------------------------------------------------

SCRIPT_DIR = Path(__file__).parent
CLI_ROOT = SCRIPT_DIR.parents[3] / "zoa-reference-cli"
CLI_VENV = CLI_ROOT / ".venv" / "bin" / "zoa"

DEFAULT_WEB_URL = "http://localhost:5000"

# Commands to compare.  Add/remove as needed.
# Format: plain command string, same as you'd type in the terminal.
# See docs/plans/terminal-cli-parity-v2.md for the full test rationale.
TEST_COMMANDS = [
    # --- airway / aw ---
    "airway V25",           # basic lookup
    "airway J80",           # jet airway
    "airway T217",          # T-route
    "airway V25 SJC MOD",   # with highlights
    "airway SUNOL",         # reverse fix lookup (SUNOL is a fix, not an airway)
    "airway OAK",           # reverse fix lookup (navaid)
    "airway OAKLAND",       # reverse fix lookup (navaid with name)
    "airway INVALID999",    # not found

    # --- mea ---
    "mea SJC MOD",          # basic
    "mea SJC MOD --a 50",   # with altitude (long form)
    "mea SJC MOD -a 50",    # with altitude (short form — currently broken in web)
    "mea FMG MZB",          # different fix pair
    "mea SUNOL MZB --a 80", # altitude check

    # --- navaid / nav ---
    "navaid SJC",
    "navaid MZB",
    "navaid VORTAC",
    "navaid oakland",
    "navaid DME",

    # --- airport / ap ---
    "airport KSFO",
    "airport SFO",
    "airport san francisco",
    "airport KOAK",

    # --- airports ---
    "airports",

    # --- aircraft / ac ---
    "aircraft B738",
    "aircraft A320",
    "aircraft boeing",
    "aircraft cessna 172",

    # --- airline / al ---
    "airline UAL",
    "airline united",
    "airline DAL",

    # --- route / rt ---
    "route KSFO KLAX",
    "route SFO LAX",
    "route OAK LAS",
    "route OAK LAS -a",
    "route OAK LAS -n 3",
    "route KSFO KJFK",

    # --- descent / desc ---
    "descent 350 100",      # altitude → distance
    "descent 350 25",       # altitude → target altitude
    "descent SJC MOD",      # fix-to-fix

    # --- approaches / apps ---
    "approaches OAK OAKES3",
    "approaches RNO SCOLA1",
    "approaches RNO SCOLA1 17",
    "approaches SFO BDEGA4 28L",

    # --- atis ---
    "atis SFO",
    "atis OAK",
    "atis --all",
    "atis all",

    # --- chart / charts ---
    "chart OAK ILS 28L",
    "chart OAK CNDEL5",
    "chart SFO DYAMD5",

    # --- list / ls ---
    "list OAK",
    "list SFO DP",
    "list SFO STAR",
    "list OAK IAP",
    "list OAK IAP ILS",

    # --- cifp ---
    "cifp KSFO ILS28R",
    "cifp OAK CNDEL5",
    "cifp RNO SCOLA1",

    # --- metar ---
    "metar KSFO",
    "metar SFO OAK SJC",
    "metar KRNO",

    # --- position / pos ---
    "position NorCal",
    "position TWR",
    "position OAK",

    # --- scratchpad / scratch ---
    "scratchpad SFO",
    "scratchpad --list",

    # --- sop / proc ---
    "sop",
    "sop OAK",
    "sop IFR",

    # --- help ---
    "help",
    "help airway",
    "help mea",
]

# ---------------------------------------------------------------------------
# Helpers
# ---------------------------------------------------------------------------

ANSI_RE = re.compile(r"\x1b\[[0-9;]*[mK]")
# VT100 terminal title sequences: ESC]0;...(BEL or ESC\) or the raw ]0;... form
TITLE_RE = re.compile(r"(\x1b\]|\])\d+;[^\x07\x1b]*(\x07|\x1b\\)?")
# CLI noise lines to drop
NOISE_RE = re.compile(
    r"^(Downloading |NASR data cached|CIFP data cached|Searching routes|"
    r"Analyzing (MEA|STAR|CIFP)|Finding approaches|"
    r"Searching (airports|aircraft|airlines|positions|IAP|DP|STAR)( charts)?[: ]|"
    r"Fetching (ATIS|charts|DP |STAR |IAP |available|scratchpads)|"
    r"Available (charts|facilities)|DP charts|STAR charts|IAP charts|"
    r"Charts containing|Looking up[: ]|Opening chart:|"
    r"Looking up procedure:|Chart has \d+ page)"
)


def strip_ansi(text: str) -> str:
    text = TITLE_RE.sub("", text)
    return ANSI_RE.sub("", text)


def normalize(text: str) -> list[str]:
    """Strip ANSI/title escapes, drop noise lines, remove blank lines and trailing whitespace."""
    text = strip_ansi(text)
    lines = text.splitlines()
    lines = [l.rstrip() for l in lines]
    # Remove timing lines e.g. "(123ms)"
    lines = [l for l in lines if not re.match(r"^\s*\(\d+ms\)\s*$", l)]
    # Remove CLI-only progress/noise lines
    lines = [l for l in lines if not NOISE_RE.match(l.strip())]
    # Drop blank lines to make structural diffs cleaner
    lines = [l for l in lines if l.strip()]
    return lines


def run_cli(command: str) -> str:
    """Run the CLI command and return stdout."""
    if not CLI_VENV.exists():
        return f"[CLI not available: {CLI_VENV} not found]"
    try:
        result = subprocess.run(
            [str(CLI_VENV)] + command.split(),
            capture_output=True,
            text=True,
            timeout=30,
        )
        return result.stdout + result.stderr
    except subprocess.TimeoutExpired:
        return "[CLI timed out]"
    except Exception as e:
        return f"[CLI error: {e}]"


def run_web(command: str, web_url: str) -> str:
    """Call the web terminal API and return the plain-text output."""
    url = f"{web_url.rstrip('/')}/api/v1/terminal/run"
    payload = json.dumps({"command": command}).encode()
    req = urllib.request.Request(
        url,
        data=payload,
        headers={"Content-Type": "application/json"},
        method="POST",
    )
    try:
        with urllib.request.urlopen(req, timeout=30) as resp:
            body = json.loads(resp.read())
            return body.get("output", "")
    except Exception as e:
        return f"[Web error: {e}]"


def print_diff(command: str, cli_out: str, web_out: str) -> bool:
    """Print a unified diff between CLI and web output. Returns True if they differ."""
    cli_lines = normalize(cli_out)
    web_lines = normalize(web_out)

    diff = list(
        difflib.unified_diff(
            cli_lines,
            web_lines,
            fromfile="CLI",
            tofile="Web",
            lineterm="",
        )
    )

    if not diff:
        print(f"  ✓  {command}")
        return False

    print(f"\n{'='*70}")
    print(f"  DIFF  {command}")
    print(f"{'='*70}")
    for line in diff:
        if line.startswith("+"):
            print(f"\033[32m{line}\033[0m")
        elif line.startswith("-"):
            print(f"\033[31m{line}\033[0m")
        elif line.startswith("@"):
            print(f"\033[36m{line}\033[0m")
        else:
            print(line)
    return True


def print_side_by_side(command: str, cli_out: str, web_out: str) -> None:
    """Print CLI and web output side-by-side."""
    cli_lines = normalize(cli_out)
    web_lines = normalize(web_out)
    col_w = 50
    print(f"\n{'='*102}")
    print(f"  {command}")
    print(f"{'CLI':<{col_w}}  {'WEB':<{col_w}}")
    print(f"{'-'*col_w}  {'-'*col_w}")
    for i in range(max(len(cli_lines), len(web_lines))):
        cl = cli_lines[i] if i < len(cli_lines) else ""
        wl = web_lines[i] if i < len(web_lines) else ""
        print(f"{cl[:col_w]:<{col_w}}  {wl[:col_w]:<{col_w}}")


# ---------------------------------------------------------------------------
# Main
# ---------------------------------------------------------------------------


def main() -> None:
    parser = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    parser.add_argument("--web-url", default=DEFAULT_WEB_URL, help="Web app base URL")
    parser.add_argument("--side-by-side", "-s", action="store_true", help="Show side-by-side instead of diff")
    parser.add_argument("--commands", "-c", nargs="+", metavar="CMD",
                        help="Run specific commands instead of the default test set")
    args = parser.parse_args()

    commands = args.commands if args.commands else TEST_COMMANDS
    web_url = args.web_url

    print(f"CLI: {CLI_VENV}")
    print(f"Web: {web_url}/api/v1/terminal/run")
    print(f"Commands: {len(commands)}\n")

    diffs = 0
    for cmd in commands:
        cli_out = run_cli(cmd)
        web_out = run_web(cmd, web_url)

        if args.side_by_side:
            print_side_by_side(cmd, cli_out, web_out)
        else:
            had_diff = print_diff(cmd, cli_out, web_out)
            if had_diff:
                diffs += 1

    if not args.side_by_side:
        print(f"\n{'='*70}")
        if diffs == 0:
            print("All commands match!")
        else:
            print(f"{diffs}/{len(commands)} commands have differences.")
        sys.exit(1 if diffs > 0 else 0)


if __name__ == "__main__":
    main()
