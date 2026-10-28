#Requires -Version 5.1
<#
.SYNOPSIS
    Runs the Stryker mutation test of each project against its own test projects.

.DESCRIPTION
    Pairs every project X/X.csproj of the repository with X.UnitTests/X.UnitTests.csproj and
    X.IntegrationTests/X.IntegrationTests.csproj, whichever exist. A project without such a test
    project is skipped, and so is a project with Avalonia views (*.axaml): Stryker cannot build it.

    Each project runs from its own folder. From the repository root Stryker would find the solution
    and take every test project that depends on the project, whatever the command line says.
    The settings shared by all runs come from stryker-config.json.

    Reports land in StrykerOutput/<time>/<Project>/reports. At the end a table shows the counts and
    the scores of each project, followed by the methods Stryker dropped in safe mode.

    Pairs with Docs/Stryker_Mutation_Testing.md.

.PARAMETER Project
    Names of the projects to run, as their folders are named, separated by commas without spaces.
    Defaults to every paired project.

.PARAMETER Concurrency
    Number of parallel test sessions. Defaults to the Stryker default: half of the logical processors.

.EXAMPLE
    powershell -ExecutionPolicy Bypass -File tools/run-stryker.ps1

.EXAMPLE
    powershell -ExecutionPolicy Bypass -File tools/run-stryker.ps1 -Project Hierarchy,TextProcessing
#>
[CmdletBinding(PositionalBinding = $false)]
param(
    [string[]]$Project,
    [int]$Concurrency
)

$ErrorActionPreference = 'Stop'
$repoRoot = Split-Path $PSScriptRoot -Parent
$configFile = Join-Path $repoRoot 'stryker-config.json'

# --- Pair each project with its own test projects --------------------------------
function Get-TestProjects([string]$name) {
    foreach ($suffix in 'UnitTests', 'IntegrationTests') {
        $testProject = Join-Path $repoRoot "$name.$suffix\$name.$suffix.csproj"
        if (Test-Path $testProject) {
            $testProject
        }
    }
}

function Test-HasViews([string]$folder) {
    $views = Get-ChildItem $folder -Recurse -File -Filter *.axaml |
        Where-Object { $_.FullName -notmatch '\\(bin|obj)\\' } |
        Select-Object -First 1
    $null -ne $views
}

$pairs = @()
$withViews = @()
foreach ($folder in Get-ChildItem $repoRoot -Directory) {
    if (-not (Test-Path (Join-Path $folder.FullName "$($folder.Name).csproj"))) {
        continue
    }
    $testProjects = @(Get-TestProjects $folder.Name)
    if ($testProjects.Count -eq 0) {
        continue
    }
    if (Test-HasViews $folder.FullName) {
        $withViews += $folder.Name
        continue
    }
    $pairs += [pscustomobject]@{ Name = $folder.Name; Folder = $folder.FullName; TestProjects = $testProjects }
}

if ($Project) {
    # -File passes "A,B" as one string, while a PowerShell prompt passes an array.
    $Project = @($Project -split ',' | Where-Object { $_ })
    foreach ($name in $Project) {
        if ($name -in $withViews) {
            throw "$name has Avalonia views; Stryker cannot build it."
        }
        if ($name -notin $pairs.Name) {
            throw "$name is not a project with its own test project."
        }
    }
    $pairs = @($pairs | Where-Object { $_.Name -in $Project })
} elseif ($withViews) {
    Write-Host "Skipped, as Stryker cannot build Avalonia views: $($withViews -join ', ')" -ForegroundColor Yellow
}

# --- Read a report: the counts by status and the flips of the ConfigureAwait flag --
# A flip of ConfigureAwait(false) to (true) changes nothing a test can see, so the second
# score leaves the flips out.
function Read-Report([string]$reportFile) {
    $report = Get-Content $reportFile -Raw -Encoding UTF8 | ConvertFrom-Json
    $counts = @{}
    $flips = @{}
    foreach ($file in $report.files.PSObject.Properties) {
        $lines = $file.Value.source -split "`n"
        foreach ($mutant in $file.Value.mutants) {
            $counts[$mutant.status] = 1 + $counts[$mutant.status]
            $line = $lines[$mutant.location.start.line - 1]
            $before = $line.Substring(0, [Math]::Min($mutant.location.start.column - 1, $line.Length))
            if ($mutant.mutatorName -like '*Boolean*' -and $before.TrimEnd().EndsWith('ConfigureAwait(')) {
                $flips[$mutant.status] = 1 + $flips[$mutant.status]
            }
        }
    }
    [pscustomobject]@{ Counts = $counts; Flips = $flips }
}

function Get-Score([hashtable]$counts) {
    $detected = [int]$counts['Killed'] + [int]$counts['Timeout']
    $valid = $detected + [int]$counts['Survived'] + [int]$counts['NoCoverage']
    if ($valid -eq 0) {
        return '-'
    }
    '{0:N1} %' -f (100 * $detected / $valid)
}

# --- Methods dropped in safe mode, from the console output of a run ---------------
# A compile error that Stryker cannot pin on one mutant drops every mutant of its method.
function Get-DroppedMethods([object[]]$output) {
    $file = $null
    foreach ($line in $output) {
        if ("$line" -match 'An unidentified mutation in (.+?) resulted in a compile error') {
            $file = Split-Path $Matches[1] -Leaf
        } elseif ("$line" -match 'Safe Mode! Stryker will remove all mutations in (\S+)') {
            "${file}: $($Matches[1])"
        }
    }
}

# --- Run ----------------------------------------------------------------------------
$runFolder = Join-Path $repoRoot ('StrykerOutput\' + (Get-Date -Format 'yyyy-MM-dd.HH-mm-ss'))
$results = @()
$dropped = @()
$failed = @()
foreach ($pair in $pairs) {
    $outputFolder = Join-Path $runFolder $pair.Name
    $arguments = @('dotnet-stryker', '-f', $configFile, '-p', "$($pair.Name).csproj", '-O', $outputFolder)
    foreach ($testProject in $pair.TestProjects) {
        $arguments += '-tp', $testProject
    }
    if ($Concurrency) {
        $arguments += '-c', $Concurrency
    }

    Write-Host "=== $($pair.Name)" -ForegroundColor Cyan
    Push-Location $pair.Folder
    try {
        & dotnet @arguments | Tee-Object -Variable output | Out-Host
    } finally {
        Pop-Location
    }

    $reportFile = Join-Path $outputFolder 'reports\mutation-report.json'
    if (-not (Test-Path $reportFile)) {
        $failed += $pair.Name
        continue
    }
    $report = Read-Report $reportFile
    $withoutFlips = @{}
    foreach ($status in $report.Counts.Keys) {
        $withoutFlips[$status] = $report.Counts[$status] - $report.Flips[$status]
    }
    $results += [pscustomobject][ordered]@{
        Project         = $pair.Name
        Mutants         = ($report.Counts.Values | Measure-Object -Sum).Sum
        Killed          = [int]$report.Counts['Killed']
        Survived        = [int]$report.Counts['Survived']
        NoCoverage      = [int]$report.Counts['NoCoverage']
        Timeout         = [int]$report.Counts['Timeout']
        CompileError    = [int]$report.Counts['CompileError']
        Score           = Get-Score $report.Counts
        'Without flips' = Get-Score $withoutFlips
    }
    $dropped += Get-DroppedMethods $output | Select-Object -Unique | ForEach-Object { "$($pair.Name) - $_" }
}

# --- Summary ------------------------------------------------------------------------
$results | Format-Table -AutoSize | Out-Host
if ($dropped) {
    Write-Host 'Methods dropped in safe mode, to be checked by hand:'
    $dropped | ForEach-Object { Write-Host "  $_" }
}
Write-Host "Reports: $runFolder"
if ($failed) {
    Write-Host "No report, see the output above: $($failed -join ', ')" -ForegroundColor Red
    exit 1
}
