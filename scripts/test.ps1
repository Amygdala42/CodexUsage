#Requires -Version 5.1
[CmdletBinding()]
param(
    [ValidateSet('Domain', 'Bridge', 'ResetFeed', 'Theme', 'Layout', 'Stacking', 'UiBehavior', 'Package', 'All')][string]$Suite = 'Domain',
    [switch]$BuildOnly,
    [Parameter(DontShow = $true)][IO.FileStream]$OperationLock
)
$ErrorActionPreference = 'Stop'
$repositoryRoot = Split-Path -Parent $PSScriptRoot
. (Join-Path $PSScriptRoot 'package-helpers.ps1')
$operation = Enter-RepositoryOperation $repositoryRoot $OperationLock
$previousTemp = $env:TEMP
$previousTmp = $env:TMP
try {
    $sourceRoot = Join-Path $repositoryRoot 'src'
    $testRoot = Join-Path $repositoryRoot 'tests'
    $outputRoot = Join-Path $repositoryRoot 'build/tests'
    $compiler = Join-Path $env:WINDIR 'Microsoft.NET/Framework64/v4.0.30319/csc.exe'
    if (-not (Test-Path -LiteralPath $compiler -PathType Leaf)) {
        throw 'The Windows x64 .NET Framework compiler was not found. See docs/BUILD.md.'
    }
    New-Item -ItemType Directory -Path $outputRoot -Force | Out-Null
    function Build-Check([string]$Name, [string[]]$Inputs, [string[]]$References = @()) {
        $destination = Join-Path $outputRoot ($Name + '.exe')
        & $compiler /nologo /codepage:65001 /langversion:5 /warnaserror+ /target:exe /platform:x64 "/out:$destination" /r:System.Web.Extensions.dll @References @Inputs
        if ($LASTEXITCODE -ne 0) { throw ('Test compilation failed: ' + $Name) }
        return $destination
    }
    $env:TEMP = $outputRoot
    $env:TMP = $outputRoot
    if ($Suite -eq 'Domain' -or $Suite -eq 'All') {
        $inputs = @('QuotaModels.cs','QuotaParser.cs','AppSettings.cs','AppPaths.cs','InteractionState.cs','ResetFeed.cs') | ForEach-Object { Join-Path $sourceRoot $_ }
        $domain = Build-Check 'DomainTests' (@((Join-Path $testRoot 'DomainTests.cs')) + $inputs)
        if (-not $BuildOnly) {
            & $domain
            if ($LASTEXITCODE -ne 0) { throw ('Domain checks failed with exit code ' + $LASTEXITCODE) }
        }
    }
    if ($Suite -eq 'Bridge' -or $Suite -eq 'All') {
        $fake = Build-Check 'FakeCodexServer' @((Join-Path $testRoot 'FakeCodexServer.cs'))
        $inputs = @('IQuotaSource.cs','CodexQuotaSource.cs','ProcessJob.cs','QuotaModels.cs','QuotaParser.cs') | ForEach-Object { Join-Path $sourceRoot $_ }
        $bridge = Build-Check 'BridgeTests' (@((Join-Path $testRoot 'BridgeTests.cs')) + $inputs)
        if (-not $BuildOnly) {
            & $bridge $fake (Join-Path $outputRoot 'fake-support')
            if ($LASTEXITCODE -ne 0) { throw ('Bridge checks failed with exit code ' + $LASTEXITCODE) }
        }
    }
    if ($Suite -eq 'ResetFeed' -or $Suite -eq 'All') {
        $resetFeed = Build-Check 'ResetFeedTests' @((Join-Path $testRoot 'ResetFeedTests.cs'), (Join-Path $sourceRoot 'ResetFeed.cs'))
        if (-not $BuildOnly) {
            & $resetFeed
            if ($LASTEXITCODE -ne 0) { throw ('Reset feed checks failed with exit code ' + $LASTEXITCODE) }
        }
    }
    if ($Suite -eq 'Theme' -or $Suite -eq 'All') {
        $inputs = @('Theme.cs','WidgetRenderer.cs','QuotaModels.cs','UiText.cs') | ForEach-Object { Join-Path $sourceRoot $_ }
        $theme = Build-Check 'ThemeTests' (@((Join-Path $testRoot 'ThemeTests.cs')) + $inputs) @('/r:System.Drawing.dll','/r:System.Windows.Forms.dll')
        if (-not $BuildOnly) {
            & $theme (Join-Path $outputRoot 'theme-render')
            if ($LASTEXITCODE -ne 0) { throw ('Theme checks failed with exit code ' + $LASTEXITCODE) }
        }
    }
    if ($Suite -eq 'Layout' -or $Suite -eq 'All') {
        $inputs = @('DetailsLayout.cs','QuotaModels.cs','QuotaParser.cs','UiText.cs') | ForEach-Object { Join-Path $sourceRoot $_ }
        $layout = Build-Check 'DetailsLayoutTests' (@((Join-Path $testRoot 'DetailsLayoutTests.cs')) + $inputs) @('/r:System.Drawing.dll','/r:System.Windows.Forms.dll')
        if (-not $BuildOnly) {
            & $layout (Join-Path $outputRoot 'layout-compact')
            if ($LASTEXITCODE -ne 0) { throw ('Layout checks failed with exit code ' + $LASTEXITCODE) }
        }
    }
    if ($Suite -eq 'Stacking' -or $Suite -eq 'All') {
        $stacking = Build-Check 'TaskbarStackingTests' @((Join-Path $testRoot 'TaskbarStackingTests.cs'), (Join-Path $sourceRoot 'TaskbarStacking.cs'))
        if (-not $BuildOnly) {
            & $stacking
            if ($LASTEXITCODE -ne 0) { throw ('Taskbar stacking checks failed with exit code ' + $LASTEXITCODE) }
        }
    }
    if ($Suite -eq 'UiBehavior' -or $Suite -eq 'All') {
        $inputs = @('InteractionState.cs','TaskbarPlacement.cs','TaskbarStacking.cs','DetailsForm.cs','DetailsLayout.cs','UiDarkControls.cs','WidgetForm.cs','WidgetRenderer.cs','LayeredSurface.cs','Theme.cs','UiText.cs','QuotaModels.cs','AppSettings.cs','AppPaths.cs','ResetFeed.cs') | ForEach-Object { Join-Path $sourceRoot $_ }
        $uiBehavior = Build-Check 'UiBehaviorTests' (@((Join-Path $testRoot 'UiBehaviorTests.cs')) + $inputs) @('/r:System.Drawing.dll','/r:System.Windows.Forms.dll','/r:Accessibility.dll')
        if (-not $BuildOnly) {
            & $uiBehavior
            if ($LASTEXITCODE -ne 0) { throw ('UI behavior checks failed with exit code ' + $LASTEXITCODE) }
        }
    }
    if (($Suite -eq 'Package' -or $Suite -eq 'All') -and -not $BuildOnly) {
        & (Join-Path $testRoot 'PackageTests.ps1') -OutputRoot (Join-Path $outputRoot 'package')
    }
    if ($BuildOnly) { Write-Output 'Test compilation completed. No tests were executed.' }
} finally {
    $env:TEMP = $previousTemp
    $env:TMP = $previousTmp
    Exit-RepositoryOperation $operation
}
