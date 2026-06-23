# Stage 4 — Build and verify Linux packages

> **Prerequisite:** `app.pupnet.conf` is filled in and the icons exist (`DataOrganizer/Assets/Logo.svg` + `Logo.256.png`). Builds run in WSL (Ubuntu) and need internet access (`dotnet publish` pulls the `linux-x64` runtime and NuGet packages).

---

## Linux package formats PupNet can build

PupNet builds several Linux formats from the same config. Each is selected with the `-k` flag and has its own settings section in `app.pupnet.conf`; the build command is identical except for `-k`.

| `-k` | Output | Target | Conf section | One-time tooling (WSL) |
|---|---|---|---|---|
| `deb` | `.deb` | Debian / Ubuntu / Mint | `# DEBIAN OPTIONS` | — |
| `rpm` | `.rpm` | Fedora / RHEL / openSUSE | `# RPM OPTIONS` | `rpmbuild` |
| `appimage` | `.AppImage` | single portable file, any distro | `# APPIMAGE OPTIONS` | `appimagetool` + type2 runtime |
| `flatpak` | `.flatpak` | sandboxed, Flathub / any distro | `# FLATPAK OPTIONS` | `flatpak` + `flatpak-builder` + runtimes |
| `zip` | `.zip` | portable archive | `# ZIP OPTIONS` | — |

`appimage` uses the raster `Logo.256.png` for its root icon; `deb`/`rpm`/`flatpak` use the scalable `Logo.svg`.

---

## Build recipes

All recipes run in **WSL (Ubuntu)**. Each block is **self-contained — copy the whole block once**: it enters the solution root, reads `<AppVersion>` from `Directory.Build.props` (the single source of truth), builds, removes the intermediate `Artifacts.*` folder on success, and lists the result.

`--app-version "${VER}[1]"`: `[1]` is the **package revision**, separate from the app version — bump it when you re-package the same version. `-r linux-x64` is the runtime, `-y` disables prompts, `--app-version` overrides `AppVersionRelease` in the conf. Add `--verbose` to diagnose a failure; the first build of each kind is slow (runtime + NuGet download).

### Debian (.deb)

```bash
cd /mnt/c/Users/alexey/source/repos/DataOrganizerAvaloniaApp && \
VER=$(grep -oP '(?<=<AppVersion>)[^<]+' Directory.Build.props) && \
pupnet app.pupnet.conf -r linux-x64 -k deb -y --app-version "${VER}[1]" && \
rm -rf Publish/Artifacts.Deb.amd64 && \
ls -la Publish/*.deb
```

> To inspect the expanded AppStream `.metainfo.xml` (Stage 3), drop the `rm -rf ...` line — it lives in `Publish/Artifacts.Deb.amd64/`.

### RPM (.rpm)

One-time: `sudo apt install -y rpm`

```bash
cd /mnt/c/Users/alexey/source/repos/DataOrganizerAvaloniaApp && \
VER=$(grep -oP '(?<=<AppVersion>)[^<]+' Directory.Build.props) && \
pupnet app.pupnet.conf -r linux-x64 -k rpm -y --app-version "${VER}[1]" && \
rm -rf Publish/Artifacts.Rpm.x86_64 && \
ls -la Publish/*.rpm
```

### AppImage (.AppImage)

**One-time setup (copy once).** WSL has neither `appimagetool` nor the bits it needs, and its proxy blocks the tool's own downloads (HTTP 302). Fetch both manually with `curl -L` (follows the redirect). The runtime path here **must match `AppImageRuntimePath` in `app.pupnet.conf`**:

```bash
sudo curl -L -o /usr/local/bin/appimagetool-x86_64.AppImage \
  https://github.com/AppImage/appimagetool/releases/download/continuous/appimagetool-x86_64.AppImage && \
sudo chmod +x /usr/local/bin/appimagetool-x86_64.AppImage && \
sudo mkdir -p /usr/local/share/pupnet && \
sudo curl -L -o /usr/local/share/pupnet/runtime-x86_64 \
  https://github.com/AppImage/type2-runtime/releases/download/continuous/runtime-x86_64 && \
sudo chmod +x /usr/local/share/pupnet/runtime-x86_64
```

**Build (copy once).** `APPIMAGE_EXTRACT_AND_RUN=1` lets the bundled tools run without `libfuse2` (WSL ships only `fuse3`):

```bash
cd /mnt/c/Users/alexey/source/repos/DataOrganizerAvaloniaApp && \
VER=$(grep -oP '(?<=<AppVersion>)[^<]+' Directory.Build.props) && \
APPIMAGE_EXTRACT_AND_RUN=1 pupnet app.pupnet.conf -r linux-x64 -k appimage -y --app-version "${VER}[1]" && \
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
cd /mnt/c/Users/alexey/source/repos/DataOrganizerAvaloniaApp && \
VER=$(grep -oP '(?<=<AppVersion>)[^<]+' Directory.Build.props) && \
pupnet app.pupnet.conf -r linux-x64 -k flatpak -y --app-version "${VER}[1]" && \
rm -rf Publish/Artifacts.Flatpak.x86_64 && \
ls -la Publish/*.flatpak
```

### Zip (.zip)

```bash
cd /mnt/c/Users/alexey/source/repos/DataOrganizerAvaloniaApp && \
VER=$(grep -oP '(?<=<AppVersion>)[^<]+' Directory.Build.props) && \
pupnet app.pupnet.conf -r linux-x64 -k zip -y --app-version "${VER}[1]" && \
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

**Uninstall** when done testing:

```bash
sudo apt remove dataorganizer
```

> Other formats run differently: an `.AppImage` just needs `chmod +x` then `./DataOrganizer*.AppImage`; a `.flatpak` installs with `flatpak install --user ./Publish/DataOrganizer*.flatpak`; a `.zip` is unpacked and run in place.
