## Data Organizer {version}

<!-- 2–4 plain-language highlights of what changed in this release. -->

### Downloads

| Platform | Installer | Portable |
|---|---|---|
| Windows (x64) | `DataOrganizer-{version}-win-x64.msi` | `DataOrganizer-{version}-win-x64-portable.zip` |
| macOS (x64) | `DataOrganizer-{version}-osx-x64.dmg` | `DataOrganizer-{version}-osx-x64-portable.zip` |
| Linux (x64) | `dataorganizer_{version}-1_amd64.deb` | `DataOrganizer-{version}-1.x86_64.AppImage` |

All builds are self-contained — the .NET runtime is bundled, no separate install required.
(The `-1` in the Linux file names is the package revision; bump it when re-packaging the same version.)

### Install & run

**Windows** — run the `.msi`, or unzip the portable archive and launch `DataOrganizer.exe`. The build is unsigned, so SmartScreen may warn: *More info → Run anyway*.

**macOS** — the app is not signed or notarized, so Gatekeeper blocks it on first launch. Right-click the app → **Open** → **Open**, or clear the quarantine flag: `xattr -dr com.apple.quarantine DataOrganizer.app`. From the `.dmg`: drag the app to Applications. Portable: unzip and run the `.app`. Runs on Apple Silicon via Rosetta 2.

**Linux** — `.deb`: `sudo apt install ./dataorganizer_{version}-1_amd64.deb`. AppImage: `chmod +x DataOrganizer-{version}-1.x86_64.AppImage` then run it (on hosts without `libfuse2`, add `--appimage-extract-and-run`).

### Notes

- x64 only in this release.
- `*.sha256.txt` files are provided for the Linux packages to verify downloads (`sha256sum -c`).
