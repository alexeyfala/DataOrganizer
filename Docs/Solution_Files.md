# Reference for the non-code files of the solution

- 📁 **`Deployment/`** — packaging; recipes in [Publish.md](Publish.md).
  - 📁 **`PupNet/`** — Linux packaging; the files live in `build/`.
    - ⚙️ [`app.metainfo.xml`](../build/app.metainfo.xml) — AppStream metadata for software centres.
    - ⚙️ [`app.pupnet.conf`](../build/app.pupnet.conf) — PupNet configuration.
- 📁 **`Docs/`**
  - 📁 **`Database/`**
    - 📄 [`Migrations.md`](Database/Migrations.md) — EF Core migration commands.
  - 📁 **`Images/`**
    - 🖼️ `*.png` — screenshots for [`README.md`](../README.md).
  - 📁 **`PupNet_Instructions/`**
    - 📄 [`1_Setup.md`](PupNet_Instructions/1_Setup.md) — stage 1: the WSL environment.
    - 📄 [`2_Config.md`](PupNet_Instructions/2_Config.md) — stage 2: [`app.pupnet.conf`](../build/app.pupnet.conf).
    - 📄 [`3_AppStream.md`](PupNet_Instructions/3_AppStream.md) — stage 3: [`app.metainfo.xml`](../build/app.metainfo.xml).
    - 📄 [`4_Build.md`](PupNet_Instructions/4_Build.md) — stage 4: building and checking the packages.
  - 📄 [`Debugging_Tests.md`](Debugging_Tests.md) — how to run `Debug All Tests` without stops on expected exceptions.
  - 📄 [`Encryption_Format.md`](Encryption_Format.md) — the layout of encrypted data.
  - 📄 [`GitHub_Release.md`](GitHub_Release.md) — checklist for publishing a release.
  - 📄 [`Publish.md`](Publish.md) — building the distributables for Windows, Linux and macOS.
  - 📄 [`release-notes.template.md`](release-notes.template.md) — release description template.
  - 📄 [`Solution_Files.md`](Solution_Files.md) — this file; `SolutionFilesReferenceTests` checks it against the solution.
  - 📄 [`Stryker_Mutation_Testing.md`](Stryker_Mutation_Testing.md) — running and reading the mutation tests.
- 📁 **`Solution Items/`**
  - 📁 **`.config/`**
    - ⚙️ [`dotnet-tools.json`](../.config/dotnet-tools.json) — local .NET tools.
  - 📁 **`.github/`**
    - 📁 **`ISSUE_TEMPLATE/`**
      - ⚙️ [`bug_report.yml`](../.github/ISSUE_TEMPLATE/bug_report.yml) — bug report form.
      - ⚙️ [`config.yml`](../.github/ISSUE_TEMPLATE/config.yml) — issue form settings.
      - ⚙️ [`feature_request.yml`](../.github/ISSUE_TEMPLATE/feature_request.yml) — feature request form.
    - 📁 **`workflows/`**
      - ⚙️ [`ci.yml`](../.github/workflows/ci.yml) — build and tests on Windows, Linux and macOS.
    - 📄 [`PULL_REQUEST_TEMPLATE.md`](../.github/PULL_REQUEST_TEMPLATE.md) — pull request template.
    - ⚙️ [`release.yml`](../.github/release.yml) — categories of the generated release notes.
  - 📁 **`.vscode/`** — VS Code only.
    - ⚙️ [`launch.json`](../.vscode/launch.json) — launch configurations.
    - ⚙️ [`settings.json`](../.vscode/settings.json) — workspace settings.
    - ⚙️ [`tasks.json`](../.vscode/tasks.json) — build tasks.
  - 📁 **`Community/`** — a virtual folder; the files live in the repository root.
    - 📄 [`CODE_OF_CONDUCT.md`](../CODE_OF_CONDUCT.md) — code of conduct.
    - 📄 [`CONTRIBUTING.md`](../CONTRIBUTING.md) — how to contribute.
    - 📄 [`SECURITY.md`](../SECURITY.md) — security policy.
  - 📁 **`License/`** — a virtual folder; the files live in the repository root.
    - ⚖️ [`LICENSE`](../LICENSE) — Apache 2.0.
    - ⚖️ [`NOTICE`](../NOTICE) — the notice Apache 2.0 requires.
    - ⚖️ [`THIRD-PARTY-NOTICES.txt`](../THIRD-PARTY-NOTICES.txt) — third-party licences; generated, never edited by hand.
  - 📁 **`build/`** — PowerShell 5.1 scripts.
    - 💻 [`gen-license-rtf.ps1`](../build/gen-license-rtf.ps1) — `LICENSE` → `src/Setup/LICENSE.rtf`.
    - 💻 [`gen-release-notes.ps1`](../build/gen-release-notes.ps1) — release notes from the template.
    - 💻 [`gen-third-party-notices.ps1`](../build/gen-third-party-notices.ps1) — generates `THIRD-PARTY-NOTICES.txt`.
    - 💻 [`run-stryker.ps1`](../build/run-stryker.ps1) — the mutation run.
  - ⚙️ [`.editorconfig`](../.editorconfig) — code style and naming rules.
  - ⚙️ [`.gitattributes`](../.gitattributes) — line ending normalisation.
  - ⚙️ [`.gitignore`](../.gitignore) — what stays out of the repository.
  - ⚙️ [`Directory.Build.props`](../Directory.Build.props) — version, names and assembly metadata of the application, C# settings shared by the projects.
  - ⚙️ [`Directory.Packages.props`](../Directory.Packages.props) — the version of every NuGet package.
  - ⚙️ [`global.json`](../global.json) — the .NET SDK the repository builds with.
  - ⚙️ [`nuget.config`](../nuget.config) — the package source.
  - 📄 [`README.md`](../README.md) — the repository front page.
  - ⚙️ [`stryker-config.json`](../stryker-config.json) — shared settings of the mutation runs.
  - ⚙️ [`Settings.XamlStyler`](../Settings.XamlStyler) — XAML formatting rules.

**Project files** — not part of the tree above.

**`AvaloniaEdit.Localization`**

- ⚙️ [`SR.ru.resx`](../AvaloniaEdit.Localization/SR.ru.resx) — Russian texts of the AvaloniaEdit search panel.

**`DataOrganizer`**

- 🖼️ `Assets/Logo.ico` — application icon.
- 🖼️ `Assets/Logo.svg` — vector icon for the Linux packages.
- 🖼️ `Assets/Logo.256.png` — raster icon for AppImage.
- 🖼️ `Assets/Background.jpg` — background of a record card.

**`DataOrganizer.Desktop`**

- ⚙️ [`app.manifest`](../DataOrganizer.Desktop/app.manifest) — Windows application manifest.

**`DataOrganizer.MacOS`**

- ⚙️ [`Info.plist`](../DataOrganizer.MacOS/Info.plist) — `.app` bundle metadata.
- ⚙️ [`Roots.xml`](../DataOrganizer.MacOS/Roots.xml) — trimmer roots for publishing.
- 🖼️ `Logo.icns` — application icon for macOS.

**`Shared`**

- ⚙️ [`Strings.resx`](../Shared/Properties/Strings.resx) — interface strings, English.
- ⚙️ [`Strings.ru.resx`](../Shared/Properties/Strings.ru.resx) — interface strings, Russian.

---

📁 folder · 📄 documentation · ⚙️ configuration · 💻 script · ⚖️ legal · 🖼️ images
