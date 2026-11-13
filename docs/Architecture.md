# Architecture

## Repository layout

- `.config/` — local .NET tools.
- `.github/` — CI workflow, issue and pull request templates, community files.
- `.vscode/` — VS Code settings.
- `artifacts/` — build output of the installers and packages; not tracked.
- `build/` — build scripts and the inputs of the Linux packages.
- `docs/` — documentation.
- `src/` — the projects of the application and its installers.
- `tests/` — the test projects.

## Projects

The projects of `src/`, in the order of the Solution Explorer:

- `AvaloniaEdit.Localization` — the Russian satellite of the AvaloniaEdit search panel.
- `Bundle` — the installer that carries the .NET runtime and the `.msi`.
- `DataOrganizer` — the user interface: views, view models, controls, styles.
- `DataOrganizer.Database` — the stored data.
  - `Entities` — the entities of the database.
  - `Repository` — the database context, its access and the repositories.
- `DataOrganizer.Database.Migrations` — the migrations of the database.
- `DataOrganizer.Desktop` — the host of the application on Windows and Linux.
- `DataOrganizer.Documents` — the texts of files, notes and the notepad.
  - `DatasetRecords` — the records of datasets.
  - `Editor` — the documents of the editor and their state.
  - `FileContents` — reading and writing the contents of files.
  - `Notepad` — the tabs of the notepad.
  - `Notes` — the notes of the objects of the tree.
  - `TextProcessing` — encodings, transformations, folding and highlighting of texts.
- `DataOrganizer.Hierarchy` — the tree of folders, files and datasets.
  - `Favorites` — the favorite files.
  - `FilteredLists` — filtered views of collections.
  - `Storage` — loading the tree and writing its changes.
- `DataOrganizer.Infrastructure` — the common services of the application.
  - `AppInstance` — the running instance: command line, environment, shutdown.
  - `AppUpdates` — the check for a new version.
  - `Diagnostics` — logging and error handling.
  - `DialogContracts` — showing dialogs.
  - `FileSystemOperations` — folders on disk and the file manager of the system.
  - `Notifications` — messages and progress shown to the user.
  - `Settings` — the settings of the application and its windows.
  - `WindowContracts` — opening and closing windows.
- `DataOrganizer.MacOS` — the host of the application on macOS.
- `DataOrganizer.Security` — encryption of contents, passwords and keys.
- `DataOrganizer.Shared` — helpers, constants and interface strings used across the projects.
  - `Threading` — the UI thread, timers and locks.
- `DataOrganizer.SystemIntegration` — the parts of the operating system the application works with.
  - `Clipboard` — the clipboard and its history.
  - `FileExecution` — opening files in the programs of the system and taking back their changes.
  - `GlobalHotkeys` — system-wide hotkeys.
- `DataOrganizer.Workflows` — the operations that span several topics.
  - `AppLifecycle` — start, background services and exit of the application.
  - `HierarchyExchange` — export and import of the tree.
  - `HierarchyScenarios` — deletion and protection of objects of the tree.
  - `SampleSeeding` — sample data for an empty database.
- `Setup` — the `.msi` installer.
- `Setup.CustomActions` — the custom actions of the `.msi`.

## References

The projects below the user interface form one chain; each project references only the one before it and
sees the rest through it:

```
Shared ← Database ← Infrastructure ← Hierarchy ← Security ← Documents ← SystemIntegration ← Workflows
                  ↖ Database.Migrations
```

`DataOrganizer` references `DataOrganizer.Workflows`, `DataOrganizer.Database.Migrations` and
`AvaloniaEdit.Localization`; the hosts reference `DataOrganizer`.

## Folders inside a project

A project holds a folder per topic and, inside it, a folder per kind (`Dto`, `Interfaces`, `Services` and
so on). The topic that names the project lies at the project root without a folder of its own. The
namespace follows the folders: `<Project>.<Topic>.<Kind>`.

The topics of a project have an order: a topic may use only the topics before it. The guard test
`TopicOrderTests` holds the order and checks it.

A project with tests has its test project in `tests/`, named `<Project>.UnitTests`, and
`<Project>.IntegrationTests` for tests that need a database file on the disk; a test on a database in
memory stays a unit test. Its folders mirror the project: `tests/<Project>.UnitTests/<Topic>/<Kind>`.
