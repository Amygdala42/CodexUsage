# Shared by the package command and its isolated filesystem tests.
function Enter-RepositoryOperation([string]$RepositoryRoot, [IO.FileStream]$OperationLock = $null) {
    $outputRoot = Join-Path $RepositoryRoot 'output'
    $lockPath = [IO.Path]::GetFullPath((Join-Path $outputRoot '.package.lock'))
    if ($null -ne $OperationLock) {
        # Nested build/test calls receive the actual open handle, never an environment
        # switch. Reject disposed, foreign and non-exclusive handles before writing.
        if (-not $OperationLock.CanRead -or -not $OperationLock.CanWrite -or
            -not [string]::Equals([IO.Path]::GetFullPath($OperationLock.Name), $lockPath, [StringComparison]::OrdinalIgnoreCase)) {
            throw 'The shared output lock handle is invalid for this repository.'
        }
        $probe = $null
        try { $probe = [IO.File]::Open($lockPath, [IO.FileMode]::Open, [IO.FileAccess]::ReadWrite, [IO.FileShare]::ReadWrite) }
        catch [IO.IOException] { }
        if ($null -ne $probe) {
            $probe.Dispose()
            throw 'The shared output lock handle must exclude competing writers.'
        }
        return [pscustomobject]@{ Handle = $OperationLock; OwnsHandle = $false }
    }
    New-Item -ItemType Directory -Path $outputRoot -Force | Out-Null
    try {
        # Keep this file after release: deleting it can race with the next caller.
        $handle = [IO.File]::Open($lockPath, [IO.FileMode]::OpenOrCreate, [IO.FileAccess]::ReadWrite, [IO.FileShare]::None)
    } catch {
        throw ('Cannot acquire the shared output lock. Another build, test or package may be running, or output is not writable. ' + $_.Exception.Message)
    }
    return [pscustomobject]@{ Handle = $handle; OwnsHandle = $true }
}

function Exit-RepositoryOperation($Operation) {
    if ($null -ne $Operation -and $Operation.OwnsHandle) { $Operation.Handle.Dispose() }
}

function ConvertTo-PackageTime([DateTimeOffset]$Instant) {
    try { $zone = [TimeZoneInfo]::FindSystemTimeZoneById('Asia/Shanghai') }
    catch [TimeZoneNotFoundException] { $zone = [TimeZoneInfo]::FindSystemTimeZoneById('China Standard Time') }
    return [TimeZoneInfo]::ConvertTime($Instant, $zone)
}

# The caller holds output/.package.lock through build, tests and final verification.
function New-PackageBatch([string]$OutputRoot, [DateTimeOffset]$StartedAt) {
    $dateRoot = Join-Path $OutputRoot $StartedAt.ToString('yyyy-MM-dd', [Globalization.CultureInfo]::InvariantCulture)
    New-Item -ItemType Directory -Path $dateRoot -Force | Out-Null
    $lastNumber = 0
    foreach ($entry in Get-ChildItem -LiteralPath $dateRoot -Force) {
        if ($entry.Name -match '^batch-([0-9]+)$') {
            $lastNumber = [Math]::Max($lastNumber, [int]::Parse($Matches[1]))
        }
    }
    $batchRoot = Join-Path $dateRoot ('batch-' + ($lastNumber + 1).ToString('D3', [Globalization.CultureInfo]::InvariantCulture))
    # No -Force: an existing batch, including a failed or interrupted one, is never reused.
    return (New-Item -ItemType Directory -Path $batchRoot -ErrorAction Stop).FullName
}

function Get-PackageInputs([string]$RepositoryRoot) {
    $paths = @('assets/app.ico', 'LICENSE', 'THIRD_PARTY_NOTICES.md')
    foreach ($directory in @('src', 'scripts', 'tests')) {
        $paths += @(Get-ChildItem -LiteralPath (Join-Path $RepositoryRoot $directory) -File | ForEach-Object { $directory + '/' + $_.Name })
    }
    foreach ($relativePath in ($paths | Sort-Object)) {
        $path = Join-Path $RepositoryRoot $relativePath
        [pscustomobject]@{ path = $relativePath; sha256 = (Get-FileHash -LiteralPath $path -Algorithm SHA256).Hash }
    }
}
