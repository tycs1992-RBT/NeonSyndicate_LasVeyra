<#
.SYNOPSIS
  Runs the Neon Syndicate automated tests (Unity Test Framework, EditMode) from the command line.

.DESCRIPTION
  Finds Unity like tools\build.ps1 does, runs the tests in batch mode, reads the NUnit results file and prints a
  short pass/fail summary with the name and message of every failing test. Exits with 0 when everything passed and
  1 when a test failed (3 when Unity produced no results, usually a compile error: see the log). Results and the log
  are saved in Logs\. Close the Unity editor for this project first.

.PARAMETER UnityPath
  Full path to Unity.exe when it is not in Unity Hub's usual folder. The UNITY_EXE environment variable works too.

.PARAMETER Platform
  EditMode (the default; fast logic tests) or PlayMode.

.PARAMETER Filter
  Optional: run only tests whose full name matches, for example "SaveDataTests" or "NeonSyndicate.Tests.CityLayoutTests".

.EXAMPLE
  powershell -ExecutionPolicy Bypass -File tools\test.ps1

.EXAMPLE
  powershell -ExecutionPolicy Bypass -File tools\test.ps1 -Filter PostGradeTests
#>
[CmdletBinding()]
param(
    [string]$UnityPath = '',
    [ValidateSet('EditMode', 'PlayMode')]
    [string]$Platform = 'EditMode',
    [string]$Filter = ''
)

$ErrorActionPreference = 'Stop'
$Project = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path

function Get-ProjectUnityVersion {
    $file = Join-Path $Project 'ProjectSettings\ProjectVersion.txt'
    if (Test-Path $file) {
        foreach ($line in Get-Content $file) {
            if ($line -match '^m_EditorVersion:\s*(\S+)') { return $Matches[1] }
        }
    }
    return '6000.3.16f1'
}

function Find-Unity([string]$Explicit) {
    if ($Explicit) {
        if (Test-Path $Explicit) { return (Resolve-Path $Explicit).Path }
        throw "Unity was not found at -UnityPath '$Explicit'."
    }
    if ($env:UNITY_EXE) {
        if (Test-Path $env:UNITY_EXE) { return $env:UNITY_EXE }
        throw "UNITY_EXE points to '$($env:UNITY_EXE)', which does not exist."
    }
    $version = Get-ProjectUnityVersion
    $roots = @()
    if ($env:ProgramFiles) { $roots += (Join-Path $env:ProgramFiles 'Unity\Hub\Editor') }
    $hubConfig = ''
    if ($env:APPDATA) { $hubConfig = Join-Path $env:APPDATA 'UnityHub\secondaryInstallPath.json' }
    if ($hubConfig -and (Test-Path $hubConfig)) {
        try {
            $custom = Get-Content $hubConfig -Raw | ConvertFrom-Json
            if ($custom) { $roots += [string]$custom }
        } catch { }
    }
    foreach ($root in $roots) {
        $exe = Join-Path $root "$version\Editor\Unity.exe"
        if (Test-Path $exe) { return $exe }
    }
    $line = ($version -split '\.')[0..1] -join '.'
    foreach ($root in $roots) {
        if (-not (Test-Path $root)) { continue }
        $other = Get-ChildItem $root -Directory | Where-Object { $_.Name -like "$line.*" } | Sort-Object Name -Descending | Select-Object -First 1
        if ($other) {
            $exe = Join-Path $other.FullName 'Editor\Unity.exe'
            if (Test-Path $exe) {
                Write-Warning "Unity $version is not installed; using $($other.Name) instead."
                return $exe
            }
        }
    }
    throw "Unity $version was not found in Unity Hub's folders ($($roots -join '; ')). Install it with Unity Hub, or pass -UnityPath 'C:\path\to\Unity.exe'."
}

function Test-ProjectOpen {
    $lock = Join-Path $Project 'Temp\UnityLockfile'
    if (-not (Test-Path $lock)) { return $false }
    try {
        $fs = [System.IO.File]::Open($lock, [System.IO.FileMode]::Open, [System.IO.FileAccess]::ReadWrite, [System.IO.FileShare]::None)
        $fs.Close()
        return $false
    } catch {
        return $true
    }
}

function Quote([string]$s) { return '"' + $s + '"' }

try {
    $unity = Find-Unity $UnityPath
} catch {
    Write-Host "TESTS NOT RUN: $($_.Exception.Message)" -ForegroundColor Red
    exit 3
}
if (Test-ProjectOpen) {
    Write-Host 'TESTS NOT RUN: this project is open in the Unity editor. Close Unity, or run the tests inside it with Window > General > Test Runner.' -ForegroundColor Red
    exit 3
}

$logs = Join-Path $Project 'Logs'
New-Item -ItemType Directory -Force -Path $logs | Out-Null
$stamp = Get-Date -Format 'yyyyMMdd-HHmmss'
$name = $Platform.ToLower()
$results = Join-Path $logs "test-results-$name.xml"
$log = Join-Path $logs "test-$name-$stamp.log"
if (Test-Path $results) { Remove-Item $results -Force }
# -runTests quits Unity by itself when the run ends; -quit must not be passed with it.
$unityArgs = @('-runTests', '-batchmode', '-projectPath', (Quote $Project), '-testPlatform', $Platform, '-testResults', (Quote $results), '-logFile', (Quote $log))
if ($Filter) { $unityArgs += @('-testFilter', (Quote $Filter)) }

Write-Host "Running $Platform tests with $unity"
Write-Host 'This takes a minute or two (longer the first time, while Unity imports the project).'
$started = Get-Date
$proc = Start-Process -FilePath $unity -ArgumentList ($unityArgs -join ' ') -PassThru -NoNewWindow
$null = $proc.Handle
$proc.WaitForExit()
$code = $proc.ExitCode
$seconds = [math]::Round(((Get-Date) - $started).TotalSeconds)

if (-not (Test-Path $results)) {
    Write-Host "TESTS DID NOT RUN (Unity exit code $code, $seconds s). Usually a script compile error; the log says why:" -ForegroundColor Red
    if (Test-Path $log) {
        $lines = Get-Content $log | Where-Object { $_ -match 'error CS\d+|Multiple Unity instances|another Unity instance|Aborting|No tests' } | Select-Object -First 25
        if (-not $lines) { $lines = Get-Content $log -Tail 30 }
        foreach ($l in $lines) { Write-Host ('  ' + $l) }
    }
    Write-Host "Full log: $log"
    exit 3
}

[xml]$xml = Get-Content $results -Raw
$run = $xml.'test-run'
$total = [int]$run.total
$passed = [int]$run.passed
$failed = [int]$run.failed
$skipped = [int]$run.skipped
$inconclusive = [int]$run.inconclusive
Write-Host ''
Write-Host "$total tests: $passed passed, $failed failed, $skipped skipped, $inconclusive inconclusive ($seconds s)"
$failures = $xml.SelectNodes("//test-case[@result='Failed']")
foreach ($f in $failures) {
    Write-Host ("FAILED  " + $f.fullname) -ForegroundColor Red
    $msg = $f.SelectSingleNode('failure/message')
    if ($msg) {
        foreach ($l in (($msg.InnerText.Trim()) -split "`n" | Select-Object -First 6)) { Write-Host ('        ' + $l.Trim()) }
    }
}
Write-Host "Results: $results"
Write-Host "Log: $log"
if ($failed -gt 0 -or ([string]$run.result) -like 'Failed*' -or $total -eq 0) {
    Write-Host 'TESTS FAILED' -ForegroundColor Red
    exit 1
}
Write-Host 'ALL TESTS PASSED' -ForegroundColor Green
exit 0
