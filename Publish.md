# Publishing Recipes

Cheat-sheet for producing Data Organizer builds on each platform.

> **Before any Release / distributable build — regenerate third-party license notices.**
> The notice set is platform-independent, so running this once per release is enough; commit the result.
> Requires PowerShell (on Linux/macOS use `pwsh`). Restore first so `project.assets.json` is current.
>
> ```powershell
> dotnet restore
> pwsh tools/gen-third-party-notices.ps1
> ```
>
> Review the console summary (watch for any `UNKNOWN` licenses) and commit the updated `THIRD-PARTY-NOTICES.txt`.

---

## Windows

### Installer (`.msi` + `.exe`)

1\. Regenerate notices (see above):

```powershell
pwsh tools/gen-third-party-notices.ps1
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
pwsh ..\tools\gen-third-party-notices.ps1
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

---

## macOS

### Release

Open a terminal in macOS. Regenerate notices first:

```bash
cd DataOrganizer.MacOS
pwsh ../tools/gen-third-party-notices.ps1
dotnet publish -c:Release -r:osx-x64 -p:UseAppHost=true -verbosity:diag -p:PublishDir="../Publish"
cd ../Publish
```

### Debug

Open a terminal in macOS.

```bash
cd DataOrganizer.MacOS
dotnet publish -c:Debug -r:osx-x64 -p:UseAppHost=true -verbosity:diag -p:PublishDir="../Publish"
cd ../Publish
```
