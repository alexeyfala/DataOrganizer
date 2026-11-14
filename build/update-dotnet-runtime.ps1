#Requires -Version 5.1
<#
.SYNOPSIS
    Pins the bundle to the latest .NET runtime patch of the target framework.

.DESCRIPTION
    Reads <AppTargetFramework> from Directory.Build.props, takes the latest runtime
    patch of that .NET channel and the SHA-512 of its x64 Windows installer from
    Microsoft's release metadata, and writes both into src/Bundle/Bundle.wixproj
    (<DotNetVersion>, <DotNetInstallerSha512>). Changes nothing when the bundle
    already pins that patch.

    The next build of the bundle downloads the new installer into artifacts; the
    installer of the previous patch stays there until it is deleted by hand.

.EXAMPLE
    powershell -ExecutionPolicy Bypass -File build/update-dotnet-runtime.ps1
#>
[CmdletBinding()]
param()

$ErrorActionPreference = 'Stop'
$repoRoot = Split-Path $PSScriptRoot -Parent

# The .NET channel, read from the single source of truth in Directory.Build.props.
[xml]$props = Get-Content (Join-Path $repoRoot 'Directory.Build.props') -Raw
$framework = "$($props.SelectSingleNode('//AppTargetFramework').InnerText)".Trim()
if ($framework -notmatch '^net(\d+\.\d+)$') {
    throw "Unexpected <AppTargetFramework> '$framework' in Directory.Build.props; expected netX.Y."
}
$channel = $Matches[1]

# --- Latest runtime patch of the channel and its installer checksum ------------
$metadata = Invoke-RestMethod "https://builds.dotnet.microsoft.com/dotnet/release-metadata/$channel/releases.json"
$version = $metadata.'latest-runtime'
$release = @($metadata.releases | Where-Object { $_.runtime.version -eq $version }) | Select-Object -First 1
$installer = @($release.runtime.files | Where-Object { $_.name -eq 'dotnet-runtime-win-x64.exe' }) | Select-Object -First 1
if (-not $installer -or -not $installer.hash) {
    throw "No x64 Windows runtime installer of .NET $version in the release metadata of channel $channel."
}
$hash = $installer.hash

# --- Write both values into the bundle project ---------------------------------
# ReadAllText drops the BOM and WriteAllText puts it back; line endings stay as they are.
$projectFile = Join-Path $repoRoot 'src\Bundle\Bundle.wixproj'
$text = [System.IO.File]::ReadAllText($projectFile)
$versionPattern = '(<DotNetVersion>)[^<]*(</DotNetVersion>)'
$hashPattern = '(<DotNetInstallerSha512>)[^<]*(</DotNetInstallerSha512>)'
foreach ($pattern in $versionPattern, $hashPattern) {
    if ([regex]::Matches($text, $pattern).Count -ne 1) {
        throw "Expected exactly one match of $pattern in $projectFile."
    }
}
$oldVersion = [regex]::Match($text, $versionPattern).Value -replace '<[^>]+>', ''
$oldHash = [regex]::Match($text, $hashPattern).Value -replace '<[^>]+>', ''
if ($oldVersion -eq $version -and $oldHash -eq $hash) {
    Write-Host "The bundle already pins the .NET runtime $version."
    return
}

$text = [regex]::Replace($text, $versionPattern, '${1}' + $version + '${2}')
$text = [regex]::Replace($text, $hashPattern, '${1}' + $hash + '${2}')
[System.IO.File]::WriteAllText($projectFile, $text, (New-Object System.Text.UTF8Encoding($true)))
Write-Host "The bundle now pins the .NET runtime $version (was $oldVersion)."
