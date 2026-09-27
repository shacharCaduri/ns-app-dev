#!/usr/bin/env bash
# Batch-mode Unity helper for humans and agents. Run `Tools/unity/unity.sh help` for usage.
# Works on the checkout (or git worktree) that contains this script.
set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
PROJECT="$(git -C "$SCRIPT_DIR" rev-parse --show-toplevel)"
UNITY="${UNITY_PATH:-/Applications/Unity/Hub/Editor/6000.3.7f1/Unity.app/Contents/MacOS/Unity}"
LOGS="$PROJECT/Logs"
BUILD_DIR="$PROJECT/Builds/macOS"
SCENE_BUILDER="WizardArena.EditorTools.SceneBuilder.RebuildFromCommandLine"
SCENE_SCREENSHOT="WizardArena.EditorTools.SceneScreenshot.CaptureFromCommandLine"

usage() {
    cat <<EOF
Usage: Tools/unity/unity.sh <command>

Commands:
  compile         Open the project in batch mode and report C# compile errors.
  test-editmode   Run EditMode tests.
  test-playmode   Run PlayMode tests.
  test            Run EditMode, then PlayMode tests.
  rebuild-scene   Regenerate Assets/Scenes/WizardMovement.unity with the editor scene builder.
  screenshot      Render Camera.main to Logs/screenshot.png for a headless visual check.
  build-macos     Build a macOS player (stub for now, added in ticket [15]).

Project: $PROJECT
Unity:   $UNITY  (override with UNITY_PATH)
Logs and test results go to $LOGS/.
Exit code is non-zero when anything fails.
EOF
}

die() {
    echo "ERROR: $*" >&2
    exit 1
}

preflight() {
    [[ -x "$UNITY" ]] || die "Unity not found at $UNITY. Set UNITY_PATH to the Unity binary."
    local lock="$PROJECT/Temp/UnityLockfile"
    if [[ -e "$lock" ]] && lsof "$lock" >/dev/null 2>&1; then
        die "This project is already open in another Unity instance ($PROJECT). Close it first, or run from another git worktree."
    fi
    mkdir -p "$LOGS"
}

# Runs Unity with the given arguments; stores the exit code in UNITY_EXIT.
run_unity() {
    local log="$1"
    shift
    echo "Running Unity ($*)"
    echo "  log: $log"
    set +e
    "$UNITY" -batchmode -nographics -projectPath "$PROJECT" -logFile "$log" "$@"
    UNITY_EXIT=$?
    set -e
}

# Same as run_unity, but without -nographics: on macOS that flag leaves Camera.Render() with no
# GPU device, so a RenderTexture capture comes back blank. Only the screenshot command needs this.
run_unity_with_graphics() {
    local log="$1"
    shift
    echo "Running Unity ($*)"
    echo "  log: $log"
    set +e
    "$UNITY" -batchmode -projectPath "$PROJECT" -logFile "$log" "$@"
    UNITY_EXIT=$?
    set -e
}

# Prints unique compile errors from a log. Returns 0 if there were any.
print_compile_errors() {
    local log="$1"
    local errors
    errors="$(grep -E 'error CS[0-9]+' "$log" 2>/dev/null | sed -E 's/^[[:space:]]+//' | sort -u || true)"
    [[ -n "$errors" ]] || return 1
    echo "Compile errors:"
    echo "$errors" | head -n 30 | sed 's/^/  /'
    return 0
}

print_log_tail() {
    echo "Last lines of $1:"
    tail -n 25 "$1" 2>/dev/null | sed 's/^/  /' || true
}

cmd_compile() {
    preflight
    local log="$LOGS/unity-compile.log"
    run_unity "$log" -quit
    if print_compile_errors "$log"; then
        echo "COMPILE: FAILED"
        return 1
    fi
    if [[ "$UNITY_EXIT" -ne 0 ]]; then
        print_log_tail "$log"
        echo "COMPILE: FAILED (Unity exit code $UNITY_EXIT)"
        return 1
    fi
    echo "COMPILE: OK"
}

xml_attr() {
    xmllint --xpath "string(/test-run/@$2)" "$1" 2>/dev/null || true
}

# $1 = EditMode | PlayMode
run_tests() {
    local platform="$1"
    local name
    name="$(echo "$platform" | tr '[:upper:]' '[:lower:]')"
    local log="$LOGS/unity-test-$name.log"
    local xml="$LOGS/test-results-$name.xml"
    rm -f "$xml"
    run_unity "$log" -runTests -testPlatform "$platform" -testResults "$xml"

    if [[ ! -s "$xml" ]]; then
        print_compile_errors "$log" || print_log_tail "$log"
        echo "$platform TESTS: FAILED (no results file, Unity exit code $UNITY_EXIT)"
        return 1
    fi

    local total passed failed skipped
    total="$(xml_attr "$xml" total)"
    passed="$(xml_attr "$xml" passed)"
    failed="$(xml_attr "$xml" failed)"
    skipped="$(xml_attr "$xml" skipped)"
    echo "  results: $xml"
    if [[ "${failed:-0}" != "0" ]]; then
        echo "Failed tests:"
        xmllint --xpath '//test-case[@result="Failed"]/@fullname' "$xml" 2>/dev/null \
            | sed -E 's/^[[:space:]]*fullname="([^"]*)"/  \1/' || true
    fi
    echo "$platform TESTS: passed $passed, failed $failed, skipped $skipped, total $total"
    if [[ "$UNITY_EXIT" -ne 0 || "${failed:-0}" != "0" || "${total:-0}" == "0" ]]; then
        echo "$platform TESTS: FAILED"
        return 1
    fi
    echo "$platform TESTS: OK"
}

cmd_test() {
    preflight
    local status=0
    run_tests EditMode || status=1
    run_tests PlayMode || status=1
    if [[ "$status" -ne 0 ]]; then
        echo "ALL TESTS: FAILED"
    else
        echo "ALL TESTS: OK"
    fi
    return "$status"
}

cmd_rebuild_scene() {
    preflight
    local log="$LOGS/unity-rebuild-scene.log"
    run_unity "$log" -quit -executeMethod "$SCENE_BUILDER"
    if [[ "$UNITY_EXIT" -ne 0 ]] || ! grep -q '\[SceneBuilder\] Rebuilt' "$log"; then
        print_compile_errors "$log" || print_log_tail "$log"
        echo "REBUILD SCENE: FAILED (Unity exit code $UNITY_EXIT)"
        return 1
    fi
    echo "REBUILD SCENE: OK (review and commit Assets/Scenes/WizardMovement.unity)"
}

# Renders Camera.main to a PNG so agents can check scene layout without Play Mode. UI Toolkit
# overlays (HUD, screens) are not captured: there is no camera involved in drawing them.
cmd_screenshot() {
    preflight
    local log="$LOGS/unity-screenshot.log"
    local out="$LOGS/screenshot.png"
    rm -f "$out"
    run_unity_with_graphics "$log" -quit -executeMethod "$SCENE_SCREENSHOT"
    if [[ "$UNITY_EXIT" -ne 0 ]] || [[ ! -s "$out" ]]; then
        print_compile_errors "$log" || print_log_tail "$log"
        echo "SCREENSHOT: FAILED (Unity exit code $UNITY_EXIT)"
        return 1
    fi
    echo "SCREENSHOT: OK ($out)"
}

# Stub: the project's Standalone scripting backend is IL2CPP, which is not installed on this
# machine, so a plain -buildOSXUniversalPlayer fails. Player builds are set up in ticket [15].
cmd_build_macos() {
    echo "build-macos is not available yet: macOS builds are added in ticket [15]." >&2
    echo "(Target output: $BUILD_DIR/WizardArena.app)" >&2
    return 2
}

UNITY_EXIT=0
case "${1:-help}" in
    compile) cmd_compile ;;
    test-editmode) preflight; run_tests EditMode ;;
    test-playmode) preflight; run_tests PlayMode ;;
    test) cmd_test ;;
    rebuild-scene) cmd_rebuild_scene ;;
    screenshot) cmd_screenshot ;;
    build-macos) cmd_build_macos ;;
    help | -h | --help) usage ;;
    *)
        usage >&2
        exit 1
        ;;
esac
