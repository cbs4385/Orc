#!/bin/bash
# Watchdog training loop — auto-restarts when communicator drops
# Usage: ./train_headless_watchdog.sh [run-id] [stop-hour]
# The trainer uses --resume to continue from the last checkpoint on each restart.

set -e

RUN_ID="${1:-headless_wd}"
STOP_HOUR="${2:-7}"

PROJECT_DIR="C:/Users/chris/source/repos/Orc/Defender of the Orcish Marches"
CONFIG="$PROJECT_DIR/ml_training_config_headless.yaml"
RESULTS_DIR="$PROJECT_DIR/results"
VENV="C:/Users/chris/source/repos/Orc/ml-venv/Scripts"
UNITY_EXE="/c/Program Files/Unity/Hub/Editor/6000.3.6f1/Editor/Unity.exe"

mkdir -p "$RESULTS_DIR"

RESTART_COUNT=0
FIRST_RUN=false

echo "============================================"
echo " Watchdog ML Training - Orcish Marches"
echo " Run ID: $RUN_ID"
echo " Stops at: ${STOP_HOUR}:00"
echo "============================================"

cleanup() {
    echo "[WATCHDOG] Cleaning up..."
    # Use PowerShell for reliable process killing (tasklist | grep hangs on Windows)
    powershell -Command "
        Stop-Process -Name mlagents-learn -Force -EA 0
        Stop-Process -Name python -Force -EA 0
        Stop-Process -Name Unity -Force -EA 0
        Stop-Process -Name UnityCrashHandler64 -Force -EA 0
        Stop-Process -Name UnityPackageManager -Force -EA 0
        Stop-Process -Name UnityAutoQuitter -Force -EA 0
        Stop-Process -Name Unity.ILPP.Runner -Force -EA 0
        Start-Sleep 10
        ri '$PROJECT_DIR/Temp/UnityLockfile' -Force -EA 0
        'cleanup done'
    " 2>/dev/null
    sleep 5
}

check_stop_time() {
    local current_hour=$(date +%H | sed 's/^0//')
    if [ "$RESTART_COUNT" -gt 0 ] && [ "$current_hour" -ge "$STOP_HOUR" ] && [ "$current_hour" -lt 23 ]; then
        return 0  # Time to stop
    fi
    return 1  # Keep going
}

while true; do
    # Check stop time
    if check_stop_time; then
        echo "[WATCHDOG] Stop hour ${STOP_HOUR}:00 reached. Training complete."
        echo "[WATCHDOG] Total restarts: $RESTART_COUNT"
        cleanup
        break
    fi

    RESTART_COUNT=$((RESTART_COUNT + 1))
    echo ""
    echo "[WATCHDOG] === Restart #$RESTART_COUNT at $(date +%H:%M:%S) ==="

    cleanup

    # Start trainer
    if $FIRST_RUN; then
        echo "[WATCHDOG] Starting trainer (first run, --force)..."
        "$VENV/mlagents-learn.exe" "$CONFIG" --run-id="$RUN_ID" --force --time-scale 5  \
            > "$RESULTS_DIR/trainer_${RUN_ID}.log" 2>&1 &
        FIRST_RUN=false
    else
        echo "[WATCHDOG] Starting trainer (--resume from checkpoint)..."
        "$VENV/mlagents-learn.exe" "$CONFIG" --run-id="$RUN_ID" --resume --time-scale 5  \
            > "$RESULTS_DIR/trainer_${RUN_ID}.log" 2>&1 &
    fi
    TRAINER_PID=$!

    # Wait for trainer to listen
    echo "[WATCHDOG] Waiting for trainer to listen..."
    for i in $(seq 1 30); do
        sleep 3
        if netstat -ano 2>/dev/null | grep -q ":5004.*LISTENING"; then
            break
        fi
        if ! powershell -Command "(Get-Process -Id $TRAINER_PID -EA 0) -ne \$null" 2>/dev/null | grep -qi "true"; then
            echo "[WATCHDOG] Trainer died during startup, retrying..."
            continue 2
        fi
    done

    if ! netstat -ano 2>/dev/null | grep -q ":5004.*LISTENING"; then
        echo "[WATCHDOG] Trainer failed to listen after 90s, retrying..."
        continue
    fi
    echo "[WATCHDOG] Trainer listening on port 5004."

    # Start Unity headless
    echo "[WATCHDOG] Launching Unity headless..."
    "$UNITY_EXE" \
        -projectPath "$PROJECT_DIR" \
        -batchmode \
        -nographics \
        -logFile "$RESULTS_DIR/unity_${RUN_ID}.log" \
        -executeMethod HeadlessEntryPoint.StartTraining &
    UNITY_PID=$!

    # Monitor both processes
    echo "[WATCHDOG] Monitoring training..."
    while true; do
        sleep 30

        # Check stop time
        if check_stop_time; then
            echo "[WATCHDOG] Stop hour reached during monitoring."
            cleanup
            echo "[WATCHDOG] Total restarts: $RESTART_COUNT"
            exit 0
        fi

        # Check if trainer died (use powershell to avoid tasklist hang)
        if ! powershell -Command "(Get-Process mlagents-learn -EA 0) -ne \$null" 2>/dev/null | grep -qi "true"; then
            echo "[WATCHDOG] Trainer process died at $(date +%H:%M:%S)."
            echo "[WATCHDOG] Last trainer output:"
            tail -5 "$RESULTS_DIR/trainer_${RUN_ID}.log" 2>/dev/null
            break
        fi

        # Check if Unity died
        if ! powershell -Command "(Get-Process Unity -EA 0) -ne \$null" 2>/dev/null | grep -qi "true"; then
            echo "[WATCHDOG] Unity process died at $(date +%H:%M:%S)."
            break
        fi
    done

    echo "[WATCHDOG] Restarting in 10 seconds..."
    sleep 10
done

echo "============================================"
echo " Training Complete"
echo " Run ID: $RUN_ID"
echo " Total restarts: $RESTART_COUNT"
echo " Results: $RESULTS_DIR/$RUN_ID"
echo "============================================"
