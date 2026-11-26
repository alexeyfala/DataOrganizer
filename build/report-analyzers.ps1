#Requires -Version 5.1
<#
.SYNOPSIS
    Lists the findings of every analyzer of the solution, grouped by rule.

.DESCRIPTION
    Rebuilds the solution and runs dotnet format for analyzers and for code style without changing the code,
    once for each platform the code is built for. The build reports the warnings of every analyzer; dotnet format
    adds the hints of level info and the code style rules the build leaves out. A finding that both report
    appears once. The messages are in English whatever the language of Windows, so that the two tools agree.

    The report has a line for each platform with the number of its findings, even when there are none: Windows
    lists all its findings, Linux and macOS only those their build adds, from code built for them alone. The
    platform of this machine runs last, so the restored packages and the build output stay its own.

    Each rule shows its level, the number of its findings and its message once. A name that differs between the
    findings, in quotes or a full name that ends the message, stands as three dots in the message and is listed
    at each place. A rule with several kinds of message shows each kind with its own number.

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

# A name in a message: one in quotes, where the apostrophe inside a word, as in "user's", opens none; or a full name
# with dots that ends the message, as CA1707 writes a type name.
$messageName = "(?<=^|[\s(])'[^']+'(?=[\s.,;:)]|$)|(?<=\s)(?:\w+\.)+\w+(?=\.?$)"

# file(line,column): level id: text (help link) [project]
$findingLine = '^\s*(?<file>.+?)\((?<line>\d+),(?<column>\d+)\): (?<level>info|warning|error) (?<id>\w+): (?<text>.+?)(?: \(https?://\S+\))? \[[^\[]+\]$'

# The platforms the code is built for, in the order of the report, each with its value of AppTargetOs.
$platforms = [ordered]@{ Windows = 'Windows'; Linux = 'Linux'; macOS = 'MacOS' }

<#
.SYNOPSIS
    Turns the output of the build and of dotnet format into findings; a finding that both report comes once.
#>
function ConvertTo-Finding([string[]]$Lines) {
    $Lines |
        ForEach-Object {
            if ($_ -match $findingLine) {
                [pscustomobject]@{
                    Key = "$($Matches.file)|$($Matches.line)|$($Matches.column)|$($Matches.id)"
                    Rule = "$($Matches.id)  $($Matches.level)"
                    File = Resolve-Path -Relative $Matches.file
                    Line = [int]$Matches.line
                    Text = $Matches.text
                    Names = @([regex]::Matches($Matches.text, $messageName) | ForEach-Object Value)
                    Template = [regex]::Replace($Matches.text, $messageName, $hiddenName)
                }
            }
        } |
        Sort-Object Key -Unique
}

<#
.SYNOPSIS
    Lists the findings by rule: the level, the number and the message of each rule once, then its places.
#>
function Format-Finding([object[]]$Items) {
    foreach ($rule in $Items | Group-Object Rule | Sort-Object Name) {
        $kinds = @($rule.Group | Group-Object Template | Sort-Object Count -Descending)
        $ruleLine = "$($rule.Name)  ($($rule.Count))"

        ''

        foreach ($kind in $kinds) {
            $places = @($kind.Group | Sort-Object File, Line)
            $first = $places[0]

            # Positions of the names that differ between the findings of this kind of message.
            $varying = @(for ($i = 0; $i -lt $first.Names.Count; $i++) {
                if (@($places | ForEach-Object { $_.Names[$i] } | Sort-Object -Unique).Count -gt 1) {
                    $i
                }
            })

            $counter = @{ Index = -1 }

            $message = [regex]::Replace($first.Text, $messageName, {
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

Push-Location $repoRoot

$consoleEncoding = [Console]::OutputEncoding
$forceUtf8 = $env:DOTNET_CLI_FORCE_UTF8_ENCODING
$invariantGlobalization = $env:DOTNET_SYSTEM_GLOBALIZATION_INVARIANT
$uiLanguage = $env:DOTNET_CLI_UI_LANGUAGE
$targetOs = $env:AppTargetOs

try {
    $env:DOTNET_CLI_UI_LANGUAGE = 'en'

    # Windows PowerShell knows neither $IsLinux nor $IsMacOS, and both are empty there.
    $hostPlatform = if ($IsLinux) { 'Linux' } elseif ($IsMacOS) { 'macOS' } else { 'Windows' }
    $runOrder = @($platforms.Keys | Where-Object { $_ -ne $hostPlatform }) + $hostPlatform
    $findings = @{}

    try {
        # The commands write UTF-8, with a console or without one, and PowerShell reads their output in it.
        [Console]::OutputEncoding = [System.Text.UTF8Encoding]::new($false)
        $env:DOTNET_CLI_FORCE_UTF8_ENCODING = 'true'

        foreach ($platform in $runOrder) {
            # MSBuild reads AppTargetOs as a property, so a build condition on it sees this platform.
            $env:AppTargetOs = $platforms[$platform]
            $env:DOTNET_SYSTEM_GLOBALIZATION_INVARIANT = $invariantGlobalization

            $output = & {
                # dotnet format writes its findings to the error stream, which must not stop the script.
                $ErrorActionPreference = 'Continue'

                dotnet build $solution --no-incremental -v q --nologo

                # dotnet format takes no UI language, only the invariant culture makes it write English. The build
                # stays out of it: it creates the cultures of the satellite resources.
                $env:DOTNET_SYSTEM_GLOBALIZATION_INVARIANT = 'true'

                # Without --verify-no-changes, dotnet format would apply the fixes of the rules.
                dotnet format analyzers $solution --verify-no-changes --severity info --no-restore
                dotnet format style $solution --verify-no-changes --severity info --no-restore
            } 2>&1 | ForEach-Object { "$_" }

            $findings[$platform] = @(ConvertTo-Finding $output)
        }
    }
    finally {
        # The report goes out in the encoding of the console, the one a calling PowerShell reads it in.
        [Console]::OutputEncoding = $consoleEncoding
        $env:DOTNET_CLI_FORCE_UTF8_ENCODING = $forceUtf8
    }

    $windowsKeys = @($findings.Windows | ForEach-Object Key)

    foreach ($platform in $platforms.Keys) {
        # Linux and macOS list only what their build adds to the one of Windows.
        $items = @($findings[$platform] | Where-Object { $platform -eq 'Windows' -or $windowsKeys -notcontains $_.Key })

        # Windows comes first and opens the report.
        if ($platform -ne 'Windows') {
            ''
        }

        "$platform  ($($items.Count))"

        Format-Finding $items
    }
}
finally {
    $env:DOTNET_SYSTEM_GLOBALIZATION_INVARIANT = $invariantGlobalization
    $env:DOTNET_CLI_UI_LANGUAGE = $uiLanguage
    $env:AppTargetOs = $targetOs
    Pop-Location
}
