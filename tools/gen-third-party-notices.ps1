#Requires -Version 5.1
<#
.SYNOPSIS
    Regenerates THIRD-PARTY-NOTICES.txt from the application's resolved NuGet graph.

.DESCRIPTION
    Reads the restored dependency graph of the distributed project (default:
    DataOrganizer.Desktop) from its project.assets.json, looks up each package's
    license and authors from the .nuspec files in the NuGet cache, groups the
    components by license, and writes THIRD-PARTY-NOTICES.txt at the repository
    root.

    The manual "libuiohook (LGPL)" section is always emitted first: libuiohook is
    a native library bundled inside the SharpHook package and does not appear as a
    NuGet package in the graph, so it cannot be discovered automatically.

    Run this before a release (or whenever the set of dependencies changes).
    Restore the project first (dotnet restore / build) so project.assets.json is
    up to date.

.PARAMETER AssetsFile
    One or more project.assets.json files to union. Defaults to the Desktop host.

.PARAMETER OutputFile
    Destination file. Defaults to THIRD-PARTY-NOTICES.txt at the repository root.

.EXAMPLE
    pwsh tools/gen-third-party-notices.ps1
#>
[CmdletBinding()]
param(
    [string[]]$AssetsFile,
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
$appLicense = Get-PropsValue 'License'
if (-not $appLicense) {
    throw "No <License> property found in Directory.Build.props; cannot determine the application license."
}
$appName = Get-PropsValue 'AppNameParted'
if (-not $appName) {
    throw "No <AppNameParted> property found in Directory.Build.props; cannot determine the application name."
}

if (-not $AssetsFile) {
    $AssetsFile = @(Join-Path $repoRoot 'DataOrganizer.Desktop\obj\project.assets.json')
}
if (-not $OutputFile) {
    $OutputFile = Join-Path $repoRoot 'THIRD-PARTY-NOTICES.txt'
}

# --- Overrides for packages whose .nuspec does not carry an SPDX expression -----
# Keyed by lowercase package id. Verified manually against the package/repository.
$licenseOverrides = @{
    'avalonia.angle.windows.natives' = 'BSD-3-Clause'   # ANGLE, file-based license
    'interop.uiautomationclient'     = 'MIT'            # file-based LICENSE.txt (MIT)
    'proitemsrepeater'               = 'MIT'            # package omits license; author publishes under MIT
}

# --- Embedded full license texts (reused across all matching components) ---------
$mitText = @'
    Permission is hereby granted, free of charge, to any person obtaining a
    copy of this software and associated documentation files (the "Software"),
    to deal in the Software without restriction, including without limitation
    the rights to use, copy, modify, merge, publish, distribute, sublicense,
    and/or sell copies of the Software, and to permit persons to whom the
    Software is furnished to do so, subject to the following conditions:

    The above copyright notice and this permission notice shall be included in
    all copies or substantial portions of the Software.

    THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
    IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
    FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
    AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
    LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING
    FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER
    DEALINGS IN THE SOFTWARE.
'@

$bsd3Text = @'
    Redistribution and use in source and binary forms, with or without
    modification, are permitted provided that the following conditions are met:

      * Redistributions of source code must retain the above copyright notice,
        this list of conditions and the following disclaimer.
      * Redistributions in binary form must reproduce the above copyright
        notice, this list of conditions and the following disclaimer in the
        documentation and/or other materials provided with the distribution.
      * Neither the name of the copyright holder nor the names of its
        contributors may be used to endorse or promote products derived from
        this software without specific prior written permission.

    THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS "AS IS"
    AND ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE
    IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE
    ARE DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT HOLDER OR CONTRIBUTORS BE
    LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR
    CONSEQUENTIAL DAMAGES (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF
    SUBSTITUTE GOODS OR SERVICES; LOSS OF USE, DATA, OR PROFITS; OR BUSINESS
    INTERRUPTION) HOWEVER CAUSED AND ON ANY THEORY OF LIABILITY, WHETHER IN
    CONTRACT, STRICT LIABILITY, OR TORT (INCLUDING NEGLIGENCE OR OTHERWISE)
    ARISING IN ANY WAY OUT OF THE USE OF THIS SOFTWARE, EVEN IF ADVISED OF THE
    POSSIBILITY OF SUCH DAMAGE.
'@

$iscText = @'
    Permission to use, copy, modify, and/or distribute this software for any
    purpose with or without fee is hereby granted, provided that the above
    copyright notice and this permission notice appear in all copies.

    THE SOFTWARE IS PROVIDED "AS IS" AND THE AUTHOR DISCLAIMS ALL WARRANTIES
    WITH REGARD TO THIS SOFTWARE INCLUDING ALL IMPLIED WARRANTIES OF
    MERCHANTABILITY AND FITNESS. IN NO EVENT SHALL THE AUTHOR BE LIABLE FOR
    ANY SPECIAL, DIRECT, INDIRECT, OR CONSEQUENTIAL DAMAGES OR ANY DAMAGES
    WHATSOEVER RESULTING FROM LOSS OF USE, DATA OR PROFITS, WHETHER IN AN
    ACTION OF CONTRACT, NEGLIGENCE OR OTHER TORTIOUS ACTION, ARISING OUT OF OR
    IN CONNECTION WITH THE USE OR PERFORMANCE OF THIS SOFTWARE.
'@

# The "reproduced in the LICENSE file" phrasing only holds when the application
# itself is Apache-2.0 (its LICENSE file then contains the Apache text).
if ($appLicense -eq 'Apache-2.0') {
    $apacheRef = @'
    These components are licensed under the Apache License, Version 2.0. The full
    text of the Apache License 2.0 is reproduced in the LICENSE file that
    accompanies this software and is also available at:

        http://www.apache.org/licenses/LICENSE-2.0
'@
} else {
    $apacheRef = @'
    These components are licensed under the Apache License, Version 2.0. The full
    text of the Apache License 2.0 is available at:

        http://www.apache.org/licenses/LICENSE-2.0
'@
}

# License body lookup: SPDX -> descriptive line + full/reference text.
$licenseBodies = @{
    'MIT'          = @{ Title = 'MIT License (MIT)';           Body = "The above components are licensed under the MIT License. Copyright is held by`r`nthe respective authors and contributors of each component. The MIT License`r`ntext is reproduced below:`r`n`r`n$mitText" }
    'Apache-2.0'   = @{ Title = 'Apache License, Version 2.0 (Apache-2.0)'; Body = $apacheRef }
    'BSD-3-Clause' = @{ Title = 'BSD 3-Clause License (BSD-3-Clause)'; Body = "The above components are licensed under the BSD 3-Clause License:`r`n`r`n$bsd3Text" }
    'ISC'          = @{ Title = 'ISC License (ISC)';           Body = "The above components are licensed under the ISC License:`r`n`r`n$iscText" }
}

# --- Read the resolved package set from the assets file(s) -----------------------
$packages = @{}   # "id/version" -> [pscustomobject]
$pkgFolders = @()

foreach ($af in $AssetsFile) {
    if (-not (Test-Path $af)) {
        throw "Assets file not found: $af. Run 'dotnet restore' first."
    }
    $assets = Get-Content $af -Raw | ConvertFrom-Json
    foreach ($pf in $assets.packageFolders.PSObject.Properties.Name) { $pkgFolders += $pf }
    foreach ($target in $assets.targets.PSObject.Properties.Value) {
        foreach ($lib in $target.PSObject.Properties) {
            if ($lib.Value.type -ne 'package') { continue }
            if (-not $packages.ContainsKey($lib.Name)) { $packages[$lib.Name] = $true }
        }
    }
}
if ($pkgFolders.Count -eq 0) { $pkgFolders = @("$env:USERPROFILE\.nuget\packages") }

# --- Resolve license + authors for each package ---------------------------------
$components = foreach ($key in $packages.Keys) {
    $parts = $key -split '/'
    $id = $parts[0]; $ver = $parts[1]
    $nuspec = $null
    foreach ($root in $pkgFolders) {
        $candidate = Join-Path $root ("$id\$ver\$id.nuspec")
        if (Test-Path $candidate) { $nuspec = $candidate; break }
    }

    $spdx = $null; $authors = 'its authors and contributors'
    if ($nuspec) {
        [xml]$x = Get-Content $nuspec -Raw
        $md = $x.package.metadata
        if ($md.authors) { $authors = ($md.authors -replace '\s+', ' ').Trim() }
        $lic = $md.license
        if ($lic -and $lic.type -eq 'expression' -and $lic.'#text') { $spdx = $lic.'#text'.Trim() }
    }
    if (-not $spdx -and $licenseOverrides.ContainsKey($id.ToLowerInvariant())) {
        $spdx = $licenseOverrides[$id.ToLowerInvariant()]
    }
    if (-not $spdx) { $spdx = 'UNKNOWN' }

    [pscustomobject]@{ Id = $id; Version = $ver; Spdx = $spdx; Authors = $authors }
}

# --- Compose the file -----------------------------------------------------------
$nl = "`r`n"
$sb = New-Object System.Text.StringBuilder
function Add-Line([string]$s = '') { [void]$sb.Append($s + $nl) }

Add-Line 'THIRD-PARTY SOFTWARE NOTICES AND INFORMATION'
Add-Line '============================================'
Add-Line
Add-Line "$appName incorporates, links against, or bundles the third-party"
Add-Line 'components listed below. Each component is the property of its respective'
Add-Line 'authors and is used under the license indicated. This file is provided to'
Add-Line 'comply with the attribution and notice requirements of those licenses.'
Add-Line
Add-Line 'This file is generated by tools/gen-third-party-notices.ps1 from the resolved'
Add-Line 'NuGet dependency graph. The libuiohook (LGPL) section below is maintained'
Add-Line 'manually because it is a native library bundled inside the SharpHook package.'

$section = 0
function Add-Header([string]$title) {
    $script:section++
    Add-Line
    Add-Line '--------------------------------------------------------------------------'
    Add-Line ("{0}. {1}" -f $script:section, $title)
    Add-Line '--------------------------------------------------------------------------'
    Add-Line
}

# Section 1: manual LGPL (libuiohook)
Add-Header 'GNU Lesser General Public License, version 3.0 or later (LGPL-3.0-or-later)'
Add-Line '  * libuiohook'
Add-Line '      Copyright (c) 2006-2023 Alexander Barker.'
Add-Line '      https://github.com/kwhat/libuiohook'
Add-Line
Add-Line 'libuiohook is a native C library that provides the global keyboard/mouse'
Add-Line 'hooks used by this application. It is distributed as a native binary'
Add-Line '(uiohook.dll / libuiohook.so / libuiohook.dylib) bundled inside the SharpHook'
Add-Line 'NuGet package and is loaded dynamically at runtime (via P/Invoke).'
Add-Line
Add-Line 'This component is licensed under the GNU Lesser General Public License,'
Add-Line 'version 3.0 or later. In accordance with the LGPL, the native library is'
Add-Line 'linked dynamically and may be replaced by the end user with a compatible'
Add-Line 'version of their own. The full text of the LGPL v3.0 and the GNU GPL v3.0'
Add-Line '(which the LGPL incorporates by reference) is available at:'
Add-Line
Add-Line '    https://www.gnu.org/licenses/lgpl-3.0.txt'
Add-Line '    https://www.gnu.org/licenses/gpl-3.0.txt'
Add-Line
Add-Line 'The corresponding source code for libuiohook is available from the project'
Add-Line 'repository listed above.'

# Remaining sections: generated groups, in a stable, sensible order.
$order = @('Apache-2.0', 'MIT', 'BSD-3-Clause', 'ISC')
$known = $components | Group-Object Spdx
$emitted = @{}

function Add-Group($spdx, $items) {
    $meta = $licenseBodies[$spdx]
    $title = if ($meta) { $meta.Title } else { $spdx }
    Add-Header $title
    foreach ($c in ($items | Sort-Object Id)) {
        Add-Line ("  * {0} {1} - Copyright (c) {2}" -f $c.Id, $c.Version, $c.Authors)
    }
    Add-Line
    if ($meta) {
        Add-Line $meta.Body
    } else {
        Add-Line "The above components are licensed under $spdx. Refer to each package or"
        Add-Line 'its repository for the authoritative license text.'
    }
}

foreach ($spdx in $order) {
    $g = $known | Where-Object { $_.Name -eq $spdx }
    if ($g) { Add-Group $spdx $g.Group; $emitted[$spdx] = $true }
}
# Any other known SPDX not in the preferred order (alphabetical), UNKNOWN last.
foreach ($g in ($known | Sort-Object Name)) {
    if ($emitted.ContainsKey($g.Name)) { continue }
    if ($g.Name -eq 'UNKNOWN') { continue }
    Add-Group $g.Name $g.Group; $emitted[$g.Name] = $true
}
$unknown = $known | Where-Object { $_.Name -eq 'UNKNOWN' }
if ($unknown) {
    Add-Group 'UNKNOWN (review manually and add to $licenseOverrides in the generator)' $unknown.Group
}

# Notes
Add-Line
Add-Line '--------------------------------------------------------------------------'
Add-Line 'Notes'
Add-Line '--------------------------------------------------------------------------'
Add-Line
Add-Line 'Version numbers reflect the dependencies resolved at generation time and may'
Add-Line "change between releases. Copyright holders are taken from each package's"
Add-Line 'metadata; where a holder could not be confirmed, copyright is attributed to'
Add-Line 'the respective authors and contributors. For the authoritative and complete'
Add-Line 'license text of any component, refer to the package or repository of that'
Add-Line 'component.'

# --- Write (UTF-8 without BOM, CRLF) --------------------------------------------
# Normalize to CRLF: embedded here-string blocks may carry LF-only newlines.
$out = $sb.ToString() -replace "`r`n", "`n" -replace "`r", "`n" -replace "`n", "`r`n"
$utf8 = New-Object System.Text.UTF8Encoding($false)
[System.IO.File]::WriteAllText($OutputFile, $out, $utf8)

# --- Console summary ------------------------------------------------------------
Write-Host "Wrote $OutputFile" -ForegroundColor Green
Write-Host ("Components: {0}" -f $components.Count)
$components | Group-Object Spdx | Sort-Object Name | ForEach-Object {
    Write-Host ("  {0,-16} {1}" -f $_.Name, $_.Count)
}
if ($unknown) {
    Write-Host 'WARNING: packages with UNKNOWN license (add them to $licenseOverrides):' -ForegroundColor Yellow
    $unknown.Group | ForEach-Object { Write-Host ("  {0} {1}" -f $_.Id, $_.Version) -ForegroundColor Yellow }
}
