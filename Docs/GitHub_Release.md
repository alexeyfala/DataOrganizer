# GitHub Release — checklist & notes template

How to cut a release on GitHub. Pairs with [Publish.md](Publish.md) (which
produces the artifacts this step uploads). The release description is rendered
from [release-notes.template.md](release-notes.template.md) by
`tools/gen-release-notes.ps1`.

---

## Checklist

1\. Build the artifacts per [Publish.md](Publish.md) — one installer + one portable per platform — into `Publish/`.

2\. **Generate the release text** — from the repository root, run:

```bash
powershell -ExecutionPolicy Bypass -File tools/gen-release-notes.ps1
```

It reads `AppVersion` from `Directory.Build.props`, fills the `{version}` placeholders in the template, writes `Publish/release-notes.md`, **copies the notes to the clipboard**, and prints the tag and title to paste:

```
Wrote Publish/release-notes.md  (copied to clipboard)
Tag:   v0.1.0
Title: Data Organizer 0.1.0
```

3\. Open **Releases → Create a new release** (`https://github.com/alexeyfala/DataOrganizer/releases/new`).

4\. **Choose a tag** → type the printed tag (`v{version}`) → **Create new tag on publish**. Target: `master`.

5\. **Release title:** the printed title (`Data Organizer {version}`).

6\. Click **Generate release notes** first — GitHub fills a **What's Changed** section from the merged pull requests, contributors, and a Full Changelog link since the previous tag. (This is why clear PR titles matter — they become the changelog.)

7\. Put the cursor at the top of the description and **paste the clipboard contents** (the rendered notes) above the generated section, then replace the highlights comment with 2–4 plain-language highlights.

8\. **Attach binaries:** drag the files from `Publish/` into the assets area. Do **not** attach `LICENSE` / `NOTICE` / `THIRD-PARTY-NOTICES.txt` separately — they ship inside the repo and inside each artifact. (`release-notes.md` is a scratch file — don't attach it.)

9\. **Release label:** **Pre-release** while the project is early; switch to **Latest** once a build is production-ready.

10\. **Save draft** to keep editing (invisible to the public, no tag created yet), or **Publish release** to go live (creates the tag `v{version}`).

---

## Notes template

The release description is generated from [release-notes.template.md](release-notes.template.md).
Edit that file to change the wording — keep the `{version}` tokens in place; the
generator fills them from `Directory.Build.props`. Do not replace versions by hand.
