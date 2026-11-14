# Stage 2 — Generate the PupNet configuration

**1) Enter Ubuntu** — in Windows PowerShell:

```powershell
wsl.exe -d Ubuntu
```

Then, inside Ubuntu, go to the `build` folder of the repository, where the PupNet inputs live, and list it:

```bash
cd "$DATAORG_REPO/build"
ls
```

**2) Generate a configuration template** — this writes a sample `*.pupnet.conf` into the current folder:

```bash
pupnet --new conf
```

> The repository already has the filled-in `app.pupnet.conf`: PupNet asks `app.pupnet.conf exists. Replace? [N/y]` — answer `N`, as `y` overwrites it. To see a fresh template, run the command in another folder.

**3) See what was created** — it is named `app.pupnet.conf` (the rest of the guide uses this name):

```bash
ls -la *.pupnet.conf
```

> The file can also be opened/edited from Windows in Visual Studio, since it lives under `/mnt/c`.

**4) Get a feel for the fields** (do *not* fill it in by hand yet — press `q` to quit `less`):

```bash
less app.pupnet.conf
```
