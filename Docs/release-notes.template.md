## Data Organizer {version}

<!-- 2–4 plain-language highlights of what changed in this release. -->

### Downloads

| Platform | Installer | Portable |
|---|---|---|
| Windows (x64) | `DataOrganizer-{version}-win-x64.msi` | `DataOrganizer-{version}-win-x64-portable.zip` |
| Linux (x64) | `dataorganizer_{version}-1_amd64.deb` | `DataOrganizer-{version}-1.x86_64.AppImage` |
| macOS (x64) | `DataOrganizer-{version}-osx-x64.dmg` | `DataOrganizer-{version}-osx-x64-portable.zip` |

The portable builds and the Linux and macOS packages are self-contained — the .NET runtime is bundled, no separate install required. The Windows `.msi` needs the [.NET 10 Runtime (x64)](https://dotnet.microsoft.com/download/dotnet/10.0) installed first.
(The `-1` in the Linux file names is the package revision; bump it when re-packaging the same version.)

### Install & run

**Windows** — run the `.msi`, or unzip the portable archive and launch `DataOrganizer.exe`. The build is unsigned, so SmartScreen may warn: *More info → Run anyway*.

**Linux** — `.deb`: `sudo apt install ./dataorganizer_{version}-1_amd64.deb`. AppImage: `chmod +x DataOrganizer-{version}-1.x86_64.AppImage` then run it (on hosts without `libfuse2`, add `--appimage-extract-and-run`).

**macOS** — the app is not signed or notarized, so Gatekeeper blocks it on first launch. Right-click the app → **Open** → **Open**, or clear the quarantine flag: `xattr -dr com.apple.quarantine DataOrganizer.app`. From the `.dmg`: drag the app to Applications. Portable: unzip and run the `.app`. On Apple Silicon (ARM64) it runs through Rosetta 2, up to macOS 27. An upgrade to macOS 27 before 27.2 removes Rosetta 2: macOS offers to install it again when the app starts, or `softwareupdate --install-rosetta --agree-to-license` does it.

### Notes

- x64 only in this release.
- `*.sha256.txt` files are provided for the Linux packages to verify downloads (`sha256sum -c`).
