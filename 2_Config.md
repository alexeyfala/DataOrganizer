# Stage 2 — Generate the PupNet configuration

**1) Enter Ubuntu and go to the solution root** via the Windows drive, then list it (you should see the `.sln` and the guide files):

```bash
wsl.exe -d Ubuntu
cd /mnt/c/Users/alexey/source/repos/DataOrganizerAvaloniaApp
ls
```

**2) Generate a configuration template** — this writes a sample `*.pupnet.conf` into the current folder:

```bash
pupnet --new conf
```

**3) See what was created** and note its exact name:

```bash
ls -la *.pupnet.conf
```

> The file can also be opened/edited from Windows in Visual Studio, since it lives under `/mnt/c`.

**4) Get a feel for the fields** (do *not* fill it in by hand yet — press `q` to quit `less`):

```bash
less app.pupnet.conf
```
