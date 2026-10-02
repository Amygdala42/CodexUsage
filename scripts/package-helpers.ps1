# Shared by the package command and its isolated filesystem tests.
function ConvertTo-PackageTime([DateTimeOffset]$Instant) {
    try { $zone = [TimeZoneInfo]::FindSystemTimeZoneById('Asia/Shanghai') }
    catch [TimeZoneNotFoundException] { $zone = [TimeZoneInfo]::FindSystemTimeZoneById('China Standard Time') }
    return [TimeZoneInfo]::ConvertTime($Instant, $zone)
}

# The caller holds output/.package.lock for both allocation and the complete build.
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
