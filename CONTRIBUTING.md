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
dotnet run --project DataOrganizer.Desktop
```

See the [README](README.md#build-from-source) for platform-specific notes.

## Running Tests

Please make sure the test suite passes before opening a pull request:

```bash
dotnet test DataOrganizer.UnitTests/DataOrganizer.UnitTests.csproj
dotnet test Repository.UnitTests/Repository.UnitTests.csproj
dotnet test Shared.UnitTests/Shared.UnitTests.csproj
```

## Coding Guidelines

- Follow the existing code style. It is enforced by the repository
  [`.editorconfig`](.editorconfig) — most editors apply it automatically.
- Write source-code comments in **English**.
- Match the patterns already used in the surrounding code (naming, structure,
  MVVM conventions).
- Keep changes focused; unrelated changes belong in separate pull requests.

## Pull Request Process

1. Create a branch off `master`.
2. Make your changes, with clear and reasonably small commits.
3. Ensure the project builds and all tests pass.
4. Open a pull request describing **what** changed and **why**.
5. Be ready to discuss and address review feedback.

## License

By contributing, you agree that your contributions will be licensed under the
[Apache License 2.0](LICENSE), the same license that covers this project.
