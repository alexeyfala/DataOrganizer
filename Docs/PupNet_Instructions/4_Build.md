# Stage 4 — Build and verify Linux packages

> **Prerequisite:** `app.pupnet.conf` is filled in and the icons exist in `IconFiles`. Builds run in WSL (Ubuntu) and need internet access (`dotnet publish` pulls the `linux-x64` runtime and NuGet packages).

---

## Linux package formats PupNet can build

PupNet builds several Linux formats from the same config. Each is selected with the `-k` flag and has its own settings section in `app.pupnet.conf`.

| `-k` | Output | Target | Conf section | One-time tooling (WSL) |
|---|---|---|---|---|
| `deb` | `.deb` | Debian / Ubuntu / Mint | `# DEBIAN OPTIONS` | — |
| `rpm` | `.rpm` | Fedora / RHEL / openSUSE | `# RPM OPTIONS` | `rpmbuild` |
| `appimage` | `.AppImage` | single portable file, any distro | `# APPIMAGE OPTIONS` | `appimagetool` + type2 runtime |
| `flatpak` | `.flatpak` | sandboxed, Flathub / any distro | `# FLATPAK OPTIONS` | `flatpak` + `flatpak-builder` + runtimes |
| `zip` | `.zip` | portable archive | `# ZIP OPTIONS` | — |

---

## Build recipes

All recipes run in **WSL (Ubuntu)**. Each block is **self-contained — copy the whole block once**: it enters the repository root via `$DATAORG_REPO` (set once in Stage 1), reads `<AppVersion>` from `Directory.Build.props` (the single source of truth), builds, removes the intermediate `Artifacts.*` folder on success, and lists the result.

`--app-version "$(dataorg_ver)[1]"`: `[1]` is the **package revision**, separate from the app version — bump it when you re-package the same version. `-r linux-x64` is the runtime, `-y` disables prompts, `--app-version` overrides `AppVersionRelease` in the conf. Add `--verbose` to diagnose a failure; the first build of each kind is slow (runtime + NuGet download).

> **Release vs Debug.** PupNet ignores the configuration selected in Visual Studio and builds **Release** by default. For a Debug build, add `-c Debug` to the `pupnet` command. PupNet then appends `.Debug` to the `AppId`, so the Debug package installs alongside the Release one instead of replacing it.

### Debian (.deb)

```bash
cd "$DATAORG_REPO" && \
pupnet app.pupnet.conf -r linux-x64 -k deb -y --app-version "$(dataorg_ver)[1]" && \
rm -rf Publish/Artifacts.Deb.amd64 && \
ls -la Publish/*.deb
```

### RPM (.rpm)

One-time: `sudo apt install -y rpm`

```bash
cd "$DATAORG_REPO" && \
pupnet app.pupnet.conf -r linux-x64 -k rpm -y --app-version "$(dataorg_ver)[1]" && \
rm -rf Publish/Artifacts.Rpm.x86_64 && \
ls -la Publish/*.rpm
```

### AppImage (.AppImage)

**One-time setup (copy once).** WSL has no `appimagetool`; fetch it once. It downloads its own type2 runtime at build time, so `AppImageRuntimePath` in `app.pupnet.conf` is left empty (a non-empty path there is validated for *every* `-k`, which would break the other recipes):

```bash
sudo curl -L -o /usr/local/bin/appimagetool-x86_64.AppImage \
  https://github.com/AppImage/appimagetool/releases/download/continuous/appimagetool-x86_64.AppImage && \
sudo chmod +x /usr/local/bin/appimagetool-x86_64.AppImage
```

**Build (copy once).** `APPIMAGE_EXTRACT_AND_RUN=1` lets the bundled tools run without `libfuse2` (WSL ships only `fuse3`):

```bash
cd "$DATAORG_REPO" && \
APPIMAGE_EXTRACT_AND_RUN=1 pupnet app.pupnet.conf -r linux-x64 -k appimage -y --app-version "$(dataorg_ver)[1]" && \
rm -rf Publish/Artifacts.AppImage.x86_64 && \
ls -la Publish/*.AppImage
```

> The same FUSE caveat applies to **running** the finished file on a host without `libfuse2`: `./DataOrganizer*.AppImage --appimage-extract-and-run` (or install `libfuse2`).

### Flatpak (.flatpak)

**One-time setup (copy once).** Install the toolchain, then add the `flathub` remote. In WSL `flatpak remote-add` hangs fetching `flathub.org` directly (HTTP 301 redirect times out), so download the small repo file with `curl` first and add it from disk; the actual runtime download from `dl.flathub.org` works fine. Runtimes are installed `--user`:

```bash
sudo apt install -y flatpak flatpak-builder && \
curl -L -o /tmp/flathub.flatpakrepo https://flathub.org/repo/flathub.flatpakrepo && \
flatpak remote-add --if-not-exists --user flathub /tmp/flathub.flatpakrepo && \
flatpak install -y --user flathub org.freedesktop.Platform//25.08 org.freedesktop.Sdk//25.08
```

**Build (copy once):**

```bash
cd "$DATAORG_REPO" && \
pupnet app.pupnet.conf -r linux-x64 -k flatpak -y --app-version "$(dataorg_ver)[1]" && \
rm -rf Publish/Artifacts.Flatpak.x86_64 && \
ls -la Publish/*.flatpak
```

### Zip (.zip)

```bash
cd "$DATAORG_REPO" && \
pupnet app.pupnet.conf -r linux-x64 -k zip -y --app-version "$(dataorg_ver)[1]" && \
rm -rf Publish/Artifacts.Zip.x86_64 && \
ls -la Publish/*.zip
```

---

## Output

Each build drops two shipping files in `Publish/` (this is what you distribute) plus one intermediate folder (removed by the `rm -rf Artifacts.*` line in each recipe):

- **`Publish/<name>`** — the package itself. It is large (~40+ MB) because it is *self-contained*: the app **and** the .NET runtime are bundled inside. Names: `dataorganizer_<version>-<rev>_amd64.deb` (Debian convention — lowercase, `amd64`), `DataOrganizer-<version>-<rev>.x86_64.{rpm,AppImage,flatpak}`, `DataOrganizer-<version>.zip`.
- **`Publish/<name>.sha256.txt`** — the SHA-256 checksum, so users can verify the download (`sha256sum -c`). Handy for GitHub Releases; shipping it is optional.
- **`Publish/Artifacts.<Kind>.<arch>/`** — intermediate files used to assemble the package (the `control` metadata, the generated `.desktop` entry, and the expanded AppStream `.metainfo.xml` validated in Stage 3). Kept for inspection, not for distribution.

---

## Verify — Debian example

**Install** (`apt` resolves dependencies; do **not** use `dpkg -i`, which does not):

```bash
sudo apt install ./Publish/DataOrganizer*.deb
```

**Confirm it landed** and that the desktop entry exists:

```bash
which dataorganizer
ls /usr/share/applications/ | grep -i dataorganizer
```

**Launch** — from the application menu (look for *"Data Organizer"* with the icon), or from a terminal:

```bash
dataorganizer
```

**Uninstall**:

```bash
sudo apt remove dataorganizer
```

> Other formats run differently: an `.AppImage` just needs `chmod +x` then `./DataOrganizer*.AppImage`; a `.flatpak` installs with `flatpak install --user ./Publish/DataOrganizer*.flatpak`; a `.zip` is unpacked and run in place.
