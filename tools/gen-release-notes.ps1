#Requires -Version 5.1
<#
.SYNOPSIS
    Renders the GitHub release notes for the current application version.

.DESCRIPTION
    Reads the application version and name from Directory.Build.props, fills the
    {version} placeholders in the notes template, writes the result to an output
    file, and copies it to the clipboard. The release tag and title are printed to
    the console for pasting into the GitHub "Create a new release" form.

    Pairs with Docs/GitHub_Release.md (the release checklist).

.PARAMETER Version
    Version to render. Defaults to <AppVersion> from Directory.Build.props.

.PARAMETER TemplateFile
    Template to render. Defaults to Docs/release-notes.template.md.

.PARAMETER OutputFile
    Destination file. Defaults to Publish/release-notes.md at the repository root.

.EXAMPLE
    powershell -ExecutionPolicy Bypass -File tools/gen-release-notes.ps1
#>
[CmdletBinding()]
param(
    [string]$Version,
    [string]$TemplateFile,
    [string]$OutputFile
)

$ErrorActionPreference = 'Stop'
$repoRoot = Split-Path $PSScriptRoot -Parent

# Application identity, read from the single source of truth in Directory.Build.props.
$propsFile = Join-Path $repoRoot 'Directory.Build.props'
if (-not (Test-Path $propsFile)) {
    throw "Directory.Build.props not found at $propsFile; cannot determine application identity."
}
[xml]$props = Get-Content $propsFile -Raw
function Get-PropsValue([string]$name) {
    $node = @($props.Project.PropertyGroup.$name) | Where-Object { $_ } | Select-Object -First 1
    ("$node").Trim()
}

if (-not $Version) {
    $Version = Get-PropsValue 'AppVersion'
    if (-not $Version) {
        throw "No <AppVersion> property found in Directory.Build.props; cannot determine the release version."
    }
}
$appName = Get-PropsValue 'AppNameParted'
if (-not $appName) {
    throw "No <AppNameParted> property found in Directory.Build.props; cannot determine the application name."
}

if (-not $TemplateFile) {
    $TemplateFile = Join-Path $repoRoot 'Docs\release-notes.template.md'
}
if (-not (Test-Path $TemplateFile)) {
    throw "Template not found: $TemplateFile."
}
if (-not $OutputFile) {
    $OutputFile = Join-Path $repoRoot 'Publish\release-notes.md'
}

# --- Render: substitute the {version} placeholder --------------------------------
# -Encoding UTF8 is required: Windows PowerShell 5.1 defaults Get-Content to the
# system ANSI codepage, which mangles the em-dashes and arrows in the template.
$template = Get-Content $TemplateFile -Raw -Encoding UTF8
$notes = $template -replace '\{version\}', $Version

# --- Write (UTF-8 without BOM, CRLF) --------------------------------------------
$outDir = Split-Path $OutputFile -Parent
if ($outDir -and -not (Test-Path $outDir)) {
    New-Item -ItemType Directory -Path $outDir -Force | Out-Null
}
$out = $notes -replace "`r`n", "`n" -replace "`r", "`n" -replace "`n", "`r`n"
$utf8 = New-Object System.Text.UTF8Encoding($false)
[System.IO.File]::WriteAllText($OutputFile, $out, $utf8)

# --- Copy to clipboard (Windows only; Set-Clipboard is unavailable elsewhere) ----
$copied = $false
if (Get-Command Set-Clipboard -ErrorAction SilentlyContinue) {
    try {
        Set-Clipboard -Value $out
        $copied = $true
    } catch {
        Write-Warning "Could not copy to clipboard: $($_.Exception.Message)"
    }
} else {
    Write-Warning 'Set-Clipboard is unavailable on this platform; skipped copying to clipboard.'
}

# --- Console summary ------------------------------------------------------------
$clipNote = if ($copied) { '  (copied to clipboard)' } else { '' }
Write-Host "Wrote $OutputFile$clipNote" -ForegroundColor Green
Write-Host ("Tag:   v{0}" -f $Version)
Write-Host ("Title: {0} {1}" -f $appName, $Version)
