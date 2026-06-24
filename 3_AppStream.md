# Stage 3 — AppStream metainfo (`.metainfo.xml`)

> **What it is:** a standard XML describing the app (summary, description, screenshots, categories, keywords, releases). Graphical *software centers* (GNOME Software, KDE Discover) read it.
>
> Not needed for `apt`/`dpkg` install — it only matters for software centers / stores like Flathub. Adding it also clears the build note *"AppStream metadata file not provided"*.

PupNet auto-fills these via **macros** at build time — do **not** hand-edit them:

`${APP_ID}`, `${PUBLISHER_NAME}`, `${PUBLISHER_ID}`, `${PUBLISHER_LINK_URL}`, `${APP_VERSION}`, `${APPSTREAM_DESCRIPTION_XML}`, `${APPSTREAM_CHANGELOG_XML}`

Fields filled by hand: `categories`, `keywords`, `content_rating`, `screenshots`.

---

**1) Enter Ubuntu** — in Windows PowerShell:

```powershell
wsl.exe -d Ubuntu
```

Then, inside Ubuntu, go to the repository root:

```bash
cd "$DATAORG_REPO"
```

**2) Generate the template** — this writes `app.metainfo.xml` (same `app` base name as `app.pupnet.conf`), then confirm:

```bash
pupnet --new meta
ls -la *.metainfo.xml
```

**3) Link it** in `app.pupnet.conf`:

```ini
MetaFile = app.metainfo.xml
```

**4) Edit `app.metainfo.xml`** to fill the manual fields.

**5) Rebuild** (same build command as Stage 4) to verify the note is gone. Optional validation:

```bash
sudo apt install -y appstream
appstreamcli validate Publish/Artifacts.Deb.amd64/com.alexeyfala.dataorganizer.metainfo.xml
```

> **IMPORTANT:** validate the **generated** file, not the source template. The source `app.metainfo.xml` still contains `${...}` macros (validation would fail on them); PupNet expands them only at build time into the package artifact under `Publish/Artifacts.Deb.amd64/`.

> **Watch the auto-clean:** the Stage 4 recipe has an `rm -rf Publish/Artifacts.Deb.amd64 && \` line that deletes the very file you validate here. Drop that line from the block, run the build, validate, then remove the folder manually.

> **Note — `url-not-reachable` warning:** `appstreamcli` does a network check on the homepage URL (`PublisherLinkUrl` in `app.pupnet.conf`). If that points to a **private / not-yet-created** GitHub repo, it returns 404 and validation *"fails"* on this single warning. This is harmless — the package is fine, and it will pass once the repo is public. To validate offline meanwhile, skip the network check:
>
> ```bash
> appstreamcli validate --no-net Publish/Artifacts.Deb.amd64/com.alexeyfala.dataorganizer.metainfo.xml
> ```
