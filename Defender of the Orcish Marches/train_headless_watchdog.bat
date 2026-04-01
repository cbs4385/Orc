@echo off
REM Watchdog training loop using the built server executable
REM Usage: train_headless_watchdog.bat [run-id] [stop-hour]
REM   run-id:    Name for this training run (default: server_v12)
REM   stop-hour: Hour (0-23) to stop training (default: 7 = 7 AM)
REM
REM Uses --env= with the built OrcTrainer.exe server (no Unity Editor needed).
REM The trainer uses --resume to continue from the last checkpoint on each restart.

setlocal enabledelayedexpansion

set RUN_ID=%~1
if "%RUN_ID%"=="" set RUN_ID=server_v12

set STOP_HOUR=%~2
if "%STOP_HOUR%"=="" set STOP_HOUR=7

set PROJECT_DIR=%~dp0
set CONFIG=%PROJECT_DIR%ml_training_config_headless.yaml
set RESULTS_DIR=%PROJECT_DIR%results
set VENV=C:\Users\chris\source\repos\Orc\ml-venv\Scripts
set SERVER_EXE=%PROJECT_DIR%Builds\MLTrainingServer\OrcTrainer.exe

if not exist "%SERVER_EXE%" (
    echo ERROR: Server build not found at %SERVER_EXE%
    echo Run ServerBuildScript.Build in Unity first.
    exit /b 1
)

if not exist "%RESULTS_DIR%" mkdir "%RESULTS_DIR%"

set RESTART_COUNT=0

echo ============================================
echo  Watchdog ML Training - Server Build
echo  Run ID: %RUN_ID%
echo  Server: %SERVER_EXE%
echo  Stops at: %STOP_HOUR%:00
echo ============================================

:LOOP
REM Check if we've passed the stop hour
for /f "tokens=1 delims=:" %%h in ("%TIME: =0%") do set CURRENT_HOUR=%%h
set /a CURRENT_HOUR=%CURRENT_HOUR%
if %RESTART_COUNT% gtr 0 (
    if %CURRENT_HOUR% geq %STOP_HOUR% (
        if %CURRENT_HOUR% lss 23 (
            echo [WATCHDOG] Stop hour %STOP_HOUR%:00 reached. Training complete.
            echo [WATCHDOG] Total restarts: %RESTART_COUNT%
            goto :END
        )
    )
)

set /a RESTART_COUNT+=1
echo.
echo [WATCHDOG] === Restart #%RESTART_COUNT% at %TIME% ===

REM Kill any leftover processes
taskkill /F /IM OrcTrainer.exe >nul 2>&1
timeout /t 3 /nobreak >nul

REM Kill zombie Python processes holding port 5004
for /f "tokens=5" %%p in ('netstat -ano ^| findstr ":5004.*LISTENING" 2^>nul') do (
    echo [WATCHDOG] Killing zombie process on port 5004: PID %%p
    taskkill /F /PID %%p >nul 2>&1
)
timeout /t 3 /nobreak >nul

REM Start trainer with --env= (launches server build automatically)
if %RESTART_COUNT% equ 1 (
    echo [WATCHDOG] Starting trainer (first run, --force)...
    start "ML-Trainer" /MIN cmd /c ""%VENV%\mlagents-learn.exe" "%CONFIG%" --run-id=%RUN_ID% --env="%SERVER_EXE%" --no-graphics --time-scale=5 --force > "%RESULTS_DIR%\trainer_%RUN_ID%.log" 2>&1"
) else (
    echo [WATCHDOG] Starting trainer (--resume from checkpoint)...
    start "ML-Trainer" /MIN cmd /c ""%VENV%\mlagents-learn.exe" "%CONFIG%" --run-id=%RUN_ID% --env="%SERVER_EXE%" --no-graphics --time-scale=5 --resume > "%RESULTS_DIR%\trainer_%RUN_ID%.log" 2>&1"
)

REM Wait for trainer to start producing output
echo [WATCHDOG] Waiting for trainer to initialize...
timeout /t 15 /nobreak >nul
echo [WATCHDOG] Trainer started.

REM Monitor trainer process
echo [WATCHDOG] Monitoring training...

:MONITOR
timeout /t 30 /nobreak >nul

REM Check stop hour
for /f "tokens=1 delims=:" %%h in ("%TIME: =0%") do set CURRENT_HOUR=%%h
set /a CURRENT_HOUR=%CURRENT_HOUR%
if %CURRENT_HOUR% geq %STOP_HOUR% (
    if %CURRENT_HOUR% lss 23 (
        echo [WATCHDOG] Stop hour %STOP_HOUR%:00 reached during monitoring.
        taskkill /F /IM mlagents-learn.exe >nul 2>&1
        taskkill /F /IM OrcTrainer.exe >nul 2>&1
        goto :END
    )
)

REM Check if trainer is still running
tasklist | findstr /I "mlagents-learn.exe" >nul 2>&1
if errorlevel 1 (
    echo [WATCHDOG] Trainer process died. Restarting...
    taskkill /F /IM OrcTrainer.exe >nul 2>&1
    timeout /t 5 /nobreak >nul
    goto :LOOP
)

REM Check if server is still running (mlagents-learn launches it)
tasklist | findstr /I "OrcTrainer.exe" >nul 2>&1
if errorlevel 1 (
    echo [WATCHDOG] Server process died. Restarting...
    taskkill /F /IM mlagents-learn.exe >nul 2>&1
    timeout /t 5 /nobreak >nul
    goto :LOOP
)

goto :MONITOR

:END
echo.
echo ============================================
echo  Training Complete
echo  Run ID: %RUN_ID%
echo  Total restarts: %RESTART_COUNT%
echo  Results: %RESULTS_DIR%\%RUN_ID%
echo ============================================
echo  To view: tensorboard --logdir "%RESULTS_DIR%\%RUN_ID%"

endlocal
