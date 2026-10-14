#!/usr/bin/env bash
# Runs the Neon Syndicate automated tests (Unity Test Framework, EditMode) from the command line on a Mac.
#
#   tools/test.sh                          all EditMode tests
#   tools/test.sh --filter SaveDataTests   only tests whose full name matches
#   tools/test.sh --platform PlayMode      Play mode tests instead
#   tools/test.sh --unity /path/to/Unity   use this Unity binary (or set UNITY_EXE)
#
# Prints a pass/fail summary from the NUnit results file and the message of every failing test. Exits with 0 when
# everything passed, 1 when a test failed and 3 when Unity produced no results (usually a compile error; see the log).
# Results and the log are saved in Logs/. Close the Unity editor for this project first.
set -u

cd "$(dirname "$0")/.." || exit 3
PROJECT="$(pwd)"
PLATFORM=EditMode
FILTER=""
UNITY_ARG=""
while [ $# -gt 0 ]; do
  case "$1" in
    --platform) PLATFORM="${2:-EditMode}"; shift 2 ;;
    --filter) FILTER="${2:-}"; shift 2 ;;
    --unity|--unity-path) UNITY_ARG="${2:-}"; shift 2 ;;
    -h|--help) sed -n '2,12p' "$0"; exit 0 ;;
    *) echo "Unknown option: $1 (try --help)"; exit 3 ;;
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

# Reads one attribute of the <test-run> element.
run_attr() {
  if command -v xmllint >/dev/null 2>&1; then
    xmllint --xpath "string(/test-run/@$1)" "$RESULTS" 2>/dev/null
  else
    grep -o "<test-run [^>]*" "$RESULTS" | head -n 1 | sed -n "s/.* $1=\"\([^\"]*\)\".*/\1/p"
  fi
}

UNITY=$(find_unity)
if [ -z "$UNITY" ] || [ ! -x "$UNITY" ]; then
  echo "TESTS NOT RUN: Unity $(project_version) was not found in /Applications/Unity/Hub/Editor. Install it with Unity Hub, or run: tools/test.sh --unity /path/to/Unity"
  exit 3
fi
if [ -f Temp/UnityLockfile ] && command -v lsof >/dev/null 2>&1 && lsof Temp/UnityLockfile >/dev/null 2>&1; then
  echo "TESTS NOT RUN: this project is open in the Unity editor. Close Unity, or run the tests inside it with Window > General > Test Runner."
  exit 3
fi

mkdir -p Logs
NAME=$(echo "$PLATFORM" | tr '[:upper:]' '[:lower:]')
RESULTS="$PROJECT/Logs/test-results-$NAME.xml"
LOG="$PROJECT/Logs/test-$NAME-$(date +%Y%m%d-%H%M%S).log"
rm -f "$RESULTS"
echo "Running $PLATFORM tests with $UNITY"
echo "This takes a minute or two (longer the first time, while Unity imports the project)."
START=$(date +%s)
# -runTests quits Unity by itself when the run ends; -quit must not be passed with it.
if [ -n "$FILTER" ]; then
  "$UNITY" -runTests -batchmode -projectPath "$PROJECT" -testPlatform "$PLATFORM" -testResults "$RESULTS" -logFile "$LOG" -testFilter "$FILTER"
else
  "$UNITY" -runTests -batchmode -projectPath "$PROJECT" -testPlatform "$PLATFORM" -testResults "$RESULTS" -logFile "$LOG"
fi
CODE=$?
SECONDS_TAKEN=$(( $(date +%s) - START ))

if [ ! -f "$RESULTS" ]; then
  echo "TESTS DID NOT RUN (Unity exit code $CODE, ${SECONDS_TAKEN} s). Usually a script compile error; the log says why:"
  if [ -f "$LOG" ]; then
    LINES=$(grep -E 'error CS[0-9]+|Multiple Unity instances|another Unity instance|Aborting|No tests' "$LOG" | head -n 25)
    [ -n "$LINES" ] || LINES=$(tail -n 30 "$LOG")
    echo "$LINES" | sed 's/^/  /'
  fi
  echo "Full log: $LOG"
  exit 3
fi

TOTAL=$(run_attr total); PASSED=$(run_attr passed); FAILED=$(run_attr failed)
SKIPPED=$(run_attr skipped); INCONCLUSIVE=$(run_attr inconclusive); RESULT=$(run_attr result)
echo
echo "${TOTAL:-0} tests: ${PASSED:-0} passed, ${FAILED:-0} failed, ${SKIPPED:-0} skipped, ${INCONCLUSIVE:-0} inconclusive (${SECONDS_TAKEN} s)"
if [ "${FAILED:-0}" != "0" ]; then
  if command -v xmllint >/dev/null 2>&1; then
    COUNT=$(xmllint --xpath "count(//test-case[@result='Failed'])" "$RESULTS" 2>/dev/null)
    i=1
    while [ "$i" -le "${COUNT:-0}" ]; do
      echo "FAILED  $(xmllint --xpath "string((//test-case[@result='Failed'])[$i]/@fullname)" "$RESULTS" 2>/dev/null)"
      xmllint --xpath "string((//test-case[@result='Failed'])[$i]/failure/message)" "$RESULTS" 2>/dev/null | head -n 6 | sed 's/^/        /'
      i=$((i + 1))
    done
  else
    grep -o '<test-case [^>]*result="Failed"[^>]*' "$RESULTS" | sed -n 's/.*fullname="\([^"]*\)".*/FAILED  \1/p'
  fi
fi
echo "Results: $RESULTS"
echo "Log: $LOG"
case "$RESULT" in
  Failed*) STATUS=1 ;;
  *) STATUS=0 ;;
esac
if [ "${FAILED:-0}" != "0" ] || [ "${TOTAL:-0}" = "0" ]; then STATUS=1; fi
if [ "$STATUS" -ne 0 ]; then echo "TESTS FAILED"; else echo "ALL TESTS PASSED"; fi
exit $STATUS
