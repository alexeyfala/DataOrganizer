#Requires -Version 5.1
<#
.SYNOPSIS
    Lists the findings of every analyzer of the solution, grouped by rule.

.DESCRIPTION
    Rebuilds the solution and runs dotnet format for analyzers and for code style without changing the code.
    The build reports the warnings of every analyzer; dotnet format adds the hints of level info and the code
    style rules the build leaves out. A finding that both report appears once.

    Each rule shows its level, the number of its findings and its message once. A name in quotes that differs
    between the findings stands as an ellipsis in the message and is listed at each place. A rule with several
    kinds of message shows each kind with its own number.

    Pairs with docs/Development/Code_Analysis.md.

.EXAMPLE
    powershell -ExecutionPolicy Bypass -File build/report-analyzers.ps1

.EXAMPLE
    powershell -ExecutionPolicy Bypass -File build/report-analyzers.ps1 | Set-Content -Encoding utf8 report.txt
#>
[CmdletBinding()]
param()

$ErrorActionPreference = 'Stop'
$repoRoot = Split-Path $PSScriptRoot -Parent
$solution = Join-Path $repoRoot 'DataOrganizerApp.slnx'
$hiddenName = "'...'"

# A name in quotes; the apostrophe inside a word, as in "user's", opens none.
$quotedName = "(?<=^|[\s(])'[^']+'(?=[\s.,;:)]|$)"

# file(line,column): level id: text (help link) [project]
$findingLine = '^\s*(?<file>.+?)\((?<line>\d+),(?<column>\d+)\): (?<level>info|warning|error) (?<id>\w+): (?<text>.+?)(?: \(https?://\S+\))? \[[^\[]+\]$'

Push-Location $repoRoot

$consoleEncoding = [Console]::OutputEncoding
$forceUtf8 = $env:DOTNET_CLI_FORCE_UTF8_ENCODING

try {
    try {
        # The commands write UTF-8, with a console or without one, and PowerShell reads their output in it.
        [Console]::OutputEncoding = [System.Text.UTF8Encoding]::new($false)
        $env:DOTNET_CLI_FORCE_UTF8_ENCODING = 'true'

        $output = & {
            # dotnet format writes its findings to the error stream, which must not stop the script.
            $ErrorActionPreference = 'Continue'

            dotnet build $solution --no-incremental -v q --nologo

            # Without --verify-no-changes, dotnet format would apply the fixes of the rules.
            dotnet format analyzers $solution --verify-no-changes --severity info --no-restore
            dotnet format style $solution --verify-no-changes --severity info --no-restore
        } 2>&1 | ForEach-Object { "$_" }
    }
    finally {
        # The report goes out in the encoding of the console, the one a calling PowerShell reads it in.
        [Console]::OutputEncoding = $consoleEncoding
        $env:DOTNET_CLI_FORCE_UTF8_ENCODING = $forceUtf8
    }

    $findings = $output |
        ForEach-Object {
            if ($_ -match $findingLine) {
                [pscustomobject]@{
                    Key = "$($Matches.file)|$($Matches.line)|$($Matches.column)|$($Matches.id)"
                    Rule = "$($Matches.id)  $($Matches.level)"
                    File = Resolve-Path -Relative $Matches.file
                    Line = [int]$Matches.line
                    Text = $Matches.text
                    Names = @([regex]::Matches($Matches.text, $quotedName) | ForEach-Object Value)
                    Template = [regex]::Replace($Matches.text, $quotedName, $hiddenName)
                }
            }
        } |
        Sort-Object Key -Unique

    foreach ($rule in $findings | Group-Object Rule | Sort-Object Name) {
        $kinds = @($rule.Group | Group-Object Template | Sort-Object Count -Descending)
        $ruleLine = "$($rule.Name)  ($($rule.Count))"

        ''

        foreach ($kind in $kinds) {
            $places = @($kind.Group | Sort-Object File, Line)
            $first = $places[0]

            # Positions of the quoted names that differ between the findings of this kind of message.
            $varying = @(for ($i = 0; $i -lt $first.Names.Count; $i++) {
                if (@($places | ForEach-Object { $_.Names[$i] } | Sort-Object -Unique).Count -gt 1) {
                    $i
                }
            })

            $counter = @{ Index = -1 }

            $message = [regex]::Replace($first.Text, $quotedName, {
                param($match)

                $counter.Index++

                if ($varying -contains $counter.Index) { $hiddenName } else { $match.Value }
            })

            if ($kinds.Count -eq 1) {
                "$ruleLine  $message"
            }
            else {
                if ($ruleLine) {
                    $ruleLine
                    $ruleLine = $null
                }

                "  $message  ($($places.Count))"
            }

            foreach ($place in $places) {
                $names = ($varying | ForEach-Object { $place.Names[$_] }) -join ', '

                if ($names) { "    $($place.File):$($place.Line)  $names" } else { "    $($place.File):$($place.Line)" }
            }
        }
    }
}
finally {
    Pop-Location
}
