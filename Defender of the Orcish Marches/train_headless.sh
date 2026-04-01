#!/bin/bash
# Headless ML training launcher for Defender of the Orcish Marches
# Usage: ./train_headless.sh [run-id] [--resume]
#
# Prerequisites:
#   1. Activate ml-venv: source ml-venv/bin/activate (or ml-venv/Scripts/activate on Git Bash)
#   2. Run this script

set -e

RUN_ID="${1:-headless_v1}"
RESUME_FLAG="$2"

SCRIPT_DIR="$(cd "$(dirname "$0")" && pwd)"
PROJECT_DIR="$SCRIPT_DIR"
CONFIG="$PROJECT_DIR/ml_training_config_headless.yaml"
RESULTS_DIR="$PROJECT_DIR/results"

mkdir -p "$RESULTS_DIR"

# Auto-detect Unity
UNITY_EXE=""
if [[ "$OSTYPE" == "msys" || "$OSTYPE" == "win32" ]]; then
    # Windows (Git Bash / MSYS)
    for d in /c/Program\ Files/Unity/Hub/Editor/6000*/Editor/Unity.exe; do
        UNITY_EXE="$d"
    done
elif [[ "$OSTYPE" == "darwin"* ]]; then
    # macOS
    for d in /Applications/Unity/Hub/Editor/6000*/Unity.app/Contents/MacOS/Unity; do
        UNITY_EXE="$d"
    done
else
    # Linux
    for d in ~/Unity/Hub/Editor/6000*/Editor/Unity; do
        UNITY_EXE="$d"
    done
fi

if [[ -z "$UNITY_EXE" || ! -f "$UNITY_EXE" ]]; then
    echo "ERROR: Unity 6000.x not found. Set UNITY_EXE manually."
    exit 1
fi

echo "============================================"
echo " Headless ML Training - Orcish Marches"
echo "============================================"
echo "Unity:     $UNITY_EXE"
echo "Project:   $PROJECT_DIR"
echo "Config:    $CONFIG"
echo "Run ID:    $RUN_ID"
echo "Results:   $RESULTS_DIR"
echo "============================================"

# Start mlagents-learn
echo "Starting mlagents-learn trainer..."
TRAINER_CMD="mlagents-learn $CONFIG --run-id=$RUN_ID"
if [[ "$RESUME_FLAG" == "--resume" ]]; then
    TRAINER_CMD="$TRAINER_CMD --resume"
fi

$TRAINER_CMD 2>&1 | tee "$RESULTS_DIR/trainer_${RUN_ID}.log" &
TRAINER_PID=$!

# Wait for trainer port
echo "Waiting for trainer to initialize..."
sleep 5

# Launch Unity headless
echo "Launching Unity in headless mode..."
"$UNITY_EXE" \
    -projectPath "$PROJECT_DIR" \
    -batchmode \
    -nographics \
    -logFile "$RESULTS_DIR/unity_${RUN_ID}.log" \
    -executeMethod HeadlessEntryPoint.StartTraining &
UNITY_PID=$!

echo ""
echo "Training started (trainer PID=$TRAINER_PID, Unity PID=$UNITY_PID)"
echo "  Trainer log: $RESULTS_DIR/trainer_${RUN_ID}.log"
echo "  Unity log:   $RESULTS_DIR/unity_${RUN_ID}.log"
echo "  TensorBoard: tensorboard --logdir $RESULTS_DIR/$RUN_ID"
echo ""
echo "Press Ctrl+C to stop training."

# Trap Ctrl+C to clean up both processes
trap "echo 'Stopping...'; kill $TRAINER_PID $UNITY_PID 2>/dev/null; exit 0" INT TERM

# Wait for trainer to finish (Unity exits when trainer disconnects)
wait $TRAINER_PID
kill $UNITY_PID 2>/dev/null
echo "Training complete."
