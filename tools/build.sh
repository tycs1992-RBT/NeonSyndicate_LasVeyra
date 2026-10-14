#!/usr/bin/env bash
# Builds Neon Syndicate: Las Veyra from the command line on a Mac, without opening the Unity editor.
#
#   tools/build.sh                         macOS build in Builds/Mac/NeonSyndicate.app
#   tools/build.sh --target windows        Windows build (needs "Windows Build Support (Mono)" in Unity Hub)
#   tools/build.sh --unity /path/to/Unity  use this Unity binary (or set UNITY_EXE)
#   tools/build.sh --development           development build (profiler and debug console)
#
# Unity is looked up in Unity Hub's folder for the version in ProjectSettings/ProjectVersion.txt:
#   /Applications/Unity/Hub/Editor/<version>/Unity.app/Contents/MacOS/Unity
# The full Unity log is saved in Logs/. Close the Unity editor for this project first.
set -u

cd "$(dirname "$0")/.." || exit 2
PROJECT="$(pwd)"
TARGET=mac
UNITY_ARG=""
DEV=""
while [ $# -gt 0 ]; do
  case "$1" in
    --target) TARGET=$(echo "${2:-mac}" | tr '[:upper:]' '[:lower:]'); shift 2 ;;
    --unity|--unity-path) UNITY_ARG="${2:-}"; shift 2 ;;
    --development) DEV="-nsDevelopment"; shift ;;
    -h|--help) sed -n '2,12p' "$0"; exit 0 ;;
    *) echo "Unknown option: $1 (try --help)"; exit 2 ;;
  esac
done

project_version() {
  local v=""
  if [ -f ProjectSettings/ProjectVersion.txt ]; then
    v=$(sed -n 's/^m_EditorVersion:[[:space:]]*//p' ProjectSettings/ProjectVersion.txt | tr -d '\r' | head -n 1)
  fi
  [ -n "$v" ] || v="6000.3.16f1"
  echo "$v"
}

# Prints the Unity binary to use, or nothing.
find_unity() {
  if [ -n "$UNITY_ARG" ]; then echo "$UNITY_ARG"; return; fi
  if [ -n "${UNITY_EXE:-}" ]; then echo "$UNITY_EXE"; return; fi
  local version roots root custom cand line other
  version=$(project_version)
  roots="/Applications/Unity/Hub/Editor|$HOME/Applications/Unity/Hub/Editor"
  # Unity Hub remembers a custom install folder (Preferences > Installs) in this file.
  custom="$HOME/Library/Application Support/UnityHub/secondaryInstallPath.json"
  if [ -f "$custom" ]; then
    root=$(tr -d '"\r\n' < "$custom")
    [ -n "$root" ] && roots="$roots|$root"
  fi
  local IFS='|'
  for root in $roots; do
    for cand in "$root/$version/Unity.app/Contents/MacOS/Unity" "$root/$version/Editor/Unity"; do
      if [ -x "$cand" ]; then echo "$cand"; return; fi
    done
  done
  # Any other install of the same release line (for example another 6000.3 patch) also works.
  line=$(echo "$version" | cut -d. -f1-2)
  for root in $roots; do
    [ -d "$root" ] || continue
    other=$(ls -1 "$root" 2>/dev/null | grep "^$line\." | sort -r | head -n 1)
    if [ -n "$other" ]; then
      for cand in "$root/$other/Unity.app/Contents/MacOS/Unity" "$root/$other/Editor/Unity"; do
        if [ -x "$cand" ]; then echo "Unity $version is not installed; using $other instead." >&2; echo "$cand"; return; fi
      done
    fi
  done
}

UNITY=$(find_unity)
if [ -z "$UNITY" ] || [ ! -x "$UNITY" ]; then
  echo "BUILD FAILED: Unity $(project_version) was not found in /Applications/Unity/Hub/Editor. Install it with Unity Hub, or run: tools/build.sh --unity /path/to/Unity"
  exit 2
fi
if [ -f Temp/UnityLockfile ] && command -v lsof >/dev/null 2>&1 && lsof Temp/UnityLockfile >/dev/null 2>&1; then
  echo "BUILD FAILED: this project is open in the Unity editor. Close Unity (or use Neon Syndicate > Build Game for This Computer inside it) and try again."
  exit 2
fi

case "$TARGET" in
  mac|macos|osx) METHOD=NeonSyndicate.EditorTools.BuildScripts.BuildMacOS; BUILD_TARGET=StandaloneOSX; OUTPUT="$PROJECT/Builds/Mac/NeonSyndicate.app" ;;
  windows|win|win64) METHOD=NeonSyndicate.EditorTools.BuildScripts.BuildWindows64; BUILD_TARGET=StandaloneWindows64; OUTPUT="$PROJECT/Builds/Windows/NeonSyndicate.exe" ;;
  *) echo "Unknown target '$TARGET' (use mac or windows)"; exit 2 ;;
esac

mkdir -p Logs
LOG="$PROJECT/Logs/build-$TARGET-$(date +%Y%m%d-%H%M%S).log"
echo "Building Neon Syndicate for $TARGET with $UNITY"
echo "This takes a few minutes (much longer the first time, while Unity imports the project)."
START=$(date +%s)
"$UNITY" -batchmode -quit -projectPath "$PROJECT" -buildTarget "$BUILD_TARGET" -executeMethod "$METHOD" -logFile "$LOG" $DEV &
PID=$!
SHOWN=0
show_progress() {
  [ -f "$LOG" ] || return
  local n
  n=$(wc -l < "$LOG" | tr -d ' ')
  if [ "$n" -gt "$SHOWN" ]; then
    sed -n "$((SHOWN + 1)),${n}p" "$LOG" | grep -E '\[Neon Syndicate\]|error CS[0-9]+' | sed 's/^/  /'
    SHOWN=$n
  fi
}
while kill -0 "$PID" 2>/dev/null; do
  sleep 2
  show_progress
done
wait "$PID"
CODE=$?
show_progress
MINUTES=$(( ($(date +%s) - START + 30) / 60 ))

echo
if [ "$CODE" -eq 0 ] && grep -q "BUILD SUCCEEDED" "$LOG" 2>/dev/null; then
  echo "BUILD SUCCEEDED in about $MINUTES min: $OUTPUT"
  if grep -q "Active Input Handling switched" "$LOG" 2>/dev/null; then
    echo "Note: Unity switched the project to Active Input Handling \"Both\" during this build. Run the build once more so controllers are surely included."
  fi
  echo "Full log: $LOG"
  exit 0
fi
echo "BUILD FAILED (Unity exit code $CODE) after about $MINUTES min."
if [ -f "$LOG" ]; then
  ERRORS=$(grep -E 'error CS[0-9]+|BUILD FAILED|Build Support|Multiple Unity instances|another Unity instance|Aborting batchmode|compilation errors' "$LOG" | head -n 25)
  if [ -n "$ERRORS" ]; then
    echo "What went wrong:"
    echo "$ERRORS" | sed 's/^/  /'
  else
    echo "Last lines of the Unity log:"
    tail -n 30 "$LOG" | sed 's/^/  /'
  fi
fi
echo "Full log: $LOG"
exit 1
