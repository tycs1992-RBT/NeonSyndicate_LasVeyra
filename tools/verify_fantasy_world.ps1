<# Opens the existing project in its exact installed Unity version and the interactive native acceptance tool.
   This launcher does not claim a test ran, activate a license, download an Editor, or modify source/saves. #>
[CmdletBinding()]
param([string]$UnityPath = '')
$ErrorActionPreference = 'Stop'
$FantasyProject = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path
$FantasyVersionFile = Join-Path $FantasyProject 'ProjectSettings\ProjectVersion.txt'
$FantasyVersionLine = Get-Content $FantasyVersionFile | Where-Object { $_ -match '^m_EditorVersion:\s*(\S+)' } | Select-Object -First 1
if (-not $FantasyVersionLine) { throw 'This folder has no readable Unity project version.' }
$FantasyVersion = ($FantasyVersionLine -split '\s+')[1]
$FantasyLock = Join-Path $FantasyProject 'Temp\UnityLockfile'
if (Test-Path $FantasyLock) {
    try { $FantasyStream = [IO.File]::Open($FantasyLock, 'Open', 'ReadWrite', 'None'); $FantasyStream.Close() }
    catch {
        Write-Host 'This project is already open. In that Unity window choose Neon Syndicate > Acceptance > Verify Fantasy World.'
        Read-Host 'Press Enter to close this launcher'
        exit 0
    }
}
$FantasyCandidates = @()
if ($UnityPath) { $FantasyCandidates += $UnityPath }
if ($env:UNITY_EXE) { $FantasyCandidates += $env:UNITY_EXE }
if ($env:ProgramFiles) { $FantasyCandidates += (Join-Path $env:ProgramFiles "Unity\Hub\Editor\$FantasyVersion\Editor\Unity.exe") }
if ($env:APPDATA) {
    $FantasyHubFile = Join-Path $env:APPDATA 'UnityHub\secondaryInstallPath.json'
    if (Test-Path $FantasyHubFile) {
        try {
            $FantasyCustom = Get-Content $FantasyHubFile -Raw | ConvertFrom-Json
            if ($FantasyCustom -is [string]) { $FantasyCandidates += (Join-Path $FantasyCustom "$FantasyVersion\Editor\Unity.exe") }
        } catch { Write-Warning 'Unity Hub custom installation path could not be read.' }
    }
}
$FantasyEditor = $FantasyCandidates | Where-Object { Test-Path -LiteralPath $_ -PathType Leaf } | Select-Object -First 1
if (-not $FantasyEditor) {
    Add-Type -AssemblyName System.Windows.Forms
    $FantasyPicker = New-Object System.Windows.Forms.OpenFileDialog
    $FantasyPicker.Title = "Select Unity $FantasyVersion Editor (Unity.exe)"
    $FantasyPicker.Filter = 'Unity Editor|Unity.exe'
    if ($FantasyPicker.ShowDialog() -ne [System.Windows.Forms.DialogResult]::OK) { Write-Host 'No Editor was selected. No test ran.'; exit 1 }
    $FantasyEditor = $FantasyPicker.FileName
}
$FantasyProductVersion = [Diagnostics.FileVersionInfo]::GetVersionInfo($FantasyEditor).ProductVersion
if (-not $FantasyProductVersion -or $FantasyProductVersion -notlike "$FantasyVersion*") {
    throw "The selected executable does not identify as Unity $FantasyVersion. Choose that exact version in Unity Hub; no project was opened."
}
$FantasyLogDir = Join-Path $FantasyProject 'Logs'
New-Item -ItemType Directory -Path $FantasyLogDir -Force | Out-Null
$FantasyLog = Join-Path $FantasyLogDir 'FantasyWorldLauncher.log'
$FantasyArguments = @('-projectPath', ('"' + $FantasyProject + '"'), '-executeMethod', 'NeonSyndicate.Editor.FantasyAcceptanceV1.OpenFromCommandLine', '-logFile', ('"' + $FantasyLog + '"'))
Start-Process -FilePath $FantasyEditor -ArgumentList $FantasyArguments | Out-Null
Write-Host 'Unity was requested to open the native acceptance window. Import, gameplay, screenshots and acceptance have not run yet.'
Write-Host 'If compilation blocks the window, keep the first red Unity Console error. The local startup log is Logs\FantasyWorldLauncher.log.'
