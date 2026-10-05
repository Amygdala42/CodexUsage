#Requires -Version 5.1
[CmdletBinding()]
param([string]$OutputRoot = (Join-Path (Split-Path -Parent $PSScriptRoot) 'build/tests/package'))
$ErrorActionPreference = 'Stop'
$repositoryRoot = Split-Path -Parent $PSScriptRoot
$fixtureVersion = ([xml](Get-Content -LiteralPath (Join-Path $repositoryRoot 'src/app.manifest') -Raw)).assembly.assemblyIdentity.version
$runRoot = Join-Path $OutputRoot ('run-' + [Guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory -Path $runRoot -Force | Out-Null
$passed = 0
$failed = 0
function Assert-True([bool]$Value, [string]$Message) {
    if (-not $Value) { throw $Message }
}
function Check([string]$Name, [scriptblock]$Action) {
    try { & $Action; $script:passed++; Write-Output ('PASS ' + $Name) }
    catch { $script:failed++; Write-Output ('FAIL ' + $Name + ': ' + $_.Exception.Message) }
}
function Write-Utf8([string]$Path, [string]$Text) {
    [IO.File]::WriteAllText($Path, $Text, (New-Object Text.UTF8Encoding($false)))
}
function New-Fixture([string]$Name) {
    $root = Join-Path $runRoot $Name
    foreach ($directory in @('scripts', 'src', 'assets', 'tests')) {
        New-Item -ItemType Directory -Path (Join-Path $root $directory) -Force | Out-Null
    }
    foreach ($name in @('build.ps1', 'test.ps1', 'package.ps1', 'package-helpers.ps1')) {
        $source = Join-Path $repositoryRoot ('scripts/' + $name)
        if (Test-Path -LiteralPath $source) { Copy-Item -LiteralPath $source -Destination (Join-Path $root ('scripts/' + $name)) }
    }
    Copy-Item -LiteralPath (Join-Path $repositoryRoot 'assets/app.ico') -Destination (Join-Path $root 'assets/app.ico')
    Copy-Item -LiteralPath (Join-Path $repositoryRoot 'src/app.manifest') -Destination (Join-Path $root 'src/app.manifest')
    Write-Utf8 (Join-Path $root 'LICENSE') 'Fixture license'
    Write-Utf8 (Join-Path $root 'THIRD_PARTY_NOTICES.md') 'Fixture notices'
    Write-Utf8 (Join-Path $root 'src/Program.cs') @"
using System.Reflection;
[assembly: AssemblyVersion("$fixtureVersion")]
[assembly: AssemblyFileVersion("$fixtureVersion")]
namespace CodexQuotaLite { internal static class Program { private static void Main() {} } }
"@
    return $root
}
function Invoke-FixturePackage([string]$Root, [switch]$SkipTests) {
    $scriptPath = Join-Path $Root 'scripts/package.ps1'
    if (-not (Test-Path -LiteralPath $scriptPath)) { throw 'Independent package command has not been implemented.' }
    # The real package command is run from an unrelated directory, against an isolated
    # build/test fixture. No application GUI or real account operation is executed.
    Push-Location $runRoot
    try { & $scriptPath -SkipTests:$SkipTests | Out-Null }
    finally { Pop-Location }
}
function Get-Batches([string]$Root) {
    $output = Join-Path $Root 'output'
    if (-not (Test-Path -LiteralPath $output)) { return @() }
    return @(Get-ChildItem -LiteralPath $output -Directory | ForEach-Object {
        Get-ChildItem -LiteralPath $_.FullName -Directory -Filter 'batch-*'
    } | Sort-Object FullName)
}
function Read-Manifest([IO.DirectoryInfo]$Batch) {
    return Get-Content -LiteralPath (Join-Path $Batch.FullName 'manifest.json') -Raw | ConvertFrom-Json
}

$fixture = New-Fixture 'repository with spaces'
Check 'build command writes its executable and checksum under build/app from any cwd' {
    Push-Location $runRoot
    try { & (Join-Path $fixture 'scripts/build.ps1') | Out-Null }
    finally { Pop-Location }
    $exe = Join-Path $fixture 'build/app/CodexUsage.exe'
    Assert-True (Test-Path -LiteralPath $exe) 'Expected build/app/CodexUsage.exe.'
    Assert-True ((Get-Content -LiteralPath (Join-Path $fixture 'build/app/SHA256SUMS.txt') -Raw).Contains((Get-FileHash -LiteralPath $exe).Hash)) 'Build checksum must describe the generated executable.'
    Assert-True (-not (Test-Path -LiteralPath (Join-Path $fixture 'env'))) 'Build must not recreate env.'
}
Check 'test compilation writes only to build/tests and restores temporary directories' {
    foreach ($name in @('QuotaModels','QuotaParser','AppSettings','AppPaths','InteractionState','ResetFeed')) {
        Write-Utf8 (Join-Path $fixture ('src/' + $name + '.cs')) '// Empty domain fixture dependency.'
    }
    Write-Utf8 (Join-Path $fixture 'tests/DomainTests.cs') 'internal static class DomainTests { public static int Main() { return 0; } }'
    $tempBefore = $env:TEMP
    $tmpBefore = $env:TMP
    Push-Location $runRoot
    try { & (Join-Path $fixture 'scripts/test.ps1') -Suite Domain -BuildOnly | Out-Null }
    finally { Pop-Location }
    Assert-True (Test-Path -LiteralPath (Join-Path $fixture 'build/tests/DomainTests.exe')) 'Expected build/tests/DomainTests.exe.'
    Assert-True ($env:TEMP -eq $tempBefore -and $env:TMP -eq $tmpBefore) 'Test command must restore TEMP/TMP.'
}
foreach ($entry in @('build', 'test')) {
    Check ($entry + ' refuses an active shared output lock before modifying outputs and releases on retry') {
        $output = Join-Path $fixture 'output'
        New-Item -ItemType Directory -Path $output -Force | Out-Null
        $lock = [IO.File]::Open((Join-Path $output '.package.lock'), [IO.FileMode]::OpenOrCreate, [IO.FileAccess]::ReadWrite, [IO.FileShare]::None)
        $arguments = @{}
        if ($entry -eq 'test') { $arguments = @{ Suite = 'Domain'; BuildOnly = $true } }
        $exe = Join-Path $fixture $(if ($entry -eq 'build') { 'build/app/CodexUsage.exe' } else { 'build/tests/DomainTests.exe' })
        $before = (Get-FileHash -LiteralPath $exe).Hash
        try {
            $rejected = $false
            try { & (Join-Path $fixture ('scripts/' + $entry + '.ps1')) @arguments | Out-Null }
            catch { $rejected = $_.Exception.Message -match 'lock' }
            Assert-True $rejected 'The real entry must reject the shared lock, rather than write concurrently.'
            Assert-True ((Get-FileHash -LiteralPath $exe).Hash -eq $before) 'Rejected entry must leave compiled output intact.'
        } finally { $lock.Dispose() }
        & (Join-Path $fixture ('scripts/' + $entry + '.ps1')) @arguments | Out-Null
    }
}
Check 'a running real test entry prevents competing build and package and releases its lock' {
    $root = New-Fixture 'running test with spaces'
    foreach ($name in @('QuotaModels','QuotaParser','AppSettings','AppPaths','InteractionState','ResetFeed')) {
        Write-Utf8 (Join-Path $root ('src/' + $name + '.cs')) '// Empty domain fixture dependency.'
    }
    Write-Utf8 (Join-Path $root 'tests/DomainTests.cs') @'
using System;
using System.IO;
using System.Threading;
internal static class DomainTests {
    public static int Main() {
        string root = AppDomain.CurrentDomain.BaseDirectory;
        File.WriteAllText(Path.Combine(root, "operation-ready"), "ready");
        for (int i = 0; i < 300; i++) {
            if (File.Exists(Path.Combine(root, "release-operation"))) return 0;
            Thread.Sleep(100);
        }
        return 2;
    }
}
'@
    $hostPath = (Get-Process -Id $PID).Path
    $process = Start-Process -FilePath $hostPath -ArgumentList @('-NoProfile', '-File', ('"' + (Join-Path $root 'scripts/test.ps1') + '"'), '-Suite', 'Domain') -WindowStyle Hidden -PassThru -RedirectStandardOutput (Join-Path $root 'child.log') -RedirectStandardError (Join-Path $root 'child-error.log')
    try {
        $ready = Join-Path $root 'build/tests/operation-ready'
        $deadline = [DateTime]::UtcNow.AddSeconds(15)
        while (-not (Test-Path -LiteralPath $ready) -and -not $process.HasExited -and [DateTime]::UtcNow -lt $deadline) { Start-Sleep -Milliseconds 100; $process.Refresh() }
        Assert-True (Test-Path -LiteralPath $ready) 'The actual test process must reach its test body.'
        $buildRejected = $false
        $packageRejected = $false
        try { & (Join-Path $root 'scripts/build.ps1') | Out-Null } catch { $buildRejected = $_.Exception.Message -match 'lock' }
        try { Invoke-FixturePackage $root -SkipTests } catch { $packageRejected = $_.Exception.Message -match 'lock' }
        Assert-True ($buildRejected -and $packageRejected) 'Running test entry must exclude both other shared writers.'
        Assert-True (@(Get-Batches $root).Count -eq 0) 'Competing package must not allocate a batch.'
    } finally {
        Write-Utf8 (Join-Path $root 'build/tests/release-operation') 'release'
        if (-not $process.WaitForExit(15000)) { $process.Kill(); throw 'Fixture test process did not exit.' }
    }
    Assert-True ($process.ExitCode -eq 0) 'Fixture tests must finish normally.'
    Invoke-FixturePackage $root -SkipTests
    Assert-True ((Read-Manifest @(Get-Batches $root)[0]).status -eq 'succeeded') 'Lock must release when tests finish.'
}
Check 'Shanghai date advances at 16:00 UTC rather than host midnight' {
    $helper = Join-Path $repositoryRoot 'scripts/package-helpers.ps1'
    if (-not (Test-Path -LiteralPath $helper)) { throw 'Shanghai batch date conversion has not been implemented.' }
    . $helper
    $before = ConvertTo-PackageTime ([DateTimeOffset]::Parse('2026-10-01T15:59:59Z'))
    $after = ConvertTo-PackageTime ([DateTimeOffset]::Parse('2026-10-01T16:00:00Z'))
    Assert-True ($before.ToString('yyyy-MM-dd') -eq '2026-10-01') 'Expected October 1 before Shanghai midnight.'
    Assert-True ($after.ToString('yyyy-MM-dd') -eq '2026-10-02' -and $after.Offset.TotalHours -eq 8) 'Expected October 2 at UTC+08:00.'
}
Check 'batch dates and numbers remain Gregorian and ASCII under Thai culture' {
    . (Join-Path $repositoryRoot 'scripts/package-helpers.ps1')
    $originalCulture = [Threading.Thread]::CurrentThread.CurrentCulture
    try {
        [Threading.Thread]::CurrentThread.CurrentCulture = New-Object Globalization.CultureInfo('th-TH')
        $instant = [DateTimeOffset]::Parse('2026-10-01T16:00:00Z', [Globalization.CultureInfo]::InvariantCulture)
        $startedAt = ConvertTo-PackageTime $instant
        $output = Join-Path $runRoot 'thai-culture'
        $first = New-PackageBatch $output $startedAt
        $second = New-PackageBatch $output $startedAt
        Assert-True ((Split-Path -Leaf (Split-Path -Parent $first)) -eq '2026-10-02') 'Batch directory must use Gregorian 2026, not Buddhist 2569.'
        Assert-True ((Split-Path -Leaf $first) -eq 'batch-001' -and (Split-Path -Leaf $second) -eq 'batch-002') 'Batch numbers must use predictable ASCII digits.'
        Assert-True ($startedAt.ToString('o') -eq '2026-10-02T00:00:00.0000000+08:00') 'Manifest round-trip ISO timestamp must remain culture-independent.'
    } finally { [Threading.Thread]::CurrentThread.CurrentCulture = $originalCulture }
}

# Full C# regression tests run separately via test.ps1 -Suite All. This strict fixture
# tests package orchestration without recursively running the package suite itself.
Write-Utf8 (Join-Path $fixture 'scripts/test.ps1') @'
param([string]$Suite, [switch]$BuildOnly, [IO.FileStream]$OperationLock)
$ErrorActionPreference = 'Stop'
if ($Suite -ne 'All' -or $BuildOnly) { throw 'Package must execute all checks by default.' }
$root = Split-Path -Parent $PSScriptRoot
if (-not (Test-Path -LiteralPath (Join-Path $root 'build/app/CodexUsage.exe'))) { throw 'Tests ran before build.' }
Add-Content -LiteralPath (Join-Path $root 'test-calls.txt') -Value 'All executed'
if (Test-Path -LiteralPath (Join-Path $root 'fail-tests')) { throw 'Deliberate test failure.' }
Write-Output 'Fixture checks passed.'
'@
Check 'default package builds and runs All before producing a complete batch' {
    Invoke-FixturePackage $fixture
    $batches = @(Get-Batches $fixture)
    Assert-True ($batches.Count -eq 1 -and $batches[0].Name -eq 'batch-001') 'First package must allocate batch-001.'
    $manifest = Read-Manifest $batches[0]
    Assert-True ($manifest.status -eq 'succeeded' -and $manifest.version -eq $fixtureVersion) 'Expected successful versioned manifest.'
    $rawManifest = Get-Content -LiteralPath (Join-Path $batches[0].FullName 'manifest.json') -Raw
    # PowerShell 7.5+ parses JSON timestamps into DateTime; inspect the serialized
    # offset so this assertion also works on Windows PowerShell 5.1.
    Assert-True ($manifest.timeZone -eq 'Asia/Shanghai' -and $rawManifest -match '"startedAt"\s*:\s*"[^"\r\n]+\+08:00"') 'Manifest must record Shanghai time.'
    Assert-True ($manifest.build.status -eq 'passed' -and $manifest.tests.status -eq 'passed' -and -not $manifest.skipTests) 'Default package must record passing build and tests.'
    Assert-True ((Get-Content -LiteralPath (Join-Path $fixture 'test-calls.txt')).Count -eq 1) 'All checks must execute exactly once.'
    Assert-True (Test-Path -LiteralPath (Join-Path $batches[0].FullName 'build.log')) 'Build log must be retained.'
    Assert-True (Test-Path -LiteralPath (Join-Path $batches[0].FullName 'tests.log')) 'Test log must be retained.'
}
Check 'manifest and SHA256SUMS agree with standalone executable and ZIP bytes' {
    $batch = @(Get-Batches $fixture)[0]
    Assert-True ($null -ne $batch) 'A successful batch is required.'
    $manifest = Read-Manifest $batch
    $checksums = Get-Content -LiteralPath (Join-Path $batch.FullName 'SHA256SUMS.txt')
    foreach ($name in @('CodexUsage.exe','CodexUsage-Windows-x64.zip')) {
        $path = Join-Path $batch.FullName $name
        $hash = (Get-FileHash -LiteralPath $path -Algorithm SHA256).Hash
        $entry = @($manifest.artifacts | Where-Object { $_.name -eq $name })
        Assert-True ($entry.Count -eq 1 -and $entry[0].sha256 -eq $hash -and $entry[0].bytes -eq (Get-Item -LiteralPath $path).Length) ('Manifest mismatch: ' + $name)
        Assert-True ($checksums -contains ($hash + '  ' + $name)) ('Checksum mismatch: ' + $name)
    }
}
Check 'source provenance uses relative paths and hashes without requiring a Git checkout' {
    $batch = @(Get-Batches $fixture)[0]
    Assert-True ($null -ne $batch) 'A successful batch is required.'
    $manifest = Read-Manifest $batch
    Assert-True ($null -eq $manifest.source.gitCommit) 'Source exports must not inherit an enclosing unrelated Git checkout.'
    foreach ($name in @('src/Program.cs','src/app.manifest','assets/app.ico','LICENSE','THIRD_PARTY_NOTICES.md','scripts/build.ps1','scripts/test.ps1','scripts/package.ps1')) {
        $entry = @($manifest.source.inputs | Where-Object { $_.path -eq $name })
        Assert-True ($entry.Count -eq 1 -and $entry[0].sha256 -eq (Get-FileHash -LiteralPath (Join-Path $fixture $name)).Hash) ('Source provenance mismatch: ' + $name)
    }
    foreach ($entry in $manifest.source.inputs) {
        Assert-True (-not [IO.Path]::IsPathRooted($entry.path)) 'Manifest source paths must be relative.'
    }
}
Check 'ZIP contains matching executable, standalone usage instructions and licenses' {
    Add-Type -AssemblyName System.IO.Compression.FileSystem
    $batch = @(Get-Batches $fixture)[0]
    Assert-True ($null -ne $batch) 'A successful batch is required.'
    $archive = [IO.Compression.ZipFile]::OpenRead((Join-Path $batch.FullName 'CodexUsage-Windows-x64.zip'))
    try {
        $names = @($archive.Entries | ForEach-Object FullName | Sort-Object)
        Assert-True (($names -join '|') -eq 'CodexUsage.exe|LICENSE|README.txt|THIRD_PARTY_NOTICES.md') 'ZIP must be self-contained with exactly four expected files.'
        foreach ($entry in $archive.Entries) {
            $stream = $entry.Open()
            $sha = [Security.Cryptography.SHA256]::Create()
            try { $hash = [BitConverter]::ToString($sha.ComputeHash($stream)).Replace('-', '') }
            finally { $sha.Dispose(); $stream.Dispose() }
            Assert-True ($hash -eq (Get-FileHash -LiteralPath (Join-Path $batch.FullName $entry.FullName)).Hash) ('ZIP bytes differ: ' + $entry.FullName)
        }
    } finally { $archive.Dispose() }
}
Check 'repeat packaging advances the batch number without modifying earlier delivery' {
    $first = @(Get-Batches $fixture)[0]
    Assert-True ($null -ne $first) 'A successful batch is required.'
    $before = (Get-FileHash -LiteralPath (Join-Path $first.FullName 'manifest.json')).Hash
    Invoke-FixturePackage $fixture
    $batches = @(Get-Batches $fixture)
    Assert-True ($batches.Count -eq 2 -and $batches[1].Name -eq 'batch-002') 'Second package must allocate batch-002.'
    Assert-True ((Get-FileHash -LiteralPath (Join-Path $first.FullName 'manifest.json')).Hash -eq $before) 'Prior batch manifest must remain unchanged.'
}
Check 'SkipTests is explicit in the manifest and never runs the test command' {
    $calls = @(Get-Content -LiteralPath (Join-Path $fixture 'test-calls.txt')).Count
    Invoke-FixturePackage $fixture -SkipTests
    $last = @(Get-Batches $fixture)[-1]
    $manifest = Read-Manifest $last
    Assert-True ($manifest.status -eq 'succeeded' -and $manifest.skipTests -and $manifest.tests.status -eq 'skipped') 'Skipped tests must be clearly recorded.'
    Assert-True (@(Get-Content -LiteralPath (Join-Path $fixture 'test-calls.txt')).Count -eq $calls) 'SkipTests must not run checks.'
}
Check 'failed checks preserve their failed batch and do not produce a delivery ZIP' {
    Write-Utf8 (Join-Path $fixture 'fail-tests') 'fail'
    $failedAsExpected = $false
    try { Invoke-FixturePackage $fixture } catch { $failedAsExpected = $true }
    Assert-True $failedAsExpected 'Test failure must fail the package command.'
    $batch = @(Get-Batches $fixture)[-1]
    Assert-True ($null -ne $batch) 'Failure must retain an allocated batch.'
    $manifest = Read-Manifest $batch
    Assert-True ($manifest.status -eq 'failed' -and $manifest.tests.status -eq 'failed' -and $manifest.phase -eq 'tests') 'Failure phase and test result must be retained.'
    Assert-True (-not (Test-Path -LiteralPath (Join-Path $batch.FullName 'CodexUsage-Windows-x64.zip'))) 'Failed checks must not produce a ZIP.'
    Remove-Item -LiteralPath (Join-Path $fixture 'fail-tests')
    $priorName = $batch.Name
    Invoke-FixturePackage $fixture
    $next = @(Get-Batches $fixture)[-1]
    Assert-True ($next.Name -ne $priorName -and (Read-Manifest $batch).status -eq 'failed') 'Retry must allocate a new batch without erasing the failure.'
}
Check 'failed build stops before tests and preserves a reviewable failure' {
    $root = New-Fixture 'build failure'
    Write-Utf8 (Join-Path $root 'scripts/build.ps1') "throw 'Deliberate compiler failure.'"
    Write-Utf8 (Join-Path $root 'scripts/test.ps1') "throw 'Tests must not be reached.'"
    $failedAsExpected = $false
    try { Invoke-FixturePackage $root } catch { $failedAsExpected = $true }
    Assert-True $failedAsExpected 'Build failure must fail the package command.'
    $batch = @(Get-Batches $root)[0]
    Assert-True ($null -ne $batch) 'Build failure must retain a batch.'
    $manifest = Read-Manifest $batch
    Assert-True ($manifest.status -eq 'failed' -and $manifest.phase -eq 'build' -and $manifest.build.status -eq 'failed' -and $manifest.tests.status -eq 'pending') 'Build failure must be reported before tests run.'
    Assert-True ((Get-Content -LiteralPath (Join-Path $batch.FullName 'build.log') -Raw) -match 'Deliberate compiler failure') 'Build failure details must be retained in the build log.'
}
Check 'an active package lock prevents competing output and permits a later retry' {
    $root = New-Fixture 'locked repository'
    $output = Join-Path $root 'output'
    New-Item -ItemType Directory -Path $output -Force | Out-Null
    $lock = [IO.File]::Open((Join-Path $output '.package.lock'), [IO.FileMode]::OpenOrCreate, [IO.FileAccess]::ReadWrite, [IO.FileShare]::None)
    try {
        $failedAsExpected = $false
        try { Invoke-FixturePackage $root -SkipTests } catch { $failedAsExpected = $true }
        Assert-True $failedAsExpected 'Concurrent package command must fail rather than share build outputs.'
        Assert-True (@(Get-Batches $root).Count -eq 0) 'Rejected concurrent command must not allocate a batch.'
    } finally { $lock.Dispose() }
    Invoke-FixturePackage $root -SkipTests
    Assert-True ((Read-Manifest @(Get-Batches $root)[0]).status -eq 'succeeded') 'Lock must be reusable after release.'
}

Check 'a changed executable at the copy boundary cannot become a successful delivery' {
    $root = New-Fixture 'copy integrity'
    $replacementSource = Join-Path $root 'replacement.cs'
    $replacement = Join-Path $root 'replacement.exe'
    Write-Utf8 $replacementSource @"
using System.Reflection;
[assembly: AssemblyVersion("$fixtureVersion")]
[assembly: AssemblyFileVersion("$fixtureVersion")]
internal static class Replacement { public static int Main() { return 42; } }
"@
    $compiler = Join-Path $env:WINDIR 'Microsoft.NET/Framework64/v4.0.30319/csc.exe'
    & $compiler /nologo /target:exe "/out:$replacement" $replacementSource
    Assert-True ($LASTEXITCODE -eq 0) 'Replacement fixture must compile.'
    function Copy-Item {
        param([string]$LiteralPath, [string]$Destination)
        Microsoft.PowerShell.Management\Copy-Item -LiteralPath $LiteralPath -Destination $Destination
        if ($Destination -match 'batch-[0-9]+[\\/]CodexUsage\.exe$') {
            Microsoft.PowerShell.Management\Copy-Item -LiteralPath $replacement -Destination $Destination
        }
    }
    $rejected = $false
    try { Invoke-FixturePackage $root -SkipTests } catch { $rejected = $_.Exception.Message -match 'Copied executable does not match' }
    Assert-True $rejected 'Final copied bytes must agree with the executable built for the batch.'
    $manifest = Read-Manifest @(Get-Batches $root)[0]
    Assert-True ($manifest.status -eq 'failed' -and $manifest.phase -eq 'package') 'Copy mismatch must retain an honest failed manifest.'
}
Check 'inputs changed after the pre-copy check cannot become a successful delivery' {
    $root = New-Fixture 'late source change'
    function Copy-Item {
        param([string]$LiteralPath, [string]$Destination)
        Microsoft.PowerShell.Management\Copy-Item -LiteralPath $LiteralPath -Destination $Destination
        if ($Destination -match 'batch-[0-9]+[\\/]CodexUsage\.exe$') {
            Add-Content -LiteralPath (Join-Path $root 'src/Program.cs') -Value '// Changed after the initial provenance check.'
        }
    }
    $rejected = $false
    try { Invoke-FixturePackage $root -SkipTests } catch { $rejected = $_.Exception.Message -match 'inputs changed' }
    Assert-True $rejected 'Input stability must be checked after final artifacts have been copied.'
    Assert-True ((Read-Manifest @(Get-Batches $root)[0]).status -eq 'failed') 'Late input change must leave a failed batch.'
}
Check 'default package invokes real build and real All test entries without reacquiring or releasing its lock' {
    $root = New-Fixture 'nested actual entries'
    foreach ($name in @('QuotaModels','QuotaParser','AppSettings','AppPaths','InteractionState','ResetFeed','IQuotaSource','CodexQuotaSource','ProcessJob','Theme','WidgetRenderer','UiText','DetailsLayout','TaskbarStacking','TaskbarPlacement','DetailsForm','UiDarkControls','WidgetForm','LayeredSurface')) {
        Write-Utf8 (Join-Path $root ('src/' + $name + '.cs')) '// Empty fixture dependency.'
    }
    foreach ($name in @('DomainTests','FakeCodexServer','BridgeTests','ResetFeedTests','ThemeTests','DetailsLayoutTests','TaskbarStackingTests','UiBehaviorTests')) {
        Write-Utf8 (Join-Path $root ('tests/' + $name + '.cs')) ('internal static class ' + $name + ' { public static int Main() { return 0; } }')
    }
    # Only the recursive Package suite is replaced. Both production entry scripts,
    # every compiler call and all other fixture executables run unchanged.
    Write-Utf8 (Join-Path $root 'tests/PackageTests.ps1') "param([string]`$OutputRoot)`r`nWrite-Output 'Recursive fixture package checks omitted.'"
    Invoke-FixturePackage $root
    $manifest = Read-Manifest @(Get-Batches $root)[0]
    Assert-True ($manifest.status -eq 'succeeded' -and $manifest.build.status -eq 'passed' -and $manifest.tests.status -eq 'passed') 'Real nested entries must finish without a recursive lock failure.'
    Assert-True (@(Get-ChildItem -LiteralPath (Join-Path $root 'build/tests') -Filter '*.exe').Count -eq 8) 'All must compile and execute every C# fixture suite including UiBehavior.'
    $lock = [IO.File]::Open((Join-Path $root 'output/.package.lock'), [IO.FileMode]::Open, [IO.FileAccess]::ReadWrite, [IO.FileShare]::None)
    $lock.Dispose()
}
Check 'real build failure releases the lock and restores temporary directories before retry' {
    $root = New-Fixture 'actual compiler failure'
    $source = Join-Path $root 'src/Program.cs'
    $validProgram = Get-Content -LiteralPath $source -Raw
    Add-Content -LiteralPath $source -Value 'invalid C# syntax!'
    $tempBefore = $env:TEMP
    $tmpBefore = $env:TMP
    $rejected = $false
    try { & (Join-Path $root 'scripts/build.ps1') | Out-Null } catch { $rejected = $_.Exception.Message -match 'Build failed with compiler exit code' }
    Assert-True $rejected 'The real compiler failure must reach the build failure path.'
    Assert-True ($env:TEMP -eq $tempBefore -and $env:TMP -eq $tmpBefore) 'Failed build must restore TEMP/TMP.'
    Write-Utf8 $source $validProgram
    & (Join-Path $root 'scripts/build.ps1') | Out-Null
    Assert-True (Test-Path -LiteralPath (Join-Path $root 'build/app/CodexUsage.exe')) 'The corrected build must acquire the released lock.'
}
Check 'real failing tests release the lock before a successful retry' {
    $root = New-Fixture 'actual test failure'
    foreach ($name in @('QuotaModels','QuotaParser','AppSettings','AppPaths','InteractionState','ResetFeed')) {
        Write-Utf8 (Join-Path $root ('src/' + $name + '.cs')) '// Empty fixture dependency.'
    }
    $testSource = Join-Path $root 'tests/DomainTests.cs'
    Write-Utf8 $testSource 'internal static class DomainTests { public static int Main() { return 1; } }'
    $rejected = $false
    try { & (Join-Path $root 'scripts/test.ps1') -Suite Domain | Out-Null } catch { $rejected = $_.Exception.Message -match 'Domain checks failed with exit code 1' }
    Assert-True $rejected 'The actual failing test executable must fail the entry.'
    Write-Utf8 $testSource 'internal static class DomainTests { public static int Main() { return 0; } }'
    & (Join-Path $root 'scripts/test.ps1') -Suite Domain | Out-Null
}
Check 'an internal lock handle cannot bypass coordination when shared or from a different repository' {
    $root = New-Fixture 'invalid lock handles'
    $output = Join-Path $root 'output'
    New-Item -ItemType Directory -Path $output -Force | Out-Null
    $shared = [IO.File]::Open((Join-Path $output '.package.lock'), [IO.FileMode]::OpenOrCreate, [IO.FileAccess]::ReadWrite, [IO.FileShare]::ReadWrite)
    try {
        $rejected = $false
        try { & (Join-Path $root 'scripts/build.ps1') -OperationLock $shared | Out-Null } catch { $rejected = $_.Exception.Message -match 'lock handle must exclude' }
        Assert-True $rejected 'A shared handle must not authorize nested entry.'
    } finally { $shared.Dispose() }
    $foreign = [IO.File]::Open((Join-Path $root 'foreign.lock'), [IO.FileMode]::CreateNew, [IO.FileAccess]::ReadWrite, [IO.FileShare]::None)
    try {
        $rejected = $false
        try { & (Join-Path $root 'scripts/build.ps1') -OperationLock $foreign | Out-Null } catch { $rejected = $_.Exception.Message -match 'lock handle is invalid' }
        Assert-True $rejected 'A foreign repository handle must not authorize nested entry.'
    } finally { $foreign.Dispose() }
    & (Join-Path $root 'scripts/build.ps1') | Out-Null
}

Write-Output ('Package checks: ' + $passed + ' passed, ' + $failed + ' failed.')
Write-Output ('Package test evidence: ' + $runRoot)
if ($failed -gt 0) { throw ('Package checks failed: ' + $failed) }
