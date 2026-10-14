<#
.SYNOPSIS
  Builds Neon Syndicate: Las Veyra from the command line, without opening the Unity editor.

.DESCRIPTION
  Finds Unity (the version named in ProjectSettings/ProjectVersion.txt, in Unity Hub's install folder), runs it in
  batch mode with NeonSyndicate.EditorTools.BuildScripts and prints a short summary. The game lands in
  Builds\Windows\NeonSyndicate.exe (or Builds\Mac\NeonSyndicate.app). The full Unity log is saved in Logs\.
  Close the Unity editor for this project first: Unity cannot open the same project twice.

.PARAMETER Target
  Windows (the default) or Mac. A Mac build made on Windows needs "Mac Build Support (Mono)" added in Unity Hub.

.PARAMETER UnityPath
  Full path to Unity.exe when it is not in Unity Hub's usual folder. The UNITY_EXE environment variable works too.

.PARAMETER Development
  Makes a development build (it can connect to the Unity profiler and shows a debug console).

.EXAMPLE
  powershell -ExecutionPolicy Bypass -File tools\build.ps1

.EXAMPLE
  powershell -ExecutionPolicy Bypass -File tools\build.ps1 -Target Mac -UnityPath "D:\Unity\6000.3.16f1\Editor\Unity.exe"
#>
[CmdletBinding()]
param(
    [ValidateSet('Windows', 'Mac')]
    [string]$Target = 'Windows',
    [string]$UnityPath = '',
    [switch]$Development
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
    # Unity Hub remembers a custom install folder (Preferences > Installs) in this file.
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
    # Any other install of the same Unity release line (for example another 6000.3 patch) also works.
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

function Show-NewLogLines([string]$Path, [ref]$Offset) {
    if (-not (Test-Path $Path)) { return }
    try {
        $fs = [System.IO.File]::Open($Path, [System.IO.FileMode]::Open, [System.IO.FileAccess]::Read, [System.IO.FileShare]::ReadWrite)
        try {
            if ($fs.Length -le $Offset.Value) { return }
            $null = $fs.Seek($Offset.Value, [System.IO.SeekOrigin]::Begin)
            $reader = New-Object System.IO.StreamReader($fs)
            $text = $reader.ReadToEnd()
            $Offset.Value = $fs.Length
            foreach ($l in ($text -split "`n")) {
                if ($l -match '\[Neon Syndicate\]|error CS\d+') { Write-Host ('  ' + $l.Trim()) }
            }
        } finally { $fs.Close() }
    } catch { }
}

try {
    $unity = Find-Unity $UnityPath
} catch {
    Write-Host "BUILD FAILED: $($_.Exception.Message)" -ForegroundColor Red
    exit 2
}
if (Test-ProjectOpen) {
    Write-Host 'BUILD FAILED: this project is open in the Unity editor. Close Unity (or use Neon Syndicate > Build Game for Windows inside it) and try again.' -ForegroundColor Red
    exit 2
}

$logs = Join-Path $Project 'Logs'
New-Item -ItemType Directory -Force -Path $logs | Out-Null
$stamp = Get-Date -Format 'yyyyMMdd-HHmmss'
$log = Join-Path $logs ("build-" + $Target.ToLower() + "-$stamp.log")
if ($Target -eq 'Mac') {
    $method = 'NeonSyndicate.EditorTools.BuildScripts.BuildMacOS'
    $buildTarget = 'StandaloneOSX'
    $output = Join-Path $Project 'Builds\Mac\NeonSyndicate.app'
} else {
    $method = 'NeonSyndicate.EditorTools.BuildScripts.BuildWindows64'
    $buildTarget = 'StandaloneWindows64'
    $output = Join-Path $Project 'Builds\Windows\NeonSyndicate.exe'
}
$unityArgs = @('-batchmode', '-quit', '-projectPath', (Quote $Project), '-buildTarget', $buildTarget, '-executeMethod', $method, '-logFile', (Quote $log))
if ($Development) { $unityArgs += '-nsDevelopment' }

Write-Host "Building Neon Syndicate for $Target with $unity"
Write-Host "This takes a few minutes (much longer the first time, while Unity imports the project)."
$started = Get-Date
$proc = Start-Process -FilePath $unity -ArgumentList ($unityArgs -join ' ') -PassThru -NoNewWindow
$null = $proc.Handle
$offset = [long]0
while (-not $proc.HasExited) {
    Start-Sleep -Seconds 2
    Show-NewLogLines $log ([ref]$offset)
}
$proc.WaitForExit()
Show-NewLogLines $log ([ref]$offset)
$code = $proc.ExitCode
$minutes = [math]::Round(((Get-Date) - $started).TotalMinutes, 1)

$text = ''
if (Test-Path $log) { $text = Get-Content $log -Raw }
$succeeded = ($code -eq 0) -and ($text -match 'BUILD SUCCEEDED')
Write-Host ''
if ($succeeded) {
    Write-Host "BUILD SUCCEEDED in $minutes min: $output" -ForegroundColor Green
    if ($text -match 'Active Input Handling switched') {
        Write-Host 'Note: Unity switched the project to Active Input Handling "Both" during this build. Run the build once more so controllers are surely included.' -ForegroundColor Yellow
    }
    Write-Host "Full log: $log"
    exit 0
}
Write-Host "BUILD FAILED (Unity exit code $code) after $minutes min." -ForegroundColor Red
$errors = @()
if ($text) {
    $errors = ($text -split "`n") | Where-Object { $_ -match 'error CS\d+|BUILD FAILED|Build Support|Multiple Unity instances|another Unity instance|Aborting batchmode|compilation errors' } | Select-Object -First 25
}
if ($errors.Count -gt 0) {
    Write-Host 'What went wrong:'
    foreach ($e in $errors) { Write-Host ('  ' + $e.Trim()) }
} elseif (Test-Path $log) {
    Write-Host 'Last lines of the Unity log:'
    Get-Content $log -Tail 30 | ForEach-Object { Write-Host ('  ' + $_) }
}
Write-Host "Full log: $log"
exit 1
