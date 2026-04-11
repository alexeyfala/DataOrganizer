# Data Organizer

**A cross-platform desktop application for organizing, managing, and securely storing structured data in a virtual file system.**

Built with [Avalonia UI](https://avaloniaui.net/) and .NET 10, Data Organizer provides a hierarchical workspace where you can create files, folders, and datasets — encrypt sensitive content, assign global hotkeys for file contents, and keep everything organized in one place.

---

## Why Data Organizer?

Managing scattered notes, credentials, code snippets, configuration fragments, and structured records across multiple tools is painful. Data Organizer solves this by providing a single, local-first application with:

- A **virtual file system** — no files on disk to lose track of; everything lives in a local SQLite database
- **Folder-level encryption** — protect sensitive content with a password, [XChaCha20-Poly1305](https://github.com/ektrah/nsec/blob/master/docs/api/nsec.cryptography.aeadalgorithm.md#xchacha20poly1305) encryption keeps your data safe at rest
- **Global hotkeys** — bind file contents to a keyboard shortcut and copy its contents to the clipboard instantly, even when the app is in the background
- **Datasets** — go beyond plain text with structured key-value records and a built-in editor
- **Favorites & clipboard history** — quick access to frequently used items and a full history of what you've copied

---

## Features

### Virtual File System
Data is stored in a tree structure of **folders**, **files**, **datasets** and is automatically saved in a local SQLite database.

### Global Hotkeys
Assigning keyboard shortcuts (with Ctrl / Alt / Shift modifiers) to files. Press the hotkey from any application and the file's contents are instantly copied to your clipboard. Perfect for templates, boilerplate text, or any content you paste frequently.

### Favorites
Allows to mark files as favorites for quick access. The dedicated **Favorites window** groups them by parent folder and provides a focused, searchable interface.

### Encryption
Folders can be protected with password-based encryption. Passwords are hashed with [BCrypt](https://github.com/BcryptNet/bcrypt.net), file contents are encrypted using [XChaCha20-Poly1305](https://github.com/ektrah/nsec/blob/master/docs/api/nsec.cryptography.aeadalgorithm.md#xchacha20poly1305) with a per-folder Data Encryption Key (DEK).

### Datasets
Represents structured records with key-value pairs. The built-in dataset editor lets you manage, group, and edit records.

### Clipboard History
Every copy of entire file contents operation is tracked. Browse, search, and restore previous clipboard entries from the history panel.

### File Execution
Allows to launch files with OS-default applications directly from Data Organizer. Execution history is tracked for easy reference.

### Import & Export
Import and export of data using JSON, XML and entire SQLite database is provided.

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
| **Architecture** | MVVM ([CommunityToolkit.Mvvm](https://github.com/CommunityToolkit/dotnet)) |
| **Database** | SQLite via Entity Framework Core (Table-Per-Concrete-Type) |
| **Encryption** | [XChaCha20-Poly1305](https://github.com/ektrah/nsec/blob/master/docs/api/nsec.cryptography.aeadalgorithm.md#xchacha20poly1305), [BCrypt](https://github.com/BcryptNet/bcrypt.net) password hashing |
| **Global Hotkeys** | [SharpHook](https://github.com/TolikPylypchuk/SharpHook) — cross-platform keyboard hooks |
| **Mapping** | [Mapster](https://github.com/MapsterMapper/Mapster) (Entity <-> DTO) |
| **Logging** | [Serilog](https://github.com/serilog/serilog) with daily rolling file sink |
| **Text Editor** | [AvaloniaEdit](https://github.com/AvaloniaUI/AvaloniaEdit) |

---

## Project Structure

```
App/
├── DataOrganizer/             # Main UI library (Views, ViewModels, Services)
├── DataOrganizer.Desktop/     # Windows, Linux desktop host
├── DataOrganizer.MacOS/       # macOS host
├── Entities/                  # Domain models & database entities
├── Repository/                # Data access layer (EF Core repositories)
└── Shared/                    # Cross-cutting utilities & localization
```

---

## Getting Started

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
%LOCALAPPDATA%/DataOrganizer/                                 (Windows)
/home/{username}/.local/share/DataOrganizer/                  (Linux)
/Users/{username}/Library/Application Support/DataOrganizer/  (macOS)
```

---

## TODO:
- Search by name in the main list
- Search by file contents
- Ability to select multiple objects in a list
- Importing file system objects into an application
- Exporting objects from an application to the file system
- Cloud synchronization
- And others ...

---

## License

This project is provided as-is. See the [LICENSE](LICENSE) file for details.