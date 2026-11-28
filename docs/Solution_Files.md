# Reference for the non-code files of the solution

- 📁 **`build/`** — scripts (PowerShell 5.1, one POSIX shell) and the inputs of the Linux packages; recipes in [Publish.md](Publishing/Publish.md).
  - ⚙️ [`app.metainfo.xml`](../build/app.metainfo.xml) — AppStream metadata for software centres.
  - ⚙️ [`app.pupnet.conf`](../build/app.pupnet.conf) — PupNet configuration.
  - 💻 [`gen-license-rtf.ps1`](../build/gen-license-rtf.ps1) — `LICENSE` → `src/Setup/LICENSE.rtf`.
  - 💻 [`gen-release-notes.ps1`](../build/gen-release-notes.ps1) — release notes from the template.
  - 💻 [`gen-third-party-notices.ps1`](../build/gen-third-party-notices.ps1) — generates `THIRD-PARTY-NOTICES.txt`.
  - 💻 [`post-publish.sh`](../build/post-publish.sh) — opens the published files to every user before PupNet packs them.
  - 💻 [`report-analyzers.ps1`](../build/report-analyzers.ps1) — the findings of every analyzer, grouped by rule.
  - 💻 [`run-stryker.ps1`](../build/run-stryker.ps1) — the mutation run.
  - 💻 [`update-dotnet-runtime.ps1`](../build/update-dotnet-runtime.ps1) — pins the bundle to the latest .NET runtime patch.
- 📁 **`docs/`**
  - 📁 **`Design/`**
    - 📄 [`Architecture.md`](Design/Architecture.md) — the projects, their topics and the references between them.
    - 📄 [`Encryption_Format.md`](Design/Encryption_Format.md) — the layout of encrypted data.
  - 📁 **`Development/`**
    - 📄 [`Code_Analysis.md`](Development/Code_Analysis.md) — the analyzers of the build, the levels of their rules and the report of their findings.
    - 📄 [`Database_Migrations.md`](Development/Database_Migrations.md) — EF Core migration commands.
    - 📄 [`Debugging_Tests.md`](Development/Debugging_Tests.md) — how to run `Debug All Tests` without stops on expected exceptions.
    - 📄 [`Development_Setup.md`](Development/Development_Setup.md) — setting up Linux and macOS to build from source.
    - 📄 [`MacOS_Intel_Build.md`](Development/MacOS_Intel_Build.md) — building the macOS app on an Intel Mac with an older Xcode.
    - 📄 [`Stryker_Mutation_Testing.md`](Development/Stryker_Mutation_Testing.md) — running and reading the mutation tests.
  - 📁 **`Images/`**
    - 🖼️ `*.png` — screenshots for [`README.md`](../README.md).
  - 📁 **`Publishing/`**
    - 📁 **`PupNet_Instructions/`**
      - 📄 [`1_Setup.md`](Publishing/PupNet_Instructions/1_Setup.md) — stage 1: the WSL environment.
      - 📄 [`2_Build.md`](Publishing/PupNet_Instructions/2_Build.md) — stage 2: building the packages.
    - 📄 [`GitHub_Release.md`](Publishing/GitHub_Release.md) — checklist for publishing a release.
    - 📄 [`Publish.md`](Publishing/Publish.md) — building the distributables for Windows, Linux and macOS.
    - 📄 [`release-notes.template.md`](Publishing/release-notes.template.md) — release description template.
  - 📄 [`Solution_Files.md`](Solution_Files.md) — this file; `SolutionFilesReferenceTests` checks it against the solution.
- 📁 **`Solution Items/`** — a virtual folder; the files live in the repository root.
  - 📁 **`.config/`**
    - ⚙️ [`dotnet-ef.json`](../.config/dotnet-ef.json) — default options of `dotnet ef`: the migrations project.
    - ⚙️ [`dotnet-tools.json`](../.config/dotnet-tools.json) — local .NET tools.
  - 📁 **`.github/`**
    - 📁 **`ISSUE_TEMPLATE/`**
      - ⚙️ [`bug_report.yml`](../.github/ISSUE_TEMPLATE/bug_report.yml) — bug report form.
      - ⚙️ [`config.yml`](../.github/ISSUE_TEMPLATE/config.yml) — issue form settings.
      - ⚙️ [`feature_request.yml`](../.github/ISSUE_TEMPLATE/feature_request.yml) — feature request form.
    - 📁 **`workflows/`**
      - ⚙️ [`ci.yml`](../.github/workflows/ci.yml) — build and tests on Windows, Linux and macOS.
    - 📄 [`CODE_OF_CONDUCT.md`](../.github/CODE_OF_CONDUCT.md) — code of conduct.
    - 📄 [`CONTRIBUTING.md`](../.github/CONTRIBUTING.md) — how to contribute.
    - 📄 [`PULL_REQUEST_TEMPLATE.md`](../.github/PULL_REQUEST_TEMPLATE.md) — pull request template.
    - ⚙️ [`release.yml`](../.github/release.yml) — categories of the generated release notes.
    - 📄 [`SECURITY.md`](../.github/SECURITY.md) — security policy.
  - 📁 **`.vscode/`** — VS Code only.
    - ⚙️ [`launch.json`](../.vscode/launch.json) — launch configurations.
    - ⚙️ [`settings.json`](../.vscode/settings.json) — workspace settings.
    - ⚙️ [`tasks.json`](../.vscode/tasks.json) — build tasks.
  - ⚙️ [`.editorconfig`](../.editorconfig) — code style and naming rules.
  - ⚙️ [`.gitattributes`](../.gitattributes) — line ending normalisation.
  - ⚙️ [`.gitignore`](../.gitignore) — what stays out of the repository.
  - ⚙️ [`DataOrganizerApp.Tests.slnf`](../DataOrganizerApp.Tests.slnf) — the projects the tests build, without macOS and the installers.
  - ⚙️ [`Directory.Build.props`](../Directory.Build.props) — version, names and assembly metadata of the application, C# settings shared by the projects.
  - ⚙️ [`Directory.Build.targets`](../Directory.Build.targets) — settings that depend on the target framework of a project.
  - ⚙️ [`Directory.Packages.props`](../Directory.Packages.props) — the version of every NuGet package.
  - ⚙️ [`global.json`](../global.json) — the .NET SDK the repository builds with.
  - ⚖️ [`LICENSE`](../LICENSE) — Apache 2.0.
  - ⚖️ [`NOTICE`](../NOTICE) — the notice Apache 2.0 requires.
  - ⚙️ [`nuget.config`](../nuget.config) — the package source.
  - 📄 [`README.md`](../README.md) — the repository front page.
  - ⚙️ [`ResXManager.config.xml`](../ResXManager.config.xml) — settings of the ResXManager extension for this solution: it leaves out the AvaloniaEdit texts, which have no neutral file here.
  - ⚙️ [`Settings.XamlStyler`](../Settings.XamlStyler) — XAML formatting rules.
  - ⚙️ [`stryker-config.json`](../stryker-config.json) — shared settings of the mutation runs.
  - ⚙️ [`stylecop.json`](../stylecop.json) — how the StyleCop rules check: the documentation of non-public members, the order and placement of usings.
  - ⚖️ [`THIRD-PARTY-NOTICES.txt`](../THIRD-PARTY-NOTICES.txt) — third-party licences; generated, never edited by hand.
- 📁 **`tests/`** — the test projects.
  - ⚙️ [`Directory.Build.props`](../tests/Directory.Build.props) — packages and settings shared by the test projects.

**Project files** — not part of the tree above.

**`AvaloniaEdit.Localization`**

- ⚙️ [`SR.ru.resx`](../src/AvaloniaEdit.Localization/SR.ru.resx) — Russian texts of the AvaloniaEdit search panel.

**`DataOrganizer.Desktop`**

- ⚙️ [`app.manifest`](../src/DataOrganizer.Desktop/app.manifest) — Windows application manifest.

**`DataOrganizer.MacOS`**

- ⚙️ [`Info.plist`](../src/DataOrganizer.MacOS/Info.plist) — `.app` bundle metadata.
- ⚙️ [`Roots.xml`](../src/DataOrganizer.MacOS/Roots.xml) — trimmer roots for publishing.
- 🖼️ `Logo.icns` — application icon for macOS.

**`DataOrganizer.Shared`**

- ⚙️ [`Strings.resx`](../src/DataOrganizer.Shared/Properties/Strings.resx) — interface strings, English.
- ⚙️ [`Strings.ru.resx`](../src/DataOrganizer.Shared/Properties/Strings.ru.resx) — interface strings, Russian.

**`DataOrganizer.UI`**

- 🖼️ `Assets/Logo.ico` — application icon.
- 🖼️ `Assets/Logo.svg` — vector icon for the Linux packages.
- 🖼️ `Assets/Logo.256.png` — raster icon for AppImage.
- 🖼️ `Assets/Background.jpg` — background of a record card.

---

📁 folder · 📄 documentation · ⚙️ configuration · 💻 script · ⚖️ legal · 🖼️ images
