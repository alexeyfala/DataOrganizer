# PupNet Deploy — Building a Linux `.deb` installer for DataOrganizer

> **Goal:** produce a `*.deb` package (and later AppImage) from the `DataOrganizer.Desktop` project.
>
> **Note:** `.deb` building needs a Linux environment with `dpkg-deb`, so all build steps run inside **WSL (Ubuntu)**.

---

## Stage 1 — Prepare the Linux build environment (WSL)

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

**3) Update the package list** (inside Ubuntu):

```bash
sudo apt update && sudo apt upgrade -y
```

**4) Install the .NET 10 SDK** inside WSL, then verify (expect `10.x`):

```bash
sudo apt install -y dotnet-sdk-10.0
dotnet --version
```

**5) Verify the Debian packaging tool** is present (ships with Ubuntu by default):

```bash
dpkg-deb --version
```

**6) Install the PupNet tool** (global dotnet tool) and confirm it is listed (look for `kuiperzone.pupnet`):

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

**7) Verify PupNet works:**

```bash
pupnet --version
```

> **Report back:** outputs of `dotnet --version`, `dpkg-deb --version`, and `pupnet --version`.
