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

**5) Install the .NET 10 SDK** inside WSL, then verify (expect `10.x`):

```bash
sudo apt install -y dotnet-sdk-10.0
dotnet --version
```

**6) Verify the Debian packaging tool** is present (ships with Ubuntu by default):

```bash
dpkg-deb --version
```

**7) Install the PupNet tool** (global dotnet tool) and confirm it is listed (look for `kuiperzone.pupnet`):

```bash
dotnet tool install -g KuiperZone.PupNet
dotnet tool list -g
```

Global dotnet tools live in `~/.dotnet/tools`, which is usually **not** on `PATH` yet — so `pupnet` gives *"Command not found"* until you add it. Add it for the current session, then make it permanent:

```bash
export PATH="$PATH:$HOME/.dotnet/tools"
echo 'export PATH="$PATH:$HOME/.dotnet/tools"' >> ~/.bashrc
source ~/.bashrc
```

**8) Verify PupNet works:**

```bash
pupnet --version
```
