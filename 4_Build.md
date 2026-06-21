# Stage 4 — Build and verify the `.deb` package

> **Prerequisite:** `app.pupnet.conf` is filled in and `DataOrganizer/Assets/Logo.256.png` exists. The build runs in WSL (Ubuntu) and needs internet access (`dotnet publish` pulls the `linux-x64` runtime and NuGet packages).

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
pupnet app.pupnet.conf -r linux-x64 -k deb -y --app-version "${VER}[1]"
```

Flags: `-k deb` (package kind), `-r linux-x64` (runtime), `-y` (no prompts), `--app-version` overrides `AppVersionRelease` from the conf.

- `[1]` is the **Debian package revision** (separate from the app version); bump it on re-packaging.
- Add `--verbose` if you need a detailed log to diagnose a failure.
- The first run is slow (runtime + NuGet download). This is expected.

**3) Check the result** — a `*.deb` should appear in `Publish/`:

```bash
ls -la Publish/*.deb
```

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
