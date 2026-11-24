# Code analysis

The build runs the .NET, NUnit and Roslynator analyzers; a change adds no warning.

## Levels of the rules

- The levels live in [`.editorconfig`](../../.editorconfig), section `[*.cs]`. A rule that does not fit is switched
  off there as a whole; the code holds no suppressions.
- The .NET analyzers run in the `Recommended` mode (`AnalysisMode` in `Directory.Build.props`), which reports more of
  their rules as warnings.
- Every Roslynator rule is a warning, except the hidden rules listed there as `silent`. When another hidden rule
  fires, fix the code or add the rule to that list.

## Build

The compiler reports the findings of a project only when it compiles the project, so a full rebuild lists them all
(in Visual Studio, Rebuild Solution):

```powershell
dotnet build DataOrganizerApp.slnx --no-incremental -v q --nologo
```

## Report

The findings of every analyzer and code style rule, hints included, grouped by rule: each rule with its number of
findings and its message once, then the places. The build gives the warnings, `dotnet format` the hints and the code
style rules the build does not report; the code stays as it is. On Linux and macOS, run the script with `pwsh`:

```powershell
powershell -ExecutionPolicy Bypass -File build/report-analyzers.ps1
```
