# Code analysis

The build runs analyzers over every C# project of the solution. A rule the code breaks shows as a warning of the
build and in the Error List of Visual Studio; a change adds no warning.

## Analyzers

- **.NET analyzers** (`CA…`, `SYSLIB…`) come with the .NET SDK.
- **Code style rules** (`IDE…`) come with the SDK too, and the build runs them as well. A few of them, such as IDE0001
  and IDE0002, show only in Visual Studio and in the report below.
- **NUnit.Analyzers** (`NUnit…`) check the test projects.
- **Roslynator** (`RCS…`) comes from the `Roslynator.Analyzers` package, which every C# project references. The
  Roslynator extension for Visual Studio brings refactorings and code fixes but no analyzers: without the package, no
  `RCS` rule runs.

## Levels of the rules

The levels live in [`.editorconfig`](../../.editorconfig), section `[*.cs]`, as
`dotnet_diagnostic.<ID>.severity = warning | suggestion | silent | none`.

- Every rule Roslynator enables is a warning: one line sets the level of the whole category. The rules Roslynator
  hides by default and the code does not follow are set back to `silent`. When another hidden rule fires later, it
  shows as a warning: fix the code or add the rule to that list.
- Some Roslynator rules work only with an option (`roslynator_…`) and stay silent without it. The rule ROS0003, off by
  default, names such rules: after a package update, set `dotnet_diagnostic.ROS0003.severity = warning` for one build.
- A rule that does not fit is switched off in `.editorconfig` as a whole. The code holds no suppressions: no
  `#pragma warning disable`, no `[SuppressMessage]`.

## Seeing the findings

### Build

The compiler reports the findings of a project only when it compiles the project, so a build that skips up-to-date
projects shows no warnings for them. A full rebuild lists them all:

```powershell
dotnet build DataOrganizerApp.slnx --no-incremental -v q --nologo
```

In Visual Studio, Rebuild Solution puts the same warnings into the Output window and the Error List.

### Report

The report lists the findings of every analyzer and code style rule, hints of level `info` included, grouped by rule.
It runs two `dotnet format` commands and takes about a minute and a half. Run it in PowerShell (on Linux and macOS in
`pwsh`) from the root of the repository:

```powershell
& {
    dotnet format analyzers DataOrganizerApp.slnx --verify-no-changes --severity info --no-restore
    dotnet format style DataOrganizerApp.slnx --verify-no-changes --severity info --no-restore
} 2>&1 |
    ForEach-Object { "$_" } |
    Sort-Object -Unique |
    ForEach-Object {
        if ($_ -match '^\s*(?<file>.+?)\((?<line>\d+),\d+\): (?<level>\w+) (?<id>\w+): (?<text>.+) \[[^\[]+\]$') {
            [pscustomobject]@{ Rule = "$($Matches.id)  $($Matches.level)  $($Matches.text)"; Place = "$(Resolve-Path -Relative $Matches.file):$($Matches.line)" }
        }
    } |
    Group-Object Rule |
    Sort-Object Name |
    ForEach-Object { ''; $_.Name; $_.Group | ForEach-Object { "    $($_.Place)" } }
```

`--verify-no-changes` keeps the code as it is: without it, `dotnet format` applies the fixes of the rules.
