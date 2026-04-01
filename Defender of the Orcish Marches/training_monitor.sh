#!/bin/bash
# Training Monitor — shows live stats in a terminal window
# Usage: ./training_monitor.sh [run-id]
# Opens a refreshing dashboard of training metrics.

RUN_ID="${1:-headless_v7}"
PROJECT_DIR="C:/Users/chris/source/repos/Orc/Defender of the Orcish Marches"
VENV="C:/Users/chris/source/repos/Orc/ml-venv/Scripts"
REFRESH=10

while true; do
    clear
    echo "╔══════════════════════════════════════════════════════════╗"
    echo "║         ML TRAINING MONITOR — $RUN_ID"
    echo "║         $(date '+%Y-%m-%d %H:%M:%S')"
    echo "╠══════════════════════════════════════════════════════════╣"

    # Process status
    UNITY_RUNNING=$(powershell -Command "(Get-Process Unity -EA 0) -ne \$null" 2>/dev/null)
    TRAINER_RUNNING=$(powershell -Command "(Get-Process mlagents-learn -EA 0) -ne \$null" 2>/dev/null)

    if echo "$UNITY_RUNNING" | grep -qi "true"; then
        echo "║  Unity:    ● RUNNING"
    else
        echo "║  Unity:    ○ STOPPED"
    fi
    if echo "$TRAINER_RUNNING" | grep -qi "true"; then
        echo "║  Trainer:  ● RUNNING"
    else
        echo "║  Trainer:  ○ STOPPED"
    fi

    echo "╠══════════════════════════════════════════════════════════╣"

    # Latest trainer steps and rewards
    echo "║  TRAINER LOG (latest):"
    LINES=$(grep -E "Step:" "$PROJECT_DIR/results/trainer_${RUN_ID}.log" 2>/dev/null | tail -6)
    if [ -n "$LINES" ]; then
        echo "$LINES" | while IFS= read -r line; do
            # Extract using sed instead of grep -P
            AGENT=$(echo "$line" | sed -n 's/.*\] \([A-Za-z]*\)\. Step:.*/\1/p')
            STEP=$(echo "$line" | sed -n 's/.*Step: \([0-9]*\).*/\1/p')
            REWARD=$(echo "$line" | sed -n 's/.*Mean Reward: \([-0-9.]*\).*/\1/p')
            if [ -n "$REWARD" ]; then
                printf "║    %-20s Step: %-8s Reward: %s\n" "$AGENT" "$STEP" "$REWARD"
            elif [ -n "$STEP" ]; then
                printf "║    %-20s Step: %-8s (no episodes yet)\n" "$AGENT" "$STEP"
            fi
        done
    else
        echo "║    (no trainer data yet)"
    fi

    echo "╠══════════════════════════════════════════════════════════╣"

    # TensorBoard metrics
    echo "║  TENSORBOARD METRICS:"
    "$VENV/python.exe" -c "
from tensorboard.backend.event_processing import event_accumulator
for agent in ['DefenderCommander', 'OrcCommander']:
    try:
        ea = event_accumulator.EventAccumulator(f'$PROJECT_DIR/results/$RUN_ID/{agent}')
        ea.Reload()
        tags = {
            'Environment/Cumulative Reward': 'Reward',
            'Policy/Entropy': 'Entropy',
            'Policy/Extrinsic Value Estimate': 'Value',
            'Self-play/ELO': 'ELO',
        }
        parts = []
        step = 0
        for tag, label in tags.items():
            vals = ea.Scalars(tag) if tag in ea.Tags().get('scalars',[]) else []
            if vals:
                parts.append(f'{label}={vals[-1].value:+.3f}')
                step = max(step, vals[-1].step)
        if parts:
            print(f'    {agent[:8]:8s} step={step:>8d}  {\"  \".join(parts)}')
    except: pass
" 2>/dev/null | while IFS= read -r line; do echo "║  $line"; done

    echo "╠══════════════════════════════════════════════════════════╣"

    # Checkpoint info
    CKPT_COUNT=$(find "$PROJECT_DIR/results/$RUN_ID/" -name "*.onnx" 2>/dev/null | wc -l)
    LATEST_CKPT=$(ls -t "$PROJECT_DIR/results/$RUN_ID/"*/*.onnx 2>/dev/null | head -1 | xargs basename 2>/dev/null)
    echo "║  Checkpoints: $CKPT_COUNT active    Latest: ${LATEST_CKPT:-(none)}"

    # Watchdog info
    # Check all watchdog logs for the current run
    WDLOG=$(ls -t "$PROJECT_DIR/results/watchdog"*.log 2>/dev/null | head -1)
    RESTARTS=$(grep -c "Restart" "$WDLOG" 2>/dev/null || echo "0")
    LAST_RESTART=$(grep "Restart" "$WDLOG" 2>/dev/null | tail -1 | sed 's/.*at //')
    echo "║  Watchdog restarts: $RESTARTS    Last: ${LAST_RESTART:-(none)}"

    echo "╚══════════════════════════════════════════════════════════╝"
    echo ""
    echo "  Refreshing every ${REFRESH}s. Press Ctrl+C to exit."

    sleep $REFRESH
done
