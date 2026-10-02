#Requires -Version 5.1
[CmdletBinding()]
param([switch]$SkipTests)
$ErrorActionPreference = 'Stop'
$repositoryRoot = Split-Path -Parent $PSScriptRoot
. (Join-Path $PSScriptRoot 'package-helpers.ps1')
$outputRoot = Join-Path $repositoryRoot 'output'
New-Item -ItemType Directory -Path $outputRoot -Force | Out-Null
try {
    # Keep the lock file; deleting it after releasing a handle creates a race with
    # the next caller. The exclusive handle serializes builds as well as numbering.
    $packageLock = [IO.File]::Open((Join-Path $outputRoot '.package.lock'), [IO.FileMode]::OpenOrCreate, [IO.FileAccess]::ReadWrite, [IO.FileShare]::None)
} catch {
    throw ('Cannot acquire the package lock. Another package may be running, or output is not writable. ' + $_.Exception.Message)
}
$batchRoot = $null
$manifest = $null
function Save-Manifest {
    $json = $manifest | ConvertTo-Json -Depth 8
    [IO.File]::WriteAllText((Join-Path $batchRoot 'manifest.json'), ($json + [Environment]::NewLine), (New-Object Text.UTF8Encoding($false)))
}
function Invoke-PackageStep([string]$ScriptPath, [string]$LogPath, [hashtable]$Arguments = @{}) {
    $writer = New-Object IO.StreamWriter($LogPath, $false, (New-Object Text.UTF8Encoding($false)))
    try {
        & $ScriptPath @Arguments *>&1 | ForEach-Object {
            $writer.WriteLine(($_ | Out-String).TrimEnd())
            $_ | Out-Host
        }
    } catch {
        $writer.WriteLine($_.ToString())
        throw
    } finally { $writer.Dispose() }
}
try {
    $startedAt = ConvertTo-PackageTime ([DateTimeOffset]::UtcNow)
    $batchRoot = New-PackageBatch $outputRoot $startedAt
    $manifest = [ordered]@{
        schemaVersion = 1
        application = 'CodexUsage'
        version = $null
        platform = 'Windows-x64'
        timeZone = 'Asia/Shanghai'
        batch = (Split-Path -Leaf $batchRoot)
        startedAt = $startedAt.ToString('o')
        finishedAt = $null
        status = 'running'
        phase = 'build'
        skipTests = [bool]$SkipTests
        build = [ordered]@{ status = 'pending'; command = 'scripts/build.ps1'; log = 'build.log' }
        tests = [ordered]@{ status = 'pending'; command = 'scripts/test.ps1 -Suite All'; log = 'tests.log' }
        powershellVersion = $PSVersionTable.PSVersion.ToString()
        source = [ordered]@{ gitCommit = $null; gitDirty = $null; inputs = @() }
        artifacts = @()
        error = $null
    }
    Save-Manifest
    Write-Host ('Package batch: ' + $batchRoot)
    $manifest.source.inputs = @(Get-PackageInputs $repositoryRoot)
    if ((Test-Path -LiteralPath (Join-Path $repositoryRoot '.git')) -and (Get-Command git -ErrorAction SilentlyContinue)) {
        $commit = & git -C $repositoryRoot rev-parse --verify HEAD 2>$null
        if ($LASTEXITCODE -eq 0) {
            $manifest.source.gitCommit = ($commit -join '').Trim()
            $changes = & git -C $repositoryRoot status --porcelain --untracked-files=normal 2>$null
            if ($LASTEXITCODE -eq 0) { $manifest.source.gitDirty = [bool]$changes }
        }
    }
    $manifest.build.status = 'running'
    Save-Manifest
    Invoke-PackageStep (Join-Path $PSScriptRoot 'build.ps1') (Join-Path $batchRoot 'build.log')
    $manifest.build.status = 'passed'
    $manifest.phase = 'tests'
    Save-Manifest
    if ($SkipTests) {
        $manifest.tests.status = 'skipped'
        [IO.File]::WriteAllText((Join-Path $batchRoot 'tests.log'), 'Tests explicitly skipped with -SkipTests.' + [Environment]::NewLine)
    } else {
        $manifest.tests.status = 'running'
        Save-Manifest
        Invoke-PackageStep (Join-Path $PSScriptRoot 'test.ps1') (Join-Path $batchRoot 'tests.log') @{ Suite = 'All' }
        $manifest.tests.status = 'passed'
    }
    $manifest.phase = 'package'
    Save-Manifest
    $currentInputs = @(Get-PackageInputs $repositoryRoot)
    if (($manifest.source.inputs | ConvertTo-Json -Compress) -ne ($currentInputs | ConvertTo-Json -Compress)) {
        throw 'Packaging inputs changed during the build or tests. Run a new batch from stable sources.'
    }

    $executable = Join-Path $repositoryRoot 'build/app/CodexUsage.exe'
    $version = [Reflection.AssemblyName]::GetAssemblyName($executable).Version.ToString()
    $fileVersion = [Diagnostics.FileVersionInfo]::GetVersionInfo($executable).FileVersion
    if ($version -ne $fileVersion) { throw 'Executable assembly and file versions do not match.' }
    $manifest.version = $version
    Copy-Item -LiteralPath $executable -Destination (Join-Path $batchRoot 'CodexUsage.exe')
    foreach ($name in @('LICENSE', 'THIRD_PARTY_NOTICES.md')) {
        Copy-Item -LiteralPath (Join-Path $repositoryRoot $name) -Destination (Join-Path $batchRoot $name)
    }
    $readme = @"
CodexUsage $version - Windows x64

Requires Windows and .NET Framework 4.8, with the Codex app or CLI installed and signed in.
Extract all ZIP files into one folder. Exit an older CodexUsage from its tray menu
before starting CodexUsage.exe. Right-click the tray icon for the application menu.

Settings and runtime logs are stored in %LOCALAPPDATA%\CodexUsage.
Keep LICENSE and THIRD_PARTY_NOTICES.md with this executable.
SHA256SUMS.txt and manifest.json beside the download ZIP describe this delivery.
"@
    [IO.File]::WriteAllText((Join-Path $batchRoot 'README.txt'), $readme, (New-Object Text.UTF8Encoding($false)))

    Add-Type -AssemblyName System.IO.Compression
    Add-Type -AssemblyName System.IO.Compression.FileSystem
    $zipPath = Join-Path $batchRoot 'CodexUsage-Windows-x64.zip'
    $zipStream = [IO.File]::Open($zipPath, [IO.FileMode]::CreateNew, [IO.FileAccess]::Write, [IO.FileShare]::None)
    try {
        $zip = New-Object IO.Compression.ZipArchive($zipStream, [IO.Compression.ZipArchiveMode]::Create, $true)
        try {
            foreach ($name in @('CodexUsage.exe', 'README.txt', 'LICENSE', 'THIRD_PARTY_NOTICES.md')) {
                [IO.Compression.ZipFileExtensions]::CreateEntryFromFile($zip, (Join-Path $batchRoot $name), $name, [IO.Compression.CompressionLevel]::Optimal) | Out-Null
            }
        } finally { $zip.Dispose() }
    } finally { $zipStream.Dispose() }

    $checksumLines = @()
    foreach ($name in @('CodexUsage.exe', 'CodexUsage-Windows-x64.zip', 'README.txt', 'LICENSE', 'THIRD_PARTY_NOTICES.md')) {
        $path = Join-Path $batchRoot $name
        $hash = (Get-FileHash -LiteralPath $path -Algorithm SHA256).Hash
        $manifest.artifacts += [ordered]@{ name = $name; bytes = (Get-Item -LiteralPath $path).Length; sha256 = $hash }
        $checksumLines += $hash + '  ' + $name
    }
    [IO.File]::WriteAllText((Join-Path $batchRoot 'SHA256SUMS.txt'), (($checksumLines -join [Environment]::NewLine) + [Environment]::NewLine), [Text.Encoding]::ASCII)
    $manifest.status = 'succeeded'
    $manifest.phase = 'complete'
    $manifest.finishedAt = (ConvertTo-PackageTime ([DateTimeOffset]::UtcNow)).ToString('o')
    Save-Manifest
    [pscustomobject]@{ BatchDirectory = $batchRoot; ManifestPath = (Join-Path $batchRoot 'manifest.json'); Status = 'succeeded' }
} catch {
    if ($null -ne $manifest) {
        $manifest.status = 'failed'
        $manifest.error = $_.Exception.Message
        $manifest.finishedAt = (ConvertTo-PackageTime ([DateTimeOffset]::UtcNow)).ToString('o')
        if ($manifest.phase -eq 'build') { $manifest.build.status = 'failed' }
        if ($manifest.phase -eq 'tests') { $manifest.tests.status = 'failed' }
        Save-Manifest
        Write-Warning ('Failed package batch retained: ' + $batchRoot)
    }
    throw
} finally {
    $packageLock.Dispose()
}
