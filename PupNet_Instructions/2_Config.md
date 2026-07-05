# Stage 2 — Generate the PupNet configuration

**1) Enter Ubuntu** — in Windows PowerShell:

```powershell
wsl.exe -d Ubuntu
```

Then, inside Ubuntu, go to the repository root and list it (you should see the `.sln` and the guide files):

```bash
cd "$DATAORG_REPO"
ls
```

**2) Generate a configuration template** — this writes a sample `*.pupnet.conf` into the current folder:

```bash
pupnet --new conf
```

**3) See what was created** — it is named `app.pupnet.conf` (the rest of the guide uses this name):

```bash
ls -la *.pupnet.conf
```

> The file can also be opened/edited from Windows in Visual Studio, since it lives under `/mnt/c`.

**4) Get a feel for the fields** (do *not* fill it in by hand yet — press `q` to quit `less`):

```bash
less app.pupnet.conf
```
