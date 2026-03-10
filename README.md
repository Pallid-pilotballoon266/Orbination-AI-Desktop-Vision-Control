# Orbination AI Desktop Vision & Control

[![Release](https://img.shields.io/github/v/release/amichail-1/Orbination-AI-Desktop-Vision-Control?style=flat-square)](https://github.com/amichail-1/Orbination-AI-Desktop-Vision-Control/releases)
[![License: MIT](https://img.shields.io/badge/License-MIT-blue.svg?style=flat-square)](LICENSE)
[![.NET 8](https://img.shields.io/badge/.NET-8.0-purple?style=flat-square)](https://dotnet.microsoft.com/download/dotnet/8.0)
[![MCP](https://img.shields.io/badge/MCP-Compatible-green?style=flat-square)](https://modelcontextprotocol.io)
[![Windows](https://img.shields.io/badge/Platform-Windows%2010%2F11-0078D6?style=flat-square&logo=windows)](https://www.microsoft.com/windows)

**Give AI assistants eyes and hands.** A native Windows MCP server that lets AI see the screen, read UI elements, click buttons, type text, and control any application.

Built for [Claude Code](https://claude.ai/code) by [Leia Enterprise Solutions](https://leia.gr) for the [Orbination](https://orbination.com) project.

> AI coding assistants are blind. They generate code but can never see the result. They can't compare a design mockup to a running app. They can't click through a UI to test it. **This server fixes that.**

## What It Does

This MCP server bridges the gap between AI and your desktop. Instead of working blind with just text, the AI can:

- **See** — Take screenshots of any screen region across multiple monitors
- **Read** — Detect every UI element (buttons, inputs, text, tabs, checkboxes) with exact positions via Windows UIAutomation
- **Interact** — Click elements, type text, fill forms, toggle checkboxes, select tabs — all without fragile coordinate guessing
- **Navigate** — Open apps, switch windows, maximize/minimize, scroll, navigate browser URLs
- **Understand** — Scan the entire desktop to build a structured map of all windows and their contents

## Why

AI coding assistants are blind. They generate code but can never see the result. They can't compare a design mockup to a running app. They can't click through a UI to test it. This server gives them eyes and hands.

## Architecture

```
Claude Code  <──MCP/stdio──>  DesktopControlMcp.exe
                                    │
                                    ├── Win32 API (EnumWindows, window management)
                                    ├── UIAutomation (element detection, interaction)
                                    ├── Native Input (mouse/keyboard simulation)
                                    └── GDI+ (screenshots)
```

Single native .NET 8 executable. No Python. No Node.js. No browser drivers. Direct Windows API access.

## Requirements

- Windows 10/11
- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)

## Build

```bash
cd DesktopControlMcp
dotnet build -c Release
```

Or publish as a single file:

```bash
dotnet publish -c Release -r win-x64 --self-contained false
```

## Setup with Claude Code

Add the MCP server to your Claude Code configuration:

```bash
claude mcp add desktop-control -- "C:\path\to\DesktopControlMcp.exe"
```

Or add it manually to your MCP config file:

```json
{
  "mcpServers": {
    "desktop-control": {
      "command": "C:\\path\\to\\DesktopControlMcp\\bin\\Release\\net8.0-windows\\DesktopControlMcp.exe",
      "args": []
    }
  }
}
```

## Tools

### Vision & Element Detection

| Tool | Description |
|---|---|
| `scan_desktop` | Full desktop scan — screens, windows, UI elements, taskbar |
| `list_windows` | List all visible windows with titles, process names, bounds |
| `get_window_details` | Get all UI elements in a window (filter by kind: button, input, text, etc.) |
| `find_element` | Search for a UI element by text across all windows |
| `read_window_text` | Extract all visible text from a window |
| `refresh_window` | Re-scan a single window's elements (faster than full scan) |

### Interaction

| Tool | Description |
|---|---|
| `click_element` | Find element by text and click it via UIAutomation (reliable, no coordinate guessing) |
| `type_in_element` | Find an input field and type text (ValuePattern, clipboard paste, or click+type fallback) |
| `interact` | Smart interaction — auto-detects element type and performs the right action |
| `fill_form` | Fill multiple form fields in one call with JSON field:value pairs |
| `select_tab` | Select a browser or application tab by text |

### Mouse & Keyboard

| Tool | Description |
|---|---|
| `mouse_click` | Click at screen coordinates |
| `mouse_move` | Move cursor to position |
| `mouse_drag` | Drag from one position to another |
| `mouse_scroll` | Scroll the mouse wheel |
| `keyboard_type` | Type text (supports Unicode) |
| `keyboard_press` | Press a single key |
| `keyboard_hotkey` | Press key combinations (Ctrl+C, Alt+Tab, etc.) |
| `keyboard_key_down` / `keyboard_key_up` | Hold and release keys |

### Window & App Management

| Tool | Description |
|---|---|
| `focus_window` | Bring a window to the foreground |
| `maximize_window` | Maximize a window |
| `minimize_window` | Minimize a window |
| `restore_window` | Restore a minimized/maximized window |
| `open_app` | Open an app by name (focuses existing, clicks taskbar, or searches Start) |
| `navigate_to_url` | Navigate a browser to a URL |

### Screenshots

| Tool | Description |
|---|---|
| `screenshot_to_file` | Full screenshot across all monitors |
| `screenshot_region` | Screenshot a specific screen region |
| `get_screen_info` | Get monitor layout (positions, sizes, primary) |

### Utilities

| Tool | Description |
|---|---|
| `click_and_type` | Click at position then type text |
| `auto_scroll` | Scroll with pauses between batches |
| `wait_seconds` | Pause between actions |

## Multi-Monitor Support

Full multi-monitor support out of the box. The server detects all connected displays and works seamlessly across them:

- **Auto-detects all monitors** — positions, sizes, primary screen via `get_screen_info`
- **Virtual desktop mapping** — coordinates span the full virtual desktop, so the AI can click or screenshot any monitor
- **Cross-monitor screenshots** — `screenshot_to_file` captures all screens, `screenshot_region` targets any region on any display
- **Window-aware** — windows on any monitor are detected with correct positions, even on non-primary displays
- **Taskbar scanning** — reads both `Shell_TrayWnd` (primary) and `Shell_SecondaryTrayWnd` (secondary monitors)

No configuration needed. Plug in a second monitor and the AI sees it immediately.

## How UIAutomation Works

Unlike screenshot-based tools that guess what's on screen, this server reads the actual UI element tree exposed by Windows. Every button, input field, text label, tab, and checkbox is detected with:

- **Exact position and size** (bounding rectangle)
- **Text/label** (what the element says)
- **Control type** (button, input, text, checkbox, etc.)
- **Automation ID** (developer-assigned identifier)
- **Supported patterns** (can it be clicked? typed into? toggled?)

This means the AI can reliably interact with applications without pixel-perfect coordinate matching.

### Limitation: Custom-Rendered Apps

Applications that render their own UI canvas (Flutter, Electron with custom rendering, game engines) may expose fewer or no elements to UIAutomation. For these, the server falls back to screenshot + coordinate-based interaction.

## Token-Efficient by Design

Every MCP tool call costs tokens. This server is engineered to minimize token usage so the AI spends less and does more:

### Structured Data Instead of Screenshots

Most desktop automation tools send full screenshots for every action — each one costs **thousands of tokens** for vision processing. This server returns **compact structured text** instead:

```
[button] "Save" @ 450,320
[input] "Search..." @ 200,60
[tab-item] "Settings" @ 120,35
```

The AI gets exact element positions and types in a few lines of text, not a 1MB image. Screenshots are available when needed but are never the default.

### CacheRequest — Single Cross-Process Call

The scanner uses Windows UIAutomation's `CacheRequest` pattern to batch-fetch all element properties (name, bounds, control type, automation ID, class name) in a **single cross-process call** per window. Without this, each property on each element would be a separate IPC round-trip — hundreds of calls for a single window. With caching, it's one call that returns everything.

### Filtered Element Scanning

Instead of crawling every node in the UI tree (thousands of elements), the scanner uses a pre-built `OrCondition` filter that only matches 14 control types the AI actually cares about: buttons, inputs, text, tabs, checkboxes, links, etc. Decorative containers, panels, and layout elements are skipped entirely — less noise, fewer tokens.

### 30-Second Smart Cache

Scan results are cached for 30 seconds. Multiple tool calls within that window reuse the cached data without re-scanning. Individual windows can be refreshed with `refresh_window` instead of a full `scan_desktop`, saving both time and tokens.

### Compact Output Format

Tool responses use minimal plain-text formatting — no JSON wrappers, no verbose metadata. Element lists show only what the AI needs: `[kind] "text" @ x,y`. Window lists show: `Title (process) [count] @ position`. Every byte of output is a token the AI has to process.

### Kind Filtering & Limits

`get_window_details` supports `kindFilter` (show only buttons, only inputs, etc.) and `limit` (default 30) so the AI can request exactly what it needs. A Chrome window might have 200+ elements — but if the AI only needs input fields, it gets 5 lines instead of 200.

### Batch Operations

`fill_form` fills multiple form fields in a single tool call instead of one call per field. `scan_desktop` returns screens + windows + elements + taskbar in one response. Fewer round-trips = fewer tokens.

## Project Structure

```
DesktopControlMcp/
├── Program.cs                    # MCP server entry point
├── NativeInput.cs                # Low-level mouse/keyboard via SendInput
├── Native/
│   └── Win32.cs                  # P/Invoke: EnumWindows, window management
├── Models/
│   └── SceneData.cs              # Data models: windows, elements, bounds
├── Services/
│   ├── DesktopScanner.cs         # Desktop scanning via Win32 + UIAutomation
│   └── UiAutomationHelper.cs     # Element interaction patterns
└── Tools/
    ├── VisionTools.cs            # scan, find, click, type, form fill
    ├── CompositeTools.cs         # navigate, open app, window management
    ├── MouseTools.cs             # Mouse control
    ├── KeyboardTools.cs          # Keyboard control
    └── ScreenTools.cs            # Screenshots
```

## Examples

See the [`examples/`](examples/) folder for real-world workflows:

- **[Visual UI Comparison](examples/visual-ui-comparison.md)** — AI opens an HTML design and a Flutter app side by side, clicks through both, and identifies every visual difference
- **[Automated UI Testing](examples/automated-testing.md)** — AI tests login flows, form validation, and navigation by clicking through any app — no test scripts needed
- **[Multi-App Workflows](examples/multi-app-workflow.md)** — AI orchestrates across browser, code editor, database tool, and desktop apps in a single workflow

## Quick Install

**Option A: Download pre-built binary**

1. Download from [Releases](https://github.com/amichail-1/Orbination-AI-Desktop-Vision-Control/releases)
2. Extract the zip
3. Add to Claude Code:
```bash
claude mcp add desktop-control -- "C:\path\to\DesktopControlMcp.exe"
```

**Option B: Build from source**

```bash
git clone https://github.com/amichail-1/Orbination-AI-Desktop-Vision-Control.git
cd Orbination-AI-Desktop-Vision-Control/DesktopControlMcp
dotnet build -c Release
claude mcp add desktop-control -- "bin\Release\net8.0-windows\DesktopControlMcp.exe"
```

## Contributing

Contributions welcome. Open an issue or submit a PR.

## License

MIT
