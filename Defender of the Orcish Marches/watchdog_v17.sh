#!/bin/bash
# Watchdog for ML training v17 - auto-restarts on crash
# Run from: Defender of the Orcish Marches/ directory

VENV="../ml-venv/Scripts"
CONFIG="ml_training_config_headless.yaml"
SERVER_EXE="Builds/MLTrainingServer/OrcTrainer.exe"
RUN_ID="server_v17"
LOG="results/trainer_server_v17.log"
RESTART_COUNT=0

kill_zombies() {
    echo "[WATCHDOG] Cleaning up old processes..."
    powershell.exe -Command "
        Stop-Process -Name OrcTrainer -Force -ErrorAction SilentlyContinue
        Stop-Process -Name python -Force -ErrorAction SilentlyContinue
        Stop-Process -Name mlagents-learn -Force -ErrorAction SilentlyContinue
    " 2>/dev/null
    sleep 3

    # Verify port 5005 is free, if not kill by PID
    local pid
    pid=$(netstat -ano 2>/dev/null | grep ":5005.*LISTENING" | head -1 | awk '{print $5}')
    if [ -n "$pid" ]; then
        echo "[WATCHDOG] Port 5005 still held by PID $pid, force killing..."
        powershell.exe -Command "Stop-Process -Id $pid -Force" 2>/dev/null
        sleep 3
    fi
}

cleanup() {
    echo "[WATCHDOG] Shutting down..."
    powershell.exe -Command "
        Stop-Process -Name OrcTrainer -Force -ErrorAction SilentlyContinue
    " 2>/dev/null
    exit 0
}
trap cleanup SIGINT SIGTERM

while true; do
    RESTART_COUNT=$((RESTART_COUNT + 1))
    echo "[WATCHDOG] === Restart #$RESTART_COUNT at $(date) ==="

    kill_zombies

    # Final port check
    if netstat -ano 2>/dev/null | grep -q ":5005.*LISTENING"; then
        echo "[WATCHDOG] Port 5005 STILL in use after cleanup. Waiting 15s..."
        sleep 15
    fi

    echo "[WATCHDOG] Starting trainer (--resume)..."
    "$VENV/mlagents-learn.exe" "$CONFIG" \
        --run-id="$RUN_ID" \
        --env="$SERVER_EXE" \
        --no-graphics \
        --time-scale=5 \
        --resume 2>&1 | tee -a "$LOG"

    EXIT_CODE=$?
    echo "[WATCHDOG] Trainer exited with code $EXIT_CODE at $(date)"
    echo "[WATCHDOG] Restarting in 10 seconds..."
    sleep 10
done
