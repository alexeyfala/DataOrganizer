# DataOrganizer

**A cross-platform desktop application for organizing, managing, and securely storing structured data in a virtual file system.**

Built with [Avalonia UI](https://avaloniaui.net/) and .NET 10, DataOrganizer provides a rich hierarchical workspace where you can create files, folders, and datasets — encrypt sensitive content, assign global hotkeys for instant clipboard access, and keep everything organized in one place.

---

## Why DataOrganizer?

Managing scattered notes, credentials, code snippets, configuration fragments, and structured records across multiple tools is painful. DataOrganizer solves this by providing a single, local-first application with:

- A **virtual file system** — no files on disk to lose track of; everything lives in a local SQLite database
- **Folder-level encryption** — protect sensitive content with a password; AES-256 encryption keeps your data safe at rest
- **Global hotkeys** — bind any file to a keyboard shortcut and copy its contents to the clipboard instantly, even when the app is in the background
- **Datasets** — go beyond plain text with structured key-value records and a built-in editor
- **Favorites & clipboard history** — quick access to frequently used items and a full history of what you've copied

---

## Features

### Virtual File System
Organize your data in a tree of **folders**, **files**, and **datasets**. Create nested hierarchies, rename, move, and annotate items with notes. Everything is persisted automatically in a local SQLite database.

### Encryption
Protect entire folders with password-based encryption. Passwords are hashed with **BCrypt**; file contents are encrypted using **AES-256** with a per-folder Data Encryption Key (DEK). Decryption requires the correct password — no backdoors.

### Global Hotkeys
Assign keyboard shortcuts (with Ctrl / Alt / Shift modifiers) to any file. Press the hotkey from any application and the file's contents are instantly copied to your clipboard. Perfect for passwords, templates, boilerplate text, or any content you paste frequently.

### Datasets
Create structured records with key-value pairs. The built-in dataset editor lets you manage, group, and edit records without leaving the application.

### Favorites
Mark files as favorites for quick access. The dedicated **Favorites window** groups them by parent folder and provides a focused, searchable interface.

### Clipboard History
Every copy operation is tracked. Browse, search, and restore previous clipboard entries from the history panel.

### File Execution
Launch files with OS-default applications directly from DataOrganizer. Execution history is tracked for easy reference.

### Import & Export
Exchange data between instances or back up your workspace using **JSON** and **XML** serialization.

### Customizable Appearance
Choose between **Light**, **Dark**, or **System-inherited** themes powered by Material Design. Customize primary and secondary accent colors to match your preference.

### Localization
Available in **English** and **Russian**, with resource-based localization ready for additional languages.

---

## Tech Stack

| Layer | Technology |
|-------|-----------|
| **UI Framework** | [Avalonia UI](https://avaloniaui.net/) 11.x with [Material Design](https://github.com/AvaloniaCommunity/Material.Avalonia) theme |
| **Runtime** | .NET 10 |
| **Architecture** | MVVM ([CommunityToolkit.Mvvm](https://learn.microsoft.com/dotnet/communitytoolkit/mvvm/)) |
| **Database** | SQLite via Entity Framework Core (Table-Per-Concrete-Type) |
| **Encryption** | AES-256 ([NSec](https://nsec.rocks/)), BCrypt password hashing |
| **Global Hotkeys** | [SharpHook](https://github.com/TolikPyl662/SharpHook) — cross-platform keyboard hooks |
| **Reactive** | System.Reactive, DynamicData |
| **Mapping** | [Mapster](https://github.com/MapsterMapper/Mapster) (Entity <-> DTO) |
| **Logging** | [Serilog](https://serilog.net/) with daily rolling file sink |
| **Text Editor** | [AvaloniaEdit](https://github.com/AvaloniaUI/AvaloniaEdit) |

---

## Project Structure

```
DataOrganizerAvaloniaApp/
├── DataOrganizer/             # Main UI library (Views, ViewModels, Services)
├── DataOrganizer.Desktop/     # Windows desktop host
├── DataOrganizer.MacOS/       # macOS host
├── Entities/                  # Domain models & database entities
├── Repository/                # Data access layer (EF Core repositories)
└── Shared/                    # Cross-cutting utilities & localization
```

---

## Getting Started

### Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download) or later

### Build & Run

```bash
git clone https://github.com/<your-username>/DataOrganizerAvaloniaApp.git
cd DataOrganizerAvaloniaApp

# Windows
dotnet run --project DataOrganizer.Desktop

# macOS
dotnet run --project DataOrganizer.MacOS
```

### Command-Line Options

| Flag | Description |
|------|-------------|
| `--console` | Show the debug console window |
| `--debug` | Enable verbose debug logging |
| `--help` | Display available options |

---

## Data Storage

All application data is stored locally:

```
%APPDATA%/DataOrganizer/       (Windows)
~/.config/DataOrganizer/       (Linux / macOS)
├── DataOrganizer.db           # SQLite database
├── AppSettings.json           # User preferences
├── Logs/                      # Daily rolling log files
└── Backups/                   # Auto-generated backups
```

---

## Architecture

DataOrganizer follows the **MVVM** pattern with clear separation of concerns:

- **Views** — Avalonia XAML user controls and windows
- **ViewModels** — Observable state and commands via CommunityToolkit.Mvvm
- **Models / Entities** — Domain objects mapped to SQLite via EF Core (TPC strategy)
- **Repositories** — Generic and specialized data access abstractions
- **Services** — Business logic (encryption, hotkeys, clipboard, file I/O, settings)
- **Dependency Injection** — Microsoft.Extensions.DependencyInjection wires everything together

---

## License

This project is provided as-is. See the [LICENSE](LICENSE) file for details.
