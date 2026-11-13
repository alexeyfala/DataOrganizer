# Contributing to Data Organizer

Thanks for your interest in improving Data Organizer! Contributions of all
kinds are welcome — bug reports, feature ideas, documentation, and code.

By participating in this project, you agree to abide by the
[Code of Conduct](CODE_OF_CONDUCT.md).

## Ways to Contribute

- **Report a bug** or **request a feature** by opening an
  [issue](https://github.com/alexeyfala/DataOrganizer/issues).
- **Ask a question** or share an idea in
  [Discussions](https://github.com/alexeyfala/DataOrganizer/discussions).
- **Submit a fix or improvement** via a pull request.

For security vulnerabilities, do **not** open a public issue — follow the
[Security Policy](SECURITY.md) instead.

## Getting Started

**Prerequisites:** [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0).

```bash
git clone https://github.com/alexeyfala/DataOrganizer.git
cd DataOrganizer
dotnet build DataOrganizerApp.slnx
dotnet run --project src/DataOrganizer.Desktop
```

See the [README](../README.md#build-from-source) for platform-specific notes.

## Running Tests

Please make sure the test suite passes before opening a pull request:

```bash
dotnet test DataOrganizerApp.Tests.slnf
```

Two categories mark the tests that read or write outside the process, so a run over the
whole solution can leave them out:

- `Integration` — the tests that need a real database file. Each writes one into a temporary
  folder of its own and removes it again.
- `Guard` — the tests that read the files of the repository (the solution file,
  `Directory.Build.props`, `.vscode`, the markup) instead of exercising the application.

```bash
dotnet test DataOrganizerApp.Tests.slnf --filter "TestCategory!=Integration&TestCategory!=Guard"
```

## Coding Guidelines

- Put new code where [`docs/Architecture.md`](../docs/Architecture.md) places its topic: the projects,
  their topics and the references between them are described there.
- Follow the existing code style. It is enforced by the repository
  [`.editorconfig`](../.editorconfig) — most editors apply it automatically.
- Write source-code comments in **English**.
- Match the patterns already used in the surrounding code (naming, structure,
  MVVM conventions).
- Work with files and folders through the `IFileSystem` interface of
  [Testably.Abstractions](https://github.com/Testably/Testably.Abstractions), not through `File`,
  `Directory` or `FileInfo`, and start, run or find processes through `IProcessLauncher`,
  `IProcessRunner` and `IProcessFinder`, not through `Process`. Test code that works with files
  on the `MockFileSystem` of Testably.
- Keep changes focused; unrelated changes belong in separate pull requests.
- Adding a non-code file — a config, a document, a packaging input? Describe it in
  [`docs/Solution_Files.md`](../docs/Solution_Files.md); a guard test fails the run when a file
  from the solution or from `docs/` is missing there.

## Pull Request Process

1. Create a branch off `master`.
2. Make your changes, with clear and reasonably small commits.
3. Ensure the project builds and all tests pass.
4. Open a pull request describing **what** changed and **why**.
5. Be ready to discuss and address review feedback.

## License

By contributing, you agree that your contributions will be licensed under the
[Apache License 2.0](../LICENSE), the same license that covers this project.
