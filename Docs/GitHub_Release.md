# GitHub Release — checklist & notes template

How to cut a release on GitHub, plus a reusable description template. Pairs with
[Publish.md](Publish.md) (which produces the artifacts this step uploads).

---

## Checklist

1\. Build the artifacts per [Publish.md](Publish.md) — one installer + one portable per platform — into `Publish/`.

2\. Open **Releases → Create a new release** (`https://github.com/alexeyfala/DataOrganizer/releases/new`).

3\. **Choose a tag** → type `v{version}` (e.g. `v0.1.0`) → **Create new tag on publish**. Target: `master`.

4\. **Release title:** `Data Organizer {version}`.

5\. Click **Generate release notes** first — GitHub fills a **What's Changed** section from the merged pull requests, contributors, and a Full Changelog link since the previous tag. (This is why clear PR titles matter — they become the changelog.)

6\. Put the cursor at the top of the description and paste the **template below**, above the generated section. Replace every `{version}`, then write 2–4 plain-language highlights.

7\. **Attach binaries:** drag the files from `Publish/` into the assets area. Do **not** attach `LICENSE` / `NOTICE` / `THIRD-PARTY-NOTICES.txt` separately — they ship inside the repo and inside each artifact.

8\. **Release label:** **Pre-release** while the project is early; switch to **Latest** once a build is production-ready.

9\. **Save draft** to keep editing (invisible to the public, no tag created yet), or **Publish release** to go live (creates the tag `v{version}`).

---

## Notes template

Copy the block below into the release description, above the auto-generated **What's Changed**.

```markdown
## Data Organizer {version}

<!-- 2–4 plain-language highlights of what changed in this release. -->

### Downloads

| Platform | Installer | Portable |
|---|---|---|
| Windows (x64) | `DataOrganizer-{version}-win-x64.msi` | `DataOrganizer-{version}-win-x64-portable.zip` |
| macOS (x64) | `DataOrganizer-{version}-osx-x64.dmg` | `DataOrganizer-{version}-osx-x64-portable.zip` |
| Linux (x64) | `dataorganizer_{version}-1_amd64.deb`, `DataOrganizer-{version}-1.x86_64.AppImage` | — |

All builds are self-contained — the .NET runtime is bundled, no separate install required.
(The `-1` in the Linux file names is the package revision; bump it when re-packaging the same version.)

### Install & run

**Windows** — run the `.msi`, or unzip the portable archive and launch `DataOrganizer.exe`. The build is unsigned, so SmartScreen may warn: *More info → Run anyway*.

**macOS** — the app is not signed or notarized, so Gatekeeper blocks it on first launch. Right-click the app → **Open** → **Open**, or clear the quarantine flag: `xattr -dr com.apple.quarantine DataOrganizer.app`. From the `.dmg`: drag the app to Applications. Portable: unzip and run the `.app`. Runs on Apple Silicon via Rosetta 2.

**Linux** — `.deb`: `sudo apt install ./dataorganizer_{version}-1_amd64.deb`. AppImage: `chmod +x DataOrganizer-{version}-1.x86_64.AppImage` then run it (on hosts without `libfuse2`, add `--appimage-extract-and-run`).

### Notes

- x64 only in this release.
- `*.sha256.txt` files are provided for the Linux packages to verify downloads (`sha256sum -c`).
```
