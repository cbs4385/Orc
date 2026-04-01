#!/bin/bash
# Watchdog training loop using the built server executable
# Usage: ./train_watchdog.sh [run-id]
#
# Runs mlagents-learn with --env= pointing to the built OrcTrainer.exe.
# On crash, kills zombies, waits, and restarts with --resume.
# First run uses --force, subsequent runs use --resume.

set -u

RUN_ID="${1:-server_v14}"
SCRIPT_DIR="$(cd "$(dirname "$0")" && pwd)"
CONFIG="$SCRIPT_DIR/ml_training_config_headless.yaml"
RESULTS_DIR="$SCRIPT_DIR/results"
SERVER_EXE="$SCRIPT_DIR/Builds/MLTrainingServer/OrcTrainer.exe"
VENV_DIR="C:/Users/chris/source/repos/Orc/ml-venv/Scripts"

mkdir -p "$RESULTS_DIR"

if [[ ! -f "$SERVER_EXE" ]]; then
    echo "ERROR: Server build not found at $SERVER_EXE"
    exit 1
fi

source "$VENV_DIR/activate"

RESTART_COUNT=0

echo "============================================"
echo " Watchdog ML Training - Server Build"
echo " Run ID: $RUN_ID"
echo " Server: $SERVER_EXE"
echo "============================================"

while true; do
    RESTART_COUNT=$((RESTART_COUNT + 1))
    echo ""
    echo "[WATCHDOG] === Restart #$RESTART_COUNT at $(date) ==="

    # Kill any leftover processes
    taskkill //F //IM OrcTrainer.exe 2>/dev/null
    sleep 5

    # Kill OrcTrainer zombie and wait for port to release
    taskkill //F //IM OrcTrainer.exe 2>/dev/null
    echo "[WATCHDOG] Waiting 30s for port 5004 to release..."
    sleep 30
    # If port still held, kill by PID
    for pid in $(netstat -ano 2>/dev/null | grep ":5004.*LISTENING" | awk '{print $5}' | sort -u); do
        echo "[WATCHDOG] Force-killing PID $pid on port 5004"
        taskkill //F //PID "$pid" 2>/dev/null
    done
    sleep 5

    # Use --resume if results exist, --force otherwise
    if [[ -d "$RESULTS_DIR/$RUN_ID" ]]; then
        echo "[WATCHDOG] Starting trainer (--resume from checkpoint)..."
        mlagents-learn "$CONFIG" \
            --env="$SERVER_EXE" \
            --run-id="$RUN_ID" \
            --no-graphics \
            --time-scale=5 \
            --resume \
            2>&1 | tee -a "$RESULTS_DIR/trainer_${RUN_ID}.log"
    else
        echo "[WATCHDOG] Starting trainer (--force, fresh run)..."
        mlagents-learn "$CONFIG" \
            --env="$SERVER_EXE" \
            --run-id="$RUN_ID" \
            --no-graphics \
            --time-scale=5 \
            --force \
            2>&1 | tee -a "$RESULTS_DIR/trainer_${RUN_ID}.log"
    fi

    EXIT_CODE=$?
    echo "[WATCHDOG] Trainer exited with code $EXIT_CODE at $(date)"
    echo "[WATCHDOG] Restarting in 20 seconds..."
    sleep 20
done
