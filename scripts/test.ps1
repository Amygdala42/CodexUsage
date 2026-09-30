#Requires -Version 5.1
[CmdletBinding()]
param(
    [ValidateSet('Domain', 'Bridge', 'ResetFeed', 'Theme', 'Layout', 'All')][string]$Suite = 'Domain',
    [switch]$BuildOnly
)
$ErrorActionPreference = 'Stop'
$repositoryRoot = Split-Path -Parent $PSScriptRoot
$sourceRoot = Join-Path $repositoryRoot 'src'
$testRoot = Join-Path $repositoryRoot 'tests'
$outputRoot = Join-Path $repositoryRoot 'env/tests'
$compiler = Join-Path $env:WINDIR 'Microsoft.NET/Framework64/v4.0.30319/csc.exe'
if (-not (Test-Path -LiteralPath $compiler -PathType Leaf)) {
    throw 'The Windows x64 .NET Framework compiler was not found. See docs/BUILD.md.'
}
New-Item -ItemType Directory -Path $outputRoot -Force | Out-Null
$previousTemp = $env:TEMP
$previousTmp = $env:TMP
function Build-Check([string]$Name, [string[]]$Inputs, [string[]]$References = @()) {
    $destination = Join-Path $outputRoot ($Name + '.exe')
    & $compiler /nologo /codepage:65001 /langversion:5 /warnaserror+ /target:exe /platform:x64 "/out:$destination" /r:System.Web.Extensions.dll @References @Inputs
    if ($LASTEXITCODE -ne 0) { throw ('Test compilation failed: ' + $Name) }
    return $destination
}
try {
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
    if ($BuildOnly) { Write-Output 'Test compilation completed. No tests were executed.' }
} finally {
    $env:TEMP = $previousTemp
    $env:TMP = $previousTmp
}
