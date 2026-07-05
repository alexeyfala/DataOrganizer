# Publishing Recipes

Cheat-sheet for producing Data Organizer builds on each platform.

> **Before any Release / distributable build — regenerate third-party license notices.**
> The notice set is platform-independent, so running this once per release is enough; commit the result.
> Restore first so `project.assets.json` is current. On Windows use `powershell`; on Linux/macOS use `pwsh`.
>
> ```powershell
> dotnet restore
> powershell -NoProfile -ExecutionPolicy Bypass -File tools\gen-third-party-notices.ps1
> ```
>
> Review the console summary (watch for any `UNKNOWN` licenses) and commit the updated `THIRD-PARTY-NOTICES.txt`.

---

## Windows

### Installer (`.msi` + `.exe`)

1\. Regenerate notices (see above):

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File tools\gen-third-party-notices.ps1
```

2\. Set **Release** or **Debug** mode.

3\. Build the **Setup** project → produces the `*.msi` file.

4\. Build the **Bundle** project → produces the `*.exe` file.

```powershell
start "..\Publish"
```

### Portable — Release

Open a terminal in the `DataOrganizer.Desktop` project. Regenerate notices first:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File ..\tools\gen-third-party-notices.ps1
dotnet publish -c:Release -p:PublishSingleFile=true -r:win-x64 --self-contained true -p:IncludeNativeLibrariesForSelfExtract=true -p:EnableCompressionInSingleFile=true -p:DebugSymbols=false -verbosity:diag -p:PublishDir="..\Publish"
Remove-Item -Path "..\Publish\*.xml", "..\Publish\*.pdb"
start "..\Publish"
```

### Portable — Debug

Open a terminal in the `DataOrganizer.Desktop` project.

```powershell
dotnet publish -c:Debug -p:PublishSingleFile=true -r:win-x64 --self-contained true -p:IncludeNativeLibrariesForSelfExtract=true -p:EnableCompressionInSingleFile=true -p:DebugSymbols=false -verbosity:diag -p:PublishDir="..\Publish"
Remove-Item -Path "..\Publish\*.xml", "..\Publish\*.pdb"
start "..\Publish"
```

---

## Linux

### Portable — Release

Open a terminal in the `DataOrganizer.Desktop` project. Regenerate notices first:

```powershell
pwsh ../tools/gen-third-party-notices.ps1
dotnet publish -c:Release -p:PublishSingleFile=true -r:linux-x64 --self-contained true -p:IncludeNativeLibrariesForSelfExtract=true -p:EnableCompressionInSingleFile=true -p:DebugSymbols=false -verbosity:diag -p:PublishDir="..\Publish"
Remove-Item -Path "..\Publish\*.xml", "..\Publish\*.pdb"
start "..\Publish"
```

### Portable — Debug

Open a terminal in the `DataOrganizer.Desktop` project.

```powershell
dotnet publish -c:Debug -p:PublishSingleFile=true -r:linux-x64 --self-contained true -p:IncludeNativeLibrariesForSelfExtract=true -p:EnableCompressionInSingleFile=true -p:DebugSymbols=false -verbosity:diag -p:PublishDir="..\Publish"
Remove-Item -Path "..\Publish\*.xml", "..\Publish\*.pdb"
start "..\Publish"
```

### Native installers — `.deb` / `.rpm` / AppImage / Flatpak (PupNet)

A separate staged workflow that runs in WSL/Ubuntu. Follow the stages in order:

- [Stage 1 — Prepare the Linux build environment (WSL)](PupNet_Instructions/1_Setup.md)
- [Stage 2 — Generate the PupNet configuration](PupNet_Instructions/2_Config.md)
- [Stage 3 — AppStream metainfo (`.metainfo.xml`)](PupNet_Instructions/3_AppStream.md)
- [Stage 4 — Build and verify Linux packages](PupNet_Instructions/4_Build.md)

---

## macOS

> **These recipes produce a `.pkg` installer.**
>
> The `rm -rf bin` step is required, not just cleanup: the build leaves a copy of `DataOrganizer.app` under
> `bin`, and macOS LaunchServices registers that copy instead of the one the `.pkg` installs into
> `/Applications`. Until `bin` is removed the installed app is not discoverable in Finder/Launchpad and cannot
> be uninstalled by dragging it to Trash.

### Release

Open a terminal in macOS. Regenerate notices first:

```bash
cd DataOrganizer.MacOS
pwsh ../tools/gen-third-party-notices.ps1
dotnet publish -c:Release -r:osx-x64 -p:UseAppHost=true -verbosity:diag -p:PublishDir="../Publish"
rm -rf bin
cd ../Publish
open .
```

### Debug

Open a terminal in macOS.

```bash
cd DataOrganizer.MacOS
dotnet publish -c:Debug -r:osx-x64 -p:UseAppHost=true -verbosity:diag -p:PublishDir="../Publish"
rm -rf bin
cd ../Publish
open .
```
