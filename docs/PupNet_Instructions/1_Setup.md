# Stage 1 — Prepare the Linux build environment (WSL)

**1) Check whether WSL/Ubuntu is already installed** — in Windows PowerShell:

```powershell
wsl -l -v
```

- A line with `Ubuntu` (or any apt-based distro) → already installed, **skip step 2**.
- Empty list / no distros → go to step 2.
- `wsl` not recognized → WSL itself is missing, go to step 2.

If the distro is not named `Ubuntu` (e.g. `Debian`), the steps are the same — just enter it with `wsl -d <DistroName>`.

**2) Install WSL with Ubuntu** (skip if step 1 found it) — in Windows PowerShell (Admin):

```powershell
wsl --install -d Ubuntu
```

Reboot if asked, then open the **Ubuntu** terminal and create a user when prompted.

If you have more than one distro (e.g. Debian + Ubuntu), enter Ubuntu explicitly, and optionally make it the default:

```powershell
wsl.exe -d Ubuntu
wsl --set-default Ubuntu
```

**3) Enable `metadata` on the Windows drive mount.** Builds run from `/mnt/c`. By default WSL mounts it as `uid=0` without `metadata`, so every file belongs to `root` and the build (running as your user) cannot set timestamps on the files it creates — it fails with `MSB3374: ... last write time ... cannot be set. Access to the path ... is denied`. Append the option to `/etc/wsl.conf` (inside Ubuntu):

```bash
sudo tee -a /etc/wsl.conf >/dev/null <<'CONF'

[automount]
options = "metadata"
CONF
```

Restart WSL from **Windows PowerShell** so `/mnt/c` remounts:

```powershell
wsl --shutdown
```

Re-open Ubuntu and confirm the mount now reports `uid=1000;gid=1000;metadata`:

```bash
mount | grep ' /mnt/c '
```

**4) Update the package list** (inside Ubuntu):

```bash
sudo apt update && sudo apt upgrade -y
```

> **WSL1:** run only `sudo apt update`. Under WSL1 the setup script of the new `systemd` cannot open `/etc/machine-id` (`Bad file descriptor`), so the upgrade stops with half-configured packages, and every later `apt install` fails on them. The build needs only the fresh package list.

**5) Define the repository path and version helper.** Step 7 and the build recipes in Stage 4 use `$DATAORG_REPO` (the repo path) and `$(dataorg_ver)` (the current `<AppVersion>`) instead of hardcoding them. The path is a fixed value; the version is a function so it is re-read from `Directory.Build.props` on every call and never goes stale. Replace `<user-name>` with the Windows user name — drive `C:` is visible inside WSL as `/mnt/c` — and keep the `&&`: a wrong folder makes `cd` fail, and then nothing is appended to `~/.bashrc`:

```bash
cd /mnt/c/Users/<user-name>/source/repos/DataOrganizerAvaloniaApp && echo "export DATAORG_REPO=\"$PWD\"" >> ~/.bashrc
echo "dataorg_ver() { grep -oP '(?<=<AppVersion>)[^<]+' \"\$DATAORG_REPO/Directory.Build.props\"; }" >> ~/.bashrc
source ~/.bashrc
```

Confirm both resolve (expect the path, then the current `<AppVersion>`):

```bash
echo "$DATAORG_REPO" && dataorg_ver
```

**6) WSL1 only: put the `tar` of Ubuntu 24.04 first on `PATH`.** The `tar` of Ubuntu 26.04 calls `openat2`, which the WSL1 kernel lacks, so it cannot unpack nested folders (`Cannot mkdir: Function not implemented`). That breaks the SDK install of step 7 and `dpkg-deb` in the Debian build. The same `tar` 1.35 built for Ubuntu 24.04 does not call it. Skip this step on WSL2 (`wsl -l -v` in Windows PowerShell shows the version):

```bash
mkdir -p ~/.local/tar-noble/bin && \
curl -fsSL -o /tmp/tar-noble.deb \
  https://archive.ubuntu.com/ubuntu/pool/main/t/tar/tar_1.35+dfsg-3build1_amd64.deb && \
dpkg-deb --fsys-tarfile /tmp/tar-noble.deb | tar -xO ./usr/bin/tar > ~/.local/tar-noble/bin/tar && \
chmod +x ~/.local/tar-noble/bin/tar && \
rm /tmp/tar-noble.deb
echo 'export PATH="$HOME/.local/tar-noble/bin:$PATH"' >> ~/.bashrc
source ~/.bashrc
```

**7) Install the .NET SDK** the repository needs. The Ubuntu feed can lag behind a new .NET: Ubuntu 26.04 has none for .NET 11 yet ([Install .NET on Ubuntu](https://learn.microsoft.com/dotnet/core/install/linux-ubuntu-install)). So install it with Microsoft's install script, which takes the version from `global.json` of the repository. The script installs no dependencies; of those, the WSL image of Ubuntu 26.04 lacks only ICU (`libicu78`; another release names its own, see the same page). Then verify (expect the SDK version of `global.json`):

```bash
sudo apt install -y libicu78 && \
curl -fsSL -o /tmp/dotnet-install.sh https://dot.net/v1/dotnet-install.sh && \
bash /tmp/dotnet-install.sh --jsonfile "$DATAORG_REPO/global.json" && \
rm /tmp/dotnet-install.sh
echo 'export DOTNET_ROOT="$HOME/.dotnet"' >> ~/.bashrc
echo 'export PATH="$PATH:$DOTNET_ROOT:$DOTNET_ROOT/tools"' >> ~/.bashrc
source ~/.bashrc
dotnet --version
```

**8) Verify the Debian packaging tool** is present (ships with Ubuntu by default):

```bash
dpkg-deb --version
```

**9) Install the PupNet tool** (global dotnet tool) and confirm it is listed (look for `kuiperzone.pupnet`). Global dotnet tools live in `~/.dotnet/tools`, which step 7 put on `PATH`:

```bash
dotnet tool install -g KuiperZone.PupNet
dotnet tool list -g
```

**10) Verify PupNet works:**

```bash
pupnet --version
```
