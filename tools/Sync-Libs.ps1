<#
.SYNOPSIS
    Copies the Raft and RaftModLoader assemblies the project compiles against into lib\.

.DESCRIPTION
    lib\ is ignored by git because the assemblies belong to their authors. Run this script after
    cloning the repository and after every game or RML update.

    Raft has to be launched once through RMLLauncher beforehand: RML creates the publicized
    Assembly-CSharp.dll on first start.

    Paths are resolved in this order: script parameters, local.props, Steam library lookup,
    defaults. local.props is created when it does not exist.

.EXAMPLE
    powershell -ExecutionPolicy Bypass -File tools\Sync-Libs.ps1

.EXAMPLE
    powershell -ExecutionPolicy Bypass -File tools\Sync-Libs.ps1 -RaftDir "E:\SteamLibrary\steamapps\common\Raft"
#>
[CmdletBinding()]
param(
    [string]$RaftDir,
    [string]$RmlDir
)

$ErrorActionPreference = 'Stop'

$RepoRoot = Split-Path -Parent $PSScriptRoot
$LocalProps = Join-Path $RepoRoot 'local.props'
$LibDir = Join-Path $RepoRoot 'lib'
$RaftAppId = '648800'

function Get-LocalProp([string]$Name) {
    if (-not (Test-Path $LocalProps)) { return $null }
    [xml]$xml = Get-Content -LiteralPath $LocalProps -Raw
    $node = $xml.SelectSingleNode("//*[local-name()='$Name']")
    if ($null -eq $node) { return $null }
    return [Environment]::ExpandEnvironmentVariables($node.InnerText.Replace('$(APPDATA)', '%APPDATA%'))
}

function Find-RaftInSteam {
    $steamPath = $null
    foreach ($key in 'HKCU:\Software\Valve\Steam', 'HKLM:\SOFTWARE\WOW6432Node\Valve\Steam') {
        $item = Get-ItemProperty -Path $key -ErrorAction SilentlyContinue
        if ($item -and $item.SteamPath) { $steamPath = $item.SteamPath; break }
        if ($item -and $item.InstallPath) { $steamPath = $item.InstallPath; break }
    }
    if (-not $steamPath) { return $null }

    $vdf = Join-Path $steamPath 'steamapps\libraryfolders.vdf'
    $libraries = @($steamPath)
    if (Test-Path $vdf) {
        $libraries += Select-String -LiteralPath $vdf -Pattern '"path"\s+"([^"]+)"' -AllMatches |
            ForEach-Object { $_.Matches } |
            ForEach-Object { $_.Groups[1].Value.Replace('\\', '\') }
    }

    foreach ($library in ($libraries | Where-Object { $_ } | Select-Object -Unique)) {
        $manifest = Join-Path $library "steamapps\appmanifest_$RaftAppId.acf"
        if (-not (Test-Path $manifest)) { continue }
        $installDir = (Select-String -LiteralPath $manifest -Pattern '"installdir"\s+"([^"]+)"').Matches[0].Groups[1].Value
        $candidate = Join-Path $library "steamapps\common\$installDir"
        if (Test-Path (Join-Path $candidate 'Raft.exe')) { return $candidate }
    }
    return $null
}

function Copy-Required([string]$Source, [string]$DestinationDir) {
    if (-not (Test-Path -LiteralPath $Source)) { throw "Not found: $Source" }
    Copy-Item -LiteralPath $Source -Destination $DestinationDir -Force
}

if (-not $RaftDir) { $RaftDir = Get-LocalProp 'RaftDir' }
if (-not $RaftDir -or -not (Test-Path (Join-Path $RaftDir 'Raft.exe'))) { $RaftDir = Find-RaftInSteam }
if (-not $RaftDir -or -not (Test-Path (Join-Path $RaftDir 'Raft.exe'))) {
    throw 'Raft installation not found. Pass -RaftDir "<folder with Raft.exe>".'
}

if (-not $RmlDir) { $RmlDir = Get-LocalProp 'RmlDir' }
if (-not $RmlDir) { $RmlDir = Join-Path $env:APPDATA 'RaftModLoader' }
if (-not (Test-Path (Join-Path $RmlDir 'binaries\coremod.dll'))) {
    throw "RaftModLoader binaries not found in '$RmlDir'. Start Raft once through RMLLauncher or pass -RmlDir."
}

$managedSrc = Join-Path $RaftDir 'Raft_Data\Managed'
$publicizedSrc = Join-Path $RmlDir 'cache\publicizedAssemblies\Assembly-CSharp.dll'
$rmlBinaries = Join-Path $RmlDir 'binaries'

$managedDst = Join-Path $LibDir 'Managed'
$rmlDst = Join-Path $LibDir 'RML'
$publicizedDst = Join-Path $LibDir 'Publicized'

foreach ($dir in $managedDst, $rmlDst, $publicizedDst) {
    if (Test-Path $dir) { Remove-Item -LiteralPath $dir -Recurse -Force }
    New-Item -ItemType Directory -Path $dir | Out-Null
}

Get-ChildItem -LiteralPath $managedSrc -Filter '*.dll' | Copy-Item -Destination $managedDst -Force

foreach ($name in '0Harmony.dll', 'coremod.dll', 'coremod.xml', 'HMLCoreLibrary.dll', 'HMLCoreLibrary.xml') {
    Copy-Required (Join-Path $rmlBinaries $name) $rmlDst
}

Copy-Required $publicizedSrc $publicizedDst

if (-not (Test-Path $LocalProps)) {
    $content = @"
<Project>
  <PropertyGroup>
    <RaftDir>$RaftDir</RaftDir>
    <RmlDir>$RmlDir</RmlDir>
  </PropertyGroup>
</Project>
"@
    [IO.File]::WriteAllText($LocalProps, $content, (New-Object Text.UTF8Encoding $false))
    Write-Host "Created local.props"
}

$count = (Get-ChildItem -LiteralPath $managedDst -Filter '*.dll').Count
Write-Host "Raft:  $RaftDir"
Write-Host "RML:   $RmlDir"
Write-Host "lib\Managed: $count assemblies, lib\RML and lib\Publicized updated."
