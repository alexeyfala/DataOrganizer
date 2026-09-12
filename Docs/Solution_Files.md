# Reference for the non-code files of the solution

- 📁 **`Deployment/`** — packaging; build recipes are in [Publish.md](Publish.md). Windows packaging is built by the WiX projects `Setup` and `Bundle`, which are not described here.
  - 📁 **`PupNet/`** — Linux packaging; both files live in the repository root.
    - ⚙️ [`app.metainfo.xml`](../app.metainfo.xml) — AppStream metadata for software centres; most fields are filled in by PupNet. **Edit:** categories, keywords, content rating and screenshots are written by hand.
    - ⚙️ [`app.pupnet.conf`](../app.pupnet.conf) — PupNet configuration: identifier, description, licence, icons, `dotnet publish` arguments, settings for `.deb`, `.rpm`, AppImage, Flatpak and zip. The version is passed through `--app-version`. **Edit:** package dependencies or publish arguments change.
- 📁 **`Docs/`**
  - 📁 **`Database/`**
    - 📄 [`Migrations.md`](Database/Migrations.md) — EF Core commands: create and revert a migration, apply it to a database, generate SQL. **Open:** the data model changed.
  - 📁 **`Images/`**
    - 🖼️ `*.png` — screenshots for [`README.md`](../README.md).
  - 📁 **`PupNet_Instructions/`**
    - 📄 [`1_Setup.md`](PupNet_Instructions/1_Setup.md) — stage 1: the WSL environment with Ubuntu.
    - 📄 [`2_Config.md`](PupNet_Instructions/2_Config.md) — stage 2: generating and filling in [`app.pupnet.conf`](../app.pupnet.conf).
    - 📄 [`3_AppStream.md`](PupNet_Instructions/3_AppStream.md) — stage 3: what in [`app.metainfo.xml`](../app.metainfo.xml) is filled in by hand.
    - 📄 [`4_Build.md`](PupNet_Instructions/4_Build.md) — stage 4: building `.deb`, `.rpm`, AppImage, Flatpak and zip, and checking the package.
  - 📄 [`Encryption_Format.md`](Encryption_Format.md) — the layout of encrypted blobs and the rule "a new layout takes a new version byte". **Open:** the encryption stack is being changed.
  - 📄 [`GitHub_Release.md`](GitHub_Release.md) — checklist for publishing a release on GitHub. **Open:** the artifacts are built and the tag is ready to go out.
  - 📄 [`Publish.md`](Publish.md) — recipes for building the distributables for Windows, Linux and macOS.
  - 📄 [`release-notes.template.md`](release-notes.template.md) — release description template with `{version}` placeholders. **Edit:** the set of release artifacts changed.
  - 📄 [`Solution_Files.md`](Solution_Files.md) — this file. **Edit:** a non-code file appeared in or left the solution — otherwise `SolutionFilesReferenceTests` fails the test run: it checks this reference against `.slnx` and against the contents of `Docs/`.
- 📁 **`Solution Items/`**
  - 📁 **`.config/`**
    - ⚙️ [`dotnet-tools.json`](../.config/dotnet-tools.json) — local tools: `dotnet-ef`. Restored with `dotnet tool restore`. **Update:** together with EF Core — the tool version stays at or above the package version. Migration commands are in [Database/Migrations.md](Database/Migrations.md).
  - 📁 **`.github/`**
    - 📁 **`ISSUE_TEMPLATE/`**
      - ⚙️ [`bug_report.yml`](../.github/ISSUE_TEMPLATE/bug_report.yml) — bug report form, label `bug`.
      - ⚙️ [`config.yml`](../.github/ISSUE_TEMPLATE/config.yml) — forbids blank issues, sends questions to Discussions and vulnerabilities to the private advisory form.
      - ⚙️ [`feature_request.yml`](../.github/ISSUE_TEMPLATE/feature_request.yml) — feature request form, label `enhancement`.
    - 📁 **`workflows/`**
      - ⚙️ [`ci.yml`](../.github/workflows/ci.yml) — builds the Desktop project and runs every test project on ubuntu, windows and macos. **Edit:** a test project was added — give it a step.
    - ⚙️ [`dependabot.yml`](../.github/dependabot.yml) — weekly NuGet and GitHub Actions updates, minor and patch in a single pull request. **Edit:** too many or too few updates arrive.
    - 📄 [`PULL_REQUEST_TEMPLATE.md`](../.github/PULL_REQUEST_TEMPLATE.md) — pull request description with a checklist.
    - ⚙️ [`release.yml`](../.github/release.yml) — categories and labels for the "Generate release notes" button; the `ignore-for-release` label hides a pull request. **Edit:** the set of labels changed.
  - 📁 **`.vscode/`** — for VS Code only; `launch.json` and `tasks.json` take the names from `settings.json` through `${config:...}`, and `VsCodeConfigConsistencyTests` keeps them in sync.
    - ⚙️ [`launch.json`](../.vscode/launch.json) — Debug and Release launch; on macOS the `.app` is started. **Edit:** the target platform or the paths change.
    - ⚙️ [`settings.json`](../.vscode/settings.json) — `app.name`, build arguments, hiding `bin` and `obj`, opening `.md` straight in the preview. **Edit:** renaming the application — here and nowhere else.
    - ⚙️ [`tasks.json`](../.vscode/tasks.json) — the `build-app` and `build-app-release` tasks; on macOS `.MacOS` is built. **Edit:** the configurations or the build arguments change.
  - 📁 **`Community/`** — a virtual folder; the files live in the repository root.
    - 📄 [`CODE_OF_CONDUCT.md`](../CODE_OF_CONDUCT.md) — Contributor Covenant. **Edit:** the contact for complaints changed.
    - 📄 [`CONTRIBUTING.md`](../CONTRIBUTING.md) — ways to contribute and what a pull request has to satisfy. **Edit:** the workflow changed.
    - 📄 [`SECURITY.md`](../SECURITY.md) — security policy: the private channel, what a report contains, the limits of the threat model. **Edit:** the encryption guarantees or the supported versions changed.
  - 📁 **`License/`** — a virtual folder; the files live in the repository root.
    - ⚖️ [`LICENSE`](../LICENSE) — Apache 2.0; the only hand-written copy of the licence, left alone.
    - ⚖️ [`NOTICE`](../NOTICE) — the notice Apache 2.0 requires: copyright, the LGPL component libuiohook, the reservation about the product name. **Edit:** the copyright year changed, or a component arrived that requires a notice.
    - ⚖️ [`THIRD-PARTY-NOTICES.txt`](../THIRD-PARTY-NOTICES.txt) — third-party components and their licences; generated by `tools/gen-third-party-notices.ps1` from `project.assets.json` and never edited by hand. **Regenerate:** before a release.
  - 📁 **`tools/`** — PowerShell 5.1, run from the repository root.
    - 💻 [`gen-license-rtf.ps1`](../tools/gen-license-rtf.ps1) — `LICENSE` → `Setup/LICENSE.rtf`; runs on its own from the PreBuild of the `Setup` project.
    - 💻 [`gen-release-notes.ps1`](../tools/gen-release-notes.ps1) — fills the version into the template, writes `Publish/release-notes.md` and copies the text to the clipboard. **Run:** when publishing a release.
    - 💻 [`gen-third-party-notices.ps1`](../tools/gen-third-party-notices.ps1) — rebuilds `THIRD-PARTY-NOTICES.txt` from `project.assets.json`. **Run:** before a release and after the dependencies change; needs a fresh `dotnet restore`, and the output must carry no `UNKNOWN`.
  - ⚙️ [`.editorconfig`](../.editorconfig) — code style and naming rules; together with `EnforceCodeStyleInBuild` a violation reaches the compiler output. The `[*.{csproj,wixproj,props,targets,wxs,wxi}]` section keeps tabs in MSBuild and WiX files. **Edit:** a convention changes.
  - ⚙️ [`.gitattributes`](../.gitattributes) — line ending normalisation (`* text=auto`); the rest is the commented-out Visual Studio template. **Edit:** almost never.
  - ⚙️ [`.gitignore`](../.gitignore) — what stays out of the repository: `bin/`, `obj/`, `.vs/`, `Publish/` (finished installers and archives), `Setup/LICENSE.rtf`. **Edit:** a new generated artifact appeared.
  - ⚙️ [`Directory.Build.props`](../Directory.Build.props) — the single source of the version, the application names and the assembly metadata. **Edit:** before a release — raise the version.
  - 📄 [`README.md`](../README.md) — the repository front page: features, screenshots, requirements, building, licence. **Edit:** the features or the requirements changed.

**Project files** — not part of the tree above.

**`DataOrganizer`**

- 🖼️ `Assets/Logo.ico` — application icon: window, shortcuts, Windows installer.
- 🖼️ `Assets/Logo.svg` — vector icon for the Linux packages; not part of the application build.
- 🖼️ `Assets/Logo.256.png` — raster fallback of the same icon (AppImage).
- 🖼️ `Assets/Background.jpg` — background of a record card in the dataset editor.

**`DataOrganizer.Desktop`**

- ⚙️ [`app.manifest`](../DataOrganizer.Desktop/app.manifest) — Windows manifest: the list of supported system versions; without it window transparency and embedded controls break. **Edit:** almost never.

**`DataOrganizer.MacOS`**

- ⚙️ [`Info.plist`](../DataOrganizer.MacOS/Info.plist) — `.app` bundle metadata: identifier, display name, icon file. **Edit:** the application name or identifier changes.
- ⚙️ [`Roots.xml`](../DataOrganizer.MacOS/Roots.xml) — trimmer roots for publishing: the assemblies that must not be thrown away. **Edit:** trimming dropped a type that is reached through reflection.
- 🖼️ `Logo.icns` — application icon for macOS.

**`Shared`**

- ⚙️ [`Strings.resx`](../Shared/Properties/Strings.resx) — interface strings, English.
- ⚙️ [`Strings.ru.resx`](../Shared/Properties/Strings.ru.resx) — Russian translation. **Edit:** an interface string was added or changed.

---

📁 folder · 📄 documentation · ⚙️ configuration · 💻 script · ⚖️ legal · 🖼️ images
