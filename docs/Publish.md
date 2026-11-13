# Publishing Recipes

Cheat-sheet for producing Data Organizer builds on each platform.

> **Before any Release / distributable build — regenerate third-party license notices.**
> The notice set is platform-independent, so running this once per release is enough; commit the result.
> Restore first so `project.assets.json` is current.
>
> ```powershell
> dotnet restore
> powershell -NoProfile -ExecutionPolicy Bypass -File build\gen-third-party-notices.ps1
> ```
>
> Review the console summary (watch for any `UNKNOWN` licenses) and commit the updated `THIRD-PARTY-NOTICES.txt`.

---

## Windows

### Installer (`.msi` + `.exe`)

1\. Set **Release** or **Debug** mode.

2\. Build the **Setup** project → produces the `*.msi` file.

3\. Build the **Bundle** project → produces the `*.exe` file.

### Updating the .NET runtime in the bundle

The bundle installs the .NET Runtime from `src\Bundle\Resources\`. For a new 10.0 patch:

1\. Download the x64 Windows installer of the .NET Runtime (`dotnet-runtime-<version>-win-x64.exe`)
from <https://dotnet.microsoft.com/en-us/download/dotnet/10.0>.

2\. Put it into `src\Bundle\Resources\` and delete the old installer.

3\. Set `DotNetVersion` in `src\Bundle\Bundle.wixproj` to the new version.

### Portable — Release

Open a terminal in the `DataOrganizer.Desktop` project. Regenerate notices first:

```powershell
dotnet publish -c:Release -p:PublishSingleFile=true -r:win-x64 --self-contained true -p:IncludeNativeLibrariesForSelfExtract=true -p:EnableCompressionInSingleFile=true -p:DebugSymbols=false -verbosity:diag -p:PublishDir="..\..\artifacts"
$name = (dotnet msbuild DataOrganizer.Desktop.csproj -getProperty:PortableName -p:RuntimeIdentifier=win-x64 -p:Configuration=Release).Trim()
Compress-Archive -Path "..\..\artifacts\$name.exe","..\..\artifacts\LICENSE","..\..\artifacts\NOTICE","..\..\artifacts\THIRD-PARTY-NOTICES.txt" -DestinationPath "..\..\artifacts\$name.zip" -Force
Remove-Item "..\..\artifacts\$name.exe","..\..\artifacts\LICENSE","..\..\artifacts\NOTICE","..\..\artifacts\THIRD-PARTY-NOTICES.txt"
explorer "/select,$(Resolve-Path ..\..\artifacts\$name.zip)"
```

### Portable — Debug

Open a terminal in the `DataOrganizer.Desktop` project.

```powershell
dotnet publish -c:Debug -p:PublishSingleFile=true -r:win-x64 --self-contained true -p:IncludeNativeLibrariesForSelfExtract=true -p:EnableCompressionInSingleFile=true -p:DebugSymbols=false -verbosity:diag -p:PublishDir="..\..\artifacts"
$name = (dotnet msbuild DataOrganizer.Desktop.csproj -getProperty:PortableName -p:RuntimeIdentifier=win-x64 -p:Configuration=Debug).Trim()
Compress-Archive -Path "..\..\artifacts\$name.exe","..\..\artifacts\LICENSE","..\..\artifacts\NOTICE","..\..\artifacts\THIRD-PARTY-NOTICES.txt" -DestinationPath "..\..\artifacts\$name.zip" -Force
Remove-Item "..\..\artifacts\$name.exe","..\..\artifacts\LICENSE","..\..\artifacts\NOTICE","..\..\artifacts\THIRD-PARTY-NOTICES.txt"
explorer "/select,$(Resolve-Path ..\..\artifacts\$name.zip)"
```

---

## Linux

### Portable — Release

Open a terminal in the `DataOrganizer.Desktop` project. Regenerate notices first:

```powershell
dotnet publish -c:Release -p:PublishSingleFile=true -r:linux-x64 --self-contained true -p:IncludeNativeLibrariesForSelfExtract=true -p:EnableCompressionInSingleFile=true -p:DebugSymbols=false -verbosity:diag -p:PublishDir="..\..\artifacts"
$name = (dotnet msbuild DataOrganizer.Desktop.csproj -getProperty:PortableName -p:RuntimeIdentifier=linux-x64 -p:Configuration=Release).Trim()
tar -czf "../../artifacts/$name.tar.gz" -C ../../artifacts "$name" LICENSE NOTICE THIRD-PARTY-NOTICES.txt
Remove-Item "../../artifacts/$name","../../artifacts/LICENSE","../../artifacts/NOTICE","../../artifacts/THIRD-PARTY-NOTICES.txt"
explorer "/select,$(Resolve-Path ..\..\artifacts\$name.tar.gz)"
```

On Linux, make the unpacked file executable: an archive made on Windows carries no execute bit.

```bash
chmod +x DataOrganizer-*
```

### Portable — Debug

Open a terminal in the `DataOrganizer.Desktop` project.

```powershell
dotnet publish -c:Debug -p:PublishSingleFile=true -r:linux-x64 --self-contained true -p:IncludeNativeLibrariesForSelfExtract=true -p:EnableCompressionInSingleFile=true -p:DebugSymbols=false -verbosity:diag -p:PublishDir="..\..\artifacts"
$name = (dotnet msbuild DataOrganizer.Desktop.csproj -getProperty:PortableName -p:RuntimeIdentifier=linux-x64 -p:Configuration=Debug).Trim()
tar -czf "../../artifacts/$name.tar.gz" -C ../../artifacts "$name" LICENSE NOTICE THIRD-PARTY-NOTICES.txt
Remove-Item "../../artifacts/$name","../../artifacts/LICENSE","../../artifacts/NOTICE","../../artifacts/THIRD-PARTY-NOTICES.txt"
explorer "/select,$(Resolve-Path ..\..\artifacts\$name.tar.gz)"
```

On Linux, make the unpacked file executable: an archive made on Windows carries no execute bit.

```bash
chmod +x DataOrganizer-*
```

### Native installers — `.deb` / `.rpm` / AppImage / Flatpak (PupNet)

A separate staged workflow that runs in WSL/Ubuntu. Follow the stages in order:

- [Stage 1 — Prepare the Linux build environment (WSL)](PupNet_Instructions/1_Setup.md)
- [Stage 2 — Generate the PupNet configuration](PupNet_Instructions/2_Config.md)
- [Stage 3 — AppStream metainfo (`.metainfo.xml`)](PupNet_Instructions/3_AppStream.md)
- [Stage 4 — Build and verify Linux packages](PupNet_Instructions/4_Build.md)

---

## macOS

Three output formats: a `.dmg` (recommended for end users — drag-to-Applications, uninstall by dragging to Trash), a `.pkg` installer, and a portable `.zip` (the `.app` bundle archived as-is — no installer, runs from anywhere).

> The `rm -rf bin` step is required, not just cleanup: the build leaves a copy of `DataOrganizer.app` under
> `bin`, which macOS LaunchServices may register instead of the copy installed into `/Applications`. Until
> `bin` is removed the installed app is not discoverable in Finder/Launchpad and cannot be uninstalled by
> dragging it to Trash.

### `.dmg` — Release

Open a terminal in macOS. Regenerate notices first:

```bash
cd src/DataOrganizer.MacOS
dotnet publish -c:Release -r:osx-x64 -p:UseAppHost=true -p:CreatePackage=false -verbosity:diag
NAME=$(dotnet msbuild DataOrganizer.MacOS.csproj -getProperty:MacInstallerName -p:Configuration=Release)
cd ../..
mkdir -p artifacts/dmg-staging
cp -R "src/DataOrganizer.MacOS/bin/Release/net10.0-macos/osx-x64/DataOrganizer.app" artifacts/dmg-staging/
ln -s /Applications artifacts/dmg-staging/Applications
hdiutil create -volname "Data Organizer" -srcfolder artifacts/dmg-staging -ov -format UDZO "artifacts/$NAME.dmg"
rm -rf artifacts/dmg-staging
rm -rf src/DataOrganizer.MacOS/bin
open -R "artifacts/$NAME.dmg"
```

### `.dmg` — Debug

Open a terminal in macOS.

```bash
cd src/DataOrganizer.MacOS
dotnet publish -c:Debug -r:osx-x64 -p:UseAppHost=true -p:CreatePackage=false -verbosity:diag
NAME=$(dotnet msbuild DataOrganizer.MacOS.csproj -getProperty:MacInstallerName -p:Configuration=Debug)
cd ../..
mkdir -p artifacts/dmg-staging
cp -R "src/DataOrganizer.MacOS/bin/Debug/net10.0-macos/osx-x64/DataOrganizer.app" artifacts/dmg-staging/
ln -s /Applications artifacts/dmg-staging/Applications
hdiutil create -volname "Data Organizer" -srcfolder artifacts/dmg-staging -ov -format UDZO "artifacts/$NAME.dmg"
rm -rf artifacts/dmg-staging
rm -rf src/DataOrganizer.MacOS/bin
open -R "artifacts/$NAME.dmg"
```

### `.pkg` — Release

Open a terminal in macOS. Regenerate notices first:

```bash
cd src/DataOrganizer.MacOS
NAME=$(dotnet msbuild DataOrganizer.MacOS.csproj -getProperty:MacInstallerName -p:Configuration=Release)
dotnet publish -c:Release -r:osx-x64 -p:UseAppHost=true -verbosity:diag -p:PublishDir="../../artifacts" -p:PkgPackagePath="../../artifacts/$NAME.pkg"
rm -rf bin
open -R "../../artifacts/$NAME.pkg"
```

### `.pkg` — Debug

Open a terminal in macOS.

```bash
cd src/DataOrganizer.MacOS
NAME=$(dotnet msbuild DataOrganizer.MacOS.csproj -getProperty:MacInstallerName -p:Configuration=Debug)
dotnet publish -c:Debug -r:osx-x64 -p:UseAppHost=true -verbosity:diag -p:PublishDir="../../artifacts" -p:PkgPackagePath="../../artifacts/$NAME.pkg"
rm -rf bin
open -R "../../artifacts/$NAME.pkg"
```

### Portable (`.zip`) — Release

Open a terminal in macOS. Regenerate notices first:

```bash
cd src/DataOrganizer.MacOS
dotnet publish -c:Release -r:osx-x64 -p:UseAppHost=true -p:CreatePackage=false -verbosity:diag
NAME=$(dotnet msbuild DataOrganizer.MacOS.csproj -getProperty:OutputBaseName)-osx-x64-portable
cd ../..
mkdir -p artifacts
ditto -c -k --keepParent "src/DataOrganizer.MacOS/bin/Release/net10.0-macos/osx-x64/DataOrganizer.app" "artifacts/$NAME.zip"
rm -rf src/DataOrganizer.MacOS/bin
open -R "artifacts/$NAME.zip"
```

### Portable (`.zip`) — Debug

Open a terminal in macOS.

```bash
cd src/DataOrganizer.MacOS
dotnet publish -c:Debug -r:osx-x64 -p:UseAppHost=true -p:CreatePackage=false -verbosity:diag
NAME=$(dotnet msbuild DataOrganizer.MacOS.csproj -getProperty:OutputBaseName)-osx-x64-portable-debug
cd ../..
mkdir -p artifacts
ditto -c -k --keepParent "src/DataOrganizer.MacOS/bin/Debug/net10.0-macos/osx-x64/DataOrganizer.app" "artifacts/$NAME.zip"
rm -rf src/DataOrganizer.MacOS/bin
open -R "artifacts/$NAME.zip"
```

### Second instance

Opening `DataOrganizer.app` again from Finder, the Dock or with plain `open` only brings the running instance to the front: LaunchServices keeps one instance per app. `open -n` starts another one; its window title carries the instance number, such as `Data Organizer (2)`.

Open a terminal in macOS.

```bash
open -n DataOrganizer.app
```
