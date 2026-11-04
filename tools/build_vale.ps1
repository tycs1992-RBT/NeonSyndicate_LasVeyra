<#
.SYNOPSIS
  Builds the Shardwake Vale pilot for Windows from the command line, without opening the Unity editor.

.DESCRIPTION
  Runs Unity in batch mode with NeonSyndicate.EditorTools.ShardwakeValeBuild.BuildWindowsPilot. That build contains
  exactly one scene, Assets/Scenes/ShardwakeVale.unity, which is therefore its startup scene; the original city scene
  and the project's build list are not used. The player lands in Builds\ShardwakeVale\ShardwakeVale.exe and the full
  Unity log in Logs\. The original game's tools\build.ps1 is unchanged and still builds the original game.
  Close the Unity editor for this project first: Unity cannot open the same project twice.

.PARAMETER UnityPath
  Full path to Unity.exe when it is not in Unity Hub's usual folder. The UNITY_EXE environment variable works too.

.EXAMPLE
  powershell -ExecutionPolicy Bypass -File tools\build_vale.ps1
#>
[CmdletBinding()]
param([string]$UnityPath = '')

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

try {
    $unity = Find-Unity $UnityPath
} catch {
    Write-Host "PILOT BUILD FAILED: $($_.Exception.Message)" -ForegroundColor Red
    exit 2
}
if (Test-ProjectOpen) {
    Write-Host 'PILOT BUILD FAILED: this project is open in the Unity editor. Close Unity, or use Shardwake Vale > Build Shardwake Vale for Windows (pilot only) inside it.' -ForegroundColor Red
    exit 2
}

$logs = Join-Path $Project 'Logs'
New-Item -ItemType Directory -Force -Path $logs | Out-Null
$log = Join-Path $logs ('vale-build-' + (Get-Date -Format 'yyyyMMdd-HHmmss') + '.log')
$output = Join-Path $Project 'Builds\ShardwakeVale\ShardwakeVale.exe'
$arguments = @('-batchmode', '-quit', '-projectPath', ('"' + $Project + '"'), '-buildTarget', 'StandaloneWindows64',
    '-executeMethod', 'NeonSyndicate.EditorTools.ShardwakeValeBuild.BuildWindowsPilot', '-logFile', ('"' + $log + '"'))
Write-Host "Building the Shardwake Vale pilot with $unity"
Write-Host "Log: $log"
$process = Start-Process -FilePath $unity -ArgumentList $arguments -Wait -PassThru -NoNewWindow
if (Test-Path $log) {
    Select-String -Path $log -Pattern '\[Shardwake Vale\]|error CS\d+' | ForEach-Object { Write-Host ('  ' + $_.Line.Trim()) }
}
if ($process.ExitCode -eq 0 -and (Test-Path $output)) {
    Write-Host "PILOT BUILD SUCCEEDED: $output (startup scene Assets/Scenes/ShardwakeVale.unity)" -ForegroundColor Green
    exit 0
}
Write-Host "PILOT BUILD FAILED (Unity exit code $($process.ExitCode)). Read $log" -ForegroundColor Red
exit 1
