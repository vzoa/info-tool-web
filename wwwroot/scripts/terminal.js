const HISTORY_KEY = "zoa-terminal-history";
const MAX_HISTORY = 500;

let terminal = null;
let fitAddon = null;
let dotnetRef = null;
let lineBuffer = "";
let cursorPos = 0;
let history = [];
let historyIndex = -1;
let tempLine = "";
let pendingSelectionCount = 0; // When >0, single digit keypress triggers selection

const PROMPT = "\x1b[38;5;208mzoa\x1b[0m \x1b[36m❯\x1b[0m ";
const PROMPT_LEN = 6; // "zoa ❯ " visible characters

function loadHistory() {
    try {
        const json = localStorage.getItem(HISTORY_KEY);
        return json ? JSON.parse(json) : [];
    } catch {
        return [];
    }
}

function saveHistory(entries) {
    try {
        localStorage.setItem(HISTORY_KEY, JSON.stringify(entries.slice(-MAX_HISTORY)));
    } catch {
        // localStorage full or unavailable
    }
}

// --- Word boundary helpers ---

function wordBoundaryLeft(buf, pos) {
    // Skip whitespace left, then skip non-whitespace left
    let i = pos;
    while (i > 0 && buf[i - 1] === " ") i--;
    while (i > 0 && buf[i - 1] !== " ") i--;
    return i;
}

function wordBoundaryRight(buf, pos) {
    // Skip non-whitespace right, then skip whitespace right
    let i = pos;
    while (i < buf.length && buf[i] !== " ") i++;
    while (i < buf.length && buf[i] === " ") i++;
    return i;
}

// --- Core ---

export async function initialize(containerEl, ref) {
    dotnetRef = ref;
    history = loadHistory();
    historyIndex = -1;

    // Dynamic import of xterm.js
    const [xtermModule, fitModule] = await Promise.all([
        import("https://esm.sh/@xterm/xterm@5"),
        import("https://esm.sh/@xterm/addon-fit@0.10")
    ]);

    const Terminal = xtermModule.Terminal;
    const FitAddon = fitModule.FitAddon;

    fitAddon = new FitAddon();
    terminal = new Terminal({
        cursorBlink: true,
        fontSize: 14,
        fontFamily: "'IBM Plex Mono', 'Cascadia Code', 'Fira Code', monospace",
        theme: {
            background: "#0f172a",
            foreground: "#e2e8f0",
            cursor: "#ea580c",
            selectionBackground: "#334155",
            black: "#0f172a",
            red: "#ef4444",
            green: "#22c55e",
            yellow: "#eab308",
            blue: "#3b82f6",
            magenta: "#a855f7",
            cyan: "#06b6d4",
            white: "#e2e8f0",
        },
        allowTransparency: false,
        scrollback: 5000,
    });

    terminal.loadAddon(fitAddon);
    terminal.open(containerEl);
    fitAddon.fit();

    // Welcome banner
    terminal.writeln("\x1b[38;5;208m" +
        "  ______  ___    _    ___      __                          \r\n" +
        "     /   / _ \\  / \\  | _ \\___ / _|___ _ _ ___ _ _  __ ___ \r\n" +
        "    /   | (_) |/ _ \\ |   / -_)  _/ -_) '_/ -_) ' \\/ _/ -_)\r\n" +
        "  _____  \\___//_/ \\_\\|_|_\\___|_| \\___|_| \\___|_||_\\__\\___|" +
        "\x1b[0m");
    terminal.writeln("");
    terminal.writeln("  \x1b[90mType \x1b[36mhelp\x1b[90m for available commands.\x1b[0m");
    terminal.writeln("");
    writePrompt();

    terminal.onData(onData);

    // Resize observer
    const resizeObserver = new ResizeObserver(() => {
        if (fitAddon) fitAddon.fit();
    });
    resizeObserver.observe(containerEl);
}

function writePrompt() {
    terminal.write(PROMPT);
}

function clearLine() {
    terminal.write("\r" + PROMPT + " ".repeat(lineBuffer.length + 2) + "\r" + PROMPT);
}

function redrawLine() {
    clearLine();
    terminal.write(lineBuffer);
    const diff = lineBuffer.length - cursorPos;
    if (diff > 0) {
        terminal.write(`\x1b[${diff}D`);
    }
}

async function submitInput(input) {
    // Process command with loading spinner
    try {
        const spinnerFrames = ["⠋", "⠙", "⠹", "⠸", "⠼", "⠴", "⠦", "⠧", "⠇", "⠏"];
        let spinnerIdx = 0;
        const spinnerInterval = setInterval(() => {
            terminal.write(`\r\x1b[90m ${spinnerFrames[spinnerIdx]}\x1b[0m`);
            spinnerIdx = (spinnerIdx + 1) % spinnerFrames.length;
        }, 80);

        const result = await dotnetRef.invokeMethodAsync("ProcessCommand", input);

        clearInterval(spinnerInterval);
        terminal.write("\r\x1b[K"); // Clear spinner line

        if (result) {
            const lines = result.split("\n");
            for (const line of lines) {
                terminal.writeln(line.replace(/\r$/, ""));
            }

            // Detect pending selection prompt (line ending with "enter a number to open")
            const lastNonEmpty = lines.filter(l => l.replace(/\r$/, "").trim()).pop() || "";
            const selectionMatch = lastNonEmpty.match(/(\d+)\s+charts?\s+for/);
            if (selectionMatch) {
                pendingSelectionCount = parseInt(selectionMatch[1], 10);
            } else {
                pendingSelectionCount = 0;
            }
        }
    } catch (err) {
        terminal.writeln("\x1b[31mError: " + err.message + "\x1b[0m");
        pendingSelectionCount = 0;
    }
}

async function onData(data) {
    // --- Inline single-keypress selection for ≤9 pending choices ---
    if (pendingSelectionCount > 0 && pendingSelectionCount <= 9
        && data.length === 1 && data >= "1" && data <= "9"
        && lineBuffer === "") {
        const num = parseInt(data, 10);
        if (num <= pendingSelectionCount) {
            terminal.writeln(data);
            pendingSelectionCount = 0;
            await submitInput(data);
            writePrompt();
            return;
        }
    }

    // Any non-digit input clears pending selection mode
    if (data !== "\r" && data !== "\t" && !data.startsWith("\x1b")) {
        if (pendingSelectionCount > 0 && lineBuffer === "") {
            const isDigit = data.length === 1 && data >= "0" && data <= "9";
            if (!isDigit) {
                pendingSelectionCount = 0;
            }
        }
    }

    // --- Enter ---
    if (data === "\r") {
        terminal.writeln("");
        const input = lineBuffer.trim();
        lineBuffer = "";
        cursorPos = 0;
        historyIndex = -1;

        if (input.length > 0) {
            if (history.length === 0 || history[history.length - 1] !== input) {
                history.push(input);
                saveHistory(history);
            }
            await submitInput(input);
        }
        writePrompt();
        return;
    }

    // --- Ctrl+C ---
    if (data === "\x03") {
        terminal.writeln("^C");
        lineBuffer = "";
        cursorPos = 0;
        pendingSelectionCount = 0;
        writePrompt();
        return;
    }

    // --- Ctrl+L ---
    if (data === "\x0c") {
        terminal.clear();
        writePrompt();
        terminal.write(lineBuffer);
        return;
    }

    // --- Tab ---
    if (data === "\t") {
        await handleTabCompletion();
        return;
    }

    // --- Ctrl+Backspace (delete word left) ---
    if (data === "\x17") { // Ctrl+W
        if (cursorPos > 0) {
            const newPos = wordBoundaryLeft(lineBuffer, cursorPos);
            lineBuffer = lineBuffer.slice(0, newPos) + lineBuffer.slice(cursorPos);
            cursorPos = newPos;
            redrawLine();
        }
        return;
    }

    // --- Backspace ---
    if (data === "\x7f" || data === "\b") {
        if (cursorPos > 0) {
            lineBuffer = lineBuffer.slice(0, cursorPos - 1) + lineBuffer.slice(cursorPos);
            cursorPos--;
            redrawLine();
        }
        return;
    }

    // --- Delete key ---
    if (data === "\x1b[3~") {
        if (cursorPos < lineBuffer.length) {
            lineBuffer = lineBuffer.slice(0, cursorPos) + lineBuffer.slice(cursorPos + 1);
            redrawLine();
        }
        return;
    }

    // --- Ctrl+Delete (delete word right) ---
    if (data === "\x1b[3;5~") {
        if (cursorPos < lineBuffer.length) {
            const newPos = wordBoundaryRight(lineBuffer, cursorPos);
            lineBuffer = lineBuffer.slice(0, cursorPos) + lineBuffer.slice(newPos);
            redrawLine();
        }
        return;
    }

    // --- Ctrl+Left (word left) ---
    if (data === "\x1b[1;5D") {
        const newPos = wordBoundaryLeft(lineBuffer, cursorPos);
        if (newPos < cursorPos) {
            terminal.write(`\x1b[${cursorPos - newPos}D`);
            cursorPos = newPos;
        }
        return;
    }

    // --- Ctrl+Right (word right) ---
    if (data === "\x1b[1;5C") {
        const newPos = wordBoundaryRight(lineBuffer, cursorPos);
        if (newPos > cursorPos) {
            terminal.write(`\x1b[${newPos - cursorPos}C`);
            cursorPos = newPos;
        }
        return;
    }

    // --- Up arrow ---
    if (data === "\x1b[A") {
        if (history.length === 0) return;
        if (historyIndex === -1) {
            tempLine = lineBuffer;
            historyIndex = history.length - 1;
        } else if (historyIndex > 0) {
            historyIndex--;
        }
        lineBuffer = history[historyIndex];
        cursorPos = lineBuffer.length;
        redrawLine();
        return;
    }

    // --- Down arrow ---
    if (data === "\x1b[B") {
        if (historyIndex === -1) return;
        if (historyIndex < history.length - 1) {
            historyIndex++;
            lineBuffer = history[historyIndex];
        } else {
            historyIndex = -1;
            lineBuffer = tempLine;
        }
        cursorPos = lineBuffer.length;
        redrawLine();
        return;
    }

    // --- Left arrow ---
    if (data === "\x1b[D") {
        if (cursorPos > 0) {
            cursorPos--;
            terminal.write(data);
        }
        return;
    }

    // --- Right arrow ---
    if (data === "\x1b[C") {
        if (cursorPos < lineBuffer.length) {
            cursorPos++;
            terminal.write(data);
        }
        return;
    }

    // --- Home ---
    if (data === "\x1b[H" || data === "\x1b[1~") {
        if (cursorPos > 0) {
            terminal.write(`\x1b[${cursorPos}D`);
            cursorPos = 0;
        }
        return;
    }

    // --- End ---
    if (data === "\x1b[F" || data === "\x1b[4~") {
        if (cursorPos < lineBuffer.length) {
            terminal.write(`\x1b[${lineBuffer.length - cursorPos}C`);
            cursorPos = lineBuffer.length;
        }
        return;
    }

    // Ignore other escape sequences
    if (data.startsWith("\x1b")) return;

    // Regular character input
    for (const ch of data) {
        if (ch >= " ") {
            lineBuffer = lineBuffer.slice(0, cursorPos) + ch + lineBuffer.slice(cursorPos);
            cursorPos++;
        }
    }
    redrawLine();
}

async function handleTabCompletion() {
    if (!lineBuffer.trim()) return;

    try {
        const completions = await dotnetRef.invokeMethodAsync("GetCompletions", lineBuffer);
        if (!completions || completions.length === 0) return;

        if (completions.length === 1) {
            const parts = lineBuffer.split(" ");
            parts[parts.length - 1] = completions[0];
            lineBuffer = parts.join(" ") + " ";
            cursorPos = lineBuffer.length;
            redrawLine();
        } else {
            terminal.writeln("");
            terminal.writeln("  " + completions.join("  "));
            writePrompt();
            terminal.write(lineBuffer);
            cursorPos = lineBuffer.length;
        }
    } catch {
        // Completion failed silently
    }
}

let dragCleanup = null;

export function initDragHandle(splitContainer, dragHandle, terminalPanel) {
    if (dragCleanup) {
        dragCleanup();
        dragCleanup = null;
    }

    if (!dragHandle || !splitContainer || !terminalPanel) return;

    let dragging = false;

    function onMouseDown(e) {
        e.preventDefault();
        dragging = true;
        document.body.style.cursor = "col-resize";
        document.body.style.userSelect = "none";

        const overlay = document.createElement("div");
        overlay.id = "drag-overlay";
        overlay.style.cssText = "position:fixed;inset:0;z-index:9999;cursor:col-resize;";
        document.body.appendChild(overlay);
    }

    function onMouseMove(e) {
        if (!dragging) return;
        const rect = splitContainer.getBoundingClientRect();
        const offset = e.clientX - rect.left;
        const pct = (offset / rect.width) * 100;
        const clamped = Math.min(Math.max(pct, 20), 80);
        terminalPanel.style.flex = `0 0 ${clamped}%`;
        if (fitAddon) fitAddon.fit();
    }

    function onMouseUp() {
        if (!dragging) return;
        dragging = false;
        document.body.style.cursor = "";
        document.body.style.userSelect = "";
        const overlay = document.getElementById("drag-overlay");
        if (overlay) overlay.remove();
        if (fitAddon) fitAddon.fit();
    }

    dragHandle.addEventListener("mousedown", onMouseDown);
    document.addEventListener("mousemove", onMouseMove);
    document.addEventListener("mouseup", onMouseUp);

    dragCleanup = () => {
        dragHandle.removeEventListener("mousedown", onMouseDown);
        document.removeEventListener("mousemove", onMouseMove);
        document.removeEventListener("mouseup", onMouseUp);
    };
}

export function dispose() {
    if (dragCleanup) {
        dragCleanup();
        dragCleanup = null;
    }
    if (terminal) {
        terminal.dispose();
        terminal = null;
    }
    fitAddon = null;
    dotnetRef = null;
}
