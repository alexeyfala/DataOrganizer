# Code analysis

The build runs the .NET, NUnit and Roslynator analyzers; a change adds no warning.

## Levels of the rules

- The levels live in [`.editorconfig`](../../.editorconfig), section `[*.cs]`. A rule that does not fit is switched
  off there as a whole; the code holds no suppressions.
- Every Roslynator rule is a warning, except the hidden rules listed there as `silent`. When another hidden rule
  fires, fix the code or add the rule to that list.

## Build

The compiler reports the findings of a project only when it compiles the project, so a full rebuild lists them all
(in Visual Studio, Rebuild Solution):

```powershell
dotnet build DataOrganizerApp.slnx --no-incremental -v q --nologo
```

## Report

The findings of every analyzer and code style rule, hints included, grouped by rule. Run it in PowerShell (`pwsh` on
Linux and macOS) from the root of the repository:

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

`--verify-no-changes` keeps the code as it is: without it, `dotnet format` applies the fixes.
