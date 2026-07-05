#Requires -Version 5.1
<#
.SYNOPSIS
    Generates Setup/LICENSE.rtf from the plain-text LICENSE (single source of truth).

.DESCRIPTION
    The WiX installer license dialog (WixUILicenseRtf) can only display RTF, so the
    plain-text LICENSE is wrapped into an RTF document. LICENSE is the only
    hand-maintained copy; Setup/LICENSE.rtf is a generated build artifact and is
    git-ignored. This script runs automatically from Setup.wixproj's PreBuild.

.PARAMETER LicenseFile
    Source plain-text license. Defaults to LICENSE at the repository root.

.PARAMETER OutputFile
    Destination RTF. Defaults to Setup/LICENSE.rtf.

.PARAMETER FontName
    Monospace font that preserves the license's fixed-width layout.

.PARAMETER FontSizePt
    Font size in points.

.EXAMPLE
    powershell -NoProfile -ExecutionPolicy Bypass -File tools/gen-license-rtf.ps1
#>
[CmdletBinding()]
param(
    [string]$LicenseFile,
    [string]$OutputFile,
    [string]$FontName = 'Consolas',
    [int]$FontSizePt = 9
)

$ErrorActionPreference = 'Stop'
$repoRoot = Split-Path $PSScriptRoot -Parent

if (-not $LicenseFile) { $LicenseFile = Join-Path $repoRoot 'LICENSE' }
if (-not $OutputFile)  { $OutputFile  = Join-Path $repoRoot 'Setup\LICENSE.rtf' }

if (-not (Test-Path $LicenseFile)) {
    throw "License source not found at $LicenseFile."
}

# Escape one line for RTF: backslash/braces, and non-ASCII as \uN unicode runs.
function ConvertTo-RtfLine([string]$line) {
    $sb = New-Object System.Text.StringBuilder
    foreach ($ch in $line.ToCharArray()) {
        $code = [int][char]$ch
        switch ($ch) {
            '\' { [void]$sb.Append('\\') }
            '{' { [void]$sb.Append('\{') }
            '}' { [void]$sb.Append('\}') }
            default {
                if ($code -gt 127) {
                    # RTF \uN takes a signed 16-bit value; '?' is the ANSI fallback char.
                    $signed = if ($code -gt 32767) { $code - 65536 } else { $code }
                    [void]$sb.Append("\u$signed?")
                } else {
                    [void]$sb.Append($ch)
                }
            }
        }
    }
    $sb.ToString()
}

$lines = [System.IO.File]::ReadAllText($LicenseFile) -split "`r`n|`r|`n"
# Drop a single trailing empty line produced by a final newline.
if ($lines.Count -gt 0 -and $lines[-1] -eq '') { $lines = $lines[0..($lines.Count - 2)] }

$fs = $FontSizePt * 2   # RTF \fs is in half-points
$sb = New-Object System.Text.StringBuilder
[void]$sb.Append("{\rtf1\ansi\ansicpg1252\deff0{\fonttbl{\f0\fmodern\fcharset0 $FontName;}}`r`n")
[void]$sb.Append("\fs$fs`r`n")
foreach ($line in $lines) {
    [void]$sb.Append((ConvertTo-RtfLine $line) + "\par`r`n")
}
[void]$sb.Append("}`r`n")

[System.IO.File]::WriteAllText($OutputFile, $sb.ToString(), [System.Text.Encoding]::ASCII)
Write-Host "Wrote $OutputFile ($FontName ${FontSizePt}pt, $($lines.Count) lines)" -ForegroundColor Green
