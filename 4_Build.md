# Stage 4 — Build and verify the `.deb` package

> **Prerequisite:** `app.pupnet.conf` is filled in and `DataOrganizer/Assets/Logo.256.png` exists. The build runs in WSL (Ubuntu) and needs internet access (`dotnet publish` pulls the `linux-x64` runtime and NuGet packages).

---

## Linux package formats PupNet can build

The `.deb` below is one of several formats. Each is selected with the `-k` flag and already has its own settings section in `app.pupnet.conf`. The build command is identical except for `-k`.

| `-k` | Output | Target | Conf section | Extra tooling needed |
|---|---|---|---|---|
| `deb` | `.deb` | Debian / Ubuntu / Mint | `# DEBIAN OPTIONS` | — |
| `rpm` | `.rpm` | Fedora / RHEL / openSUSE | `# RPM OPTIONS` | `rpmbuild` (`sudo apt install rpm`) |
| `appimage` | `.AppImage` | single portable file, any distro | `# APPIMAGE OPTIONS` | PupNet auto-downloads `appimagetool` (internet on first run) |
| `flatpak` | `.flatpak` | sandboxed, Flathub / any distro | `# FLATPAK OPTIONS` | `flatpak` + `flatpak-builder` + the `org.freedesktop.Platform`/`Sdk` runtimes |
| `zip` | `.zip` | portable archive | `# ZIP OPTIONS` | — |

`appimage` uses the raster `Logo.256.png` for its root icon; `deb`/`rpm`/`flatpak` use the scalable `Logo.svg`. Ready-to-copy commands for each format are in [**Build — other formats**](#build--other-formats-rpm-appimage-flatpak-zip) below.

---

## Build

**1) Enter Ubuntu and go to the solution root:**

```bash
wsl.exe -d Ubuntu
cd /mnt/c/Users/alexey/source/repos/DataOrganizerAvaloniaApp
```

**2) Build the `.deb`** — the version is read from `<AppVersion>` in `Directory.Build.props` (single source):

```bash
VER=$(grep -oP '(?<=<AppVersion>)[^<]+' Directory.Build.props)
pupnet app.pupnet.conf -r linux-x64 -k deb -y --app-version "${VER}[1]" \
  && rm -rf Publish/Artifacts.Deb.amd64
```

Flags: `-k deb` (package kind), `-r linux-x64` (runtime), `-y` (no prompts), `--app-version` overrides `AppVersionRelease` from the conf.

- `[1]` is the **Debian package revision** (separate from the app version); bump it on re-packaging.
- The `&& rm -rf Publish/Artifacts.Deb.amd64` removes the intermediate build artifacts **only on success** (a failed build keeps them for debugging). The final `.deb` and `.sha256.txt` stay.
- Add `--verbose` if you need a detailed log to diagnose a failure.
- The first run is slow (runtime + NuGet download). This is expected.

> **If you are validating AppStream metainfo (Stage 3):** the expanded `.metainfo.xml` lives inside `Artifacts.Deb.amd64/`, so run the build **without** the `&& rm -rf ...` part, validate, then clean up.

**3) Check the result** — a `*.deb` should appear in `Publish/`:

```bash
ls -la Publish/*.deb
```

## Output

A build produces the following under `Publish/`:

**Shipping files (in `Publish/` root) — this is what you distribute:**

- `dataorganizer_<version>-<rev>_amd64.deb` — the package itself. It is large (~40+ MB) because it is *self-contained*: the app **and** the .NET runtime are bundled inside.
- `dataorganizer_<version>-<rev>_amd64.deb.sha256.txt` — the SHA-256 checksum of the `.deb`, so users can verify the download with `sha256sum -c`. Handy for GitHub Releases; shipping it is optional.

**Intermediate artifacts (in `Publish/Artifacts.Deb.amd64/`) — used to assemble the `.deb`, kept for inspection, not for distribution:**

- `control` — Debian package metadata (name, version, architecture, dependencies from `DebianRecommends`, description).
- `com.alexeyfala.dataorganizer.desktop` — the generated desktop entry (menu shortcut: name, icon, launch command, category).
- `com.alexeyfala.dataorganizer.metainfo.xml` — the expanded AppStream metainfo (the file validated in Stage 3, with all `${...}` macros already substituted).

## Verify

**4) Install the package** (`apt` resolves dependencies; do **not** use `dpkg -i`, which does not):

```bash
sudo apt install ./Publish/DataOrganizer*.deb
```

**5) Confirm it landed** and that the desktop entry exists:

```bash
which dataorganizer
ls /usr/share/applications/ | grep -i dataorganizer
```

**6) Launch it** — from the application menu (look for *"Data Organizer"* with the icon), or from a terminal:

```bash
dataorganizer
```

**7) Uninstall** when done testing:

```bash
sudo apt remove dataorganizer
```

> **Report back:** the output of `ls -la Publish/*.deb`, and whether install + launch + menu icon work. If the build fails, re-run step 2 with `--verbose` and paste the last ~30 lines of output.

---

## Build — other formats (rpm, appimage, flatpak, zip)

Same workflow as the `.deb`: run in WSL (Ubuntu), only `-k` changes. Each block below is **self-contained — copy the whole block once**: it enters the solution root, reads the version from `Directory.Build.props`, builds, and lists the result.

### RPM — Fedora / RHEL / openSUSE

Needs `rpmbuild` once: `sudo apt install -y rpm`

```bash
cd /mnt/c/Users/alexey/source/repos/DataOrganizerAvaloniaApp && \
VER=$(grep -oP '(?<=<AppVersion>)[^<]+' Directory.Build.props) && \
pupnet app.pupnet.conf -r linux-x64 -k rpm -y --app-version "${VER}[1]" && \
rm -rf Publish/Artifacts.Rpm.x86_64 && \
ls -la Publish/*.rpm
```

### AppImage — single portable file, any distro

First run downloads `appimagetool` (needs internet). No install step for the end user — just `chmod +x` and run.

```bash
cd /mnt/c/Users/alexey/source/repos/DataOrganizerAvaloniaApp && \
VER=$(grep -oP '(?<=<AppVersion>)[^<]+' Directory.Build.props) && \
pupnet app.pupnet.conf -r linux-x64 -k appimage -y --app-version "${VER}[1]" && \
ls -la Publish/*.AppImage
```

### Flatpak — sandboxed, Flathub / any distro

One-time toolchain + runtimes (copy once):

```bash
sudo apt install -y flatpak flatpak-builder && \
flatpak install -y flathub org.freedesktop.Platform//25.08 org.freedesktop.Sdk//25.08
```

Build (copy once):

```bash
cd /mnt/c/Users/alexey/source/repos/DataOrganizerAvaloniaApp && \
VER=$(grep -oP '(?<=<AppVersion>)[^<]+' Directory.Build.props) && \
pupnet app.pupnet.conf -r linux-x64 -k flatpak -y --app-version "${VER}[1]" && \
ls -la Publish/*.flatpak
```

### Zip — portable archive

```bash
cd /mnt/c/Users/alexey/source/repos/DataOrganizerAvaloniaApp && \
VER=$(grep -oP '(?<=<AppVersion>)[^<]+' Directory.Build.props) && \
pupnet app.pupnet.conf -r linux-x64 -k zip -y --app-version "${VER}[1]" && \
ls -la Publish/*.zip
```

### Clean up intermediate artifacts

Each format leaves an `Artifacts.<Kind>.<arch>/` folder in `Publish/`. Remove them all after a successful build (the shipping files stay):

```bash
cd /mnt/c/Users/alexey/source/repos/DataOrganizerAvaloniaApp && rm -rf Publish/Artifacts.*
```

> Keep the `Artifacts.*` folder if you still need to inspect the expanded `.metainfo.xml` / `.desktop` / `control` for that format.
