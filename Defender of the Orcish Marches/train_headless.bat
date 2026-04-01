@echo off
REM Headless ML training launcher for Defender of the Orcish Marches
REM Usage: train_headless.bat [run-id] [--resume]
REM
REM Prerequisites:
REM   1. Build the project: Unity > File > Build Settings > Build (Windows)
REM      OR use the built-in -batchmode approach (no build needed)
REM   2. Activate ml-venv: ml-venv\Scripts\activate
REM   3. Run this script

setlocal

set RUN_ID=%1
if "%RUN_ID%"=="" set RUN_ID=headless_v1

set RESUME_FLAG=%2

set PROJECT_DIR=%~dp0
set UNITY_PROJECT=%PROJECT_DIR%
set CONFIG=%PROJECT_DIR%ml_training_config_headless.yaml
set RESULTS_DIR=%PROJECT_DIR%results

REM Auto-detect Unity installation
set UNITY_EXE=
for /d %%d in ("C:\Program Files\Unity\Hub\Editor\6000*") do set UNITY_EXE=%%d\Editor\Unity.exe
if "%UNITY_EXE%"=="" (
    echo ERROR: Unity 6000.x not found in default Hub location.
    echo Set UNITY_EXE manually: set UNITY_EXE="path\to\Unity.exe"
    exit /b 1
)

echo ============================================
echo  Headless ML Training - Orcish Marches
echo ============================================
echo Unity:     %UNITY_EXE%
echo Project:   %UNITY_PROJECT%
echo Config:    %CONFIG%
echo Run ID:    %RUN_ID%
echo Results:   %RESULTS_DIR%
echo ============================================

REM Start mlagents-learn in the background
echo Starting mlagents-learn trainer...
if "%RESUME_FLAG%"=="--resume" (
    start "ML-Agents Trainer" cmd /c "cd /d %PROJECT_DIR% && mlagents-learn %CONFIG% --run-id=%RUN_ID% --resume 2>&1 | tee results\trainer_%RUN_ID%.log"
) else (
    start "ML-Agents Trainer" cmd /c "cd /d %PROJECT_DIR% && mlagents-learn %CONFIG% --run-id=%RUN_ID% 2>&1 | tee results\trainer_%RUN_ID%.log"
)

REM Wait for trainer to open its port
echo Waiting for trainer to initialize...
timeout /t 5 /nobreak >nul

REM Launch Unity in headless batch mode
echo Launching Unity in headless mode...
"%UNITY_EXE%" ^
    -projectPath "%UNITY_PROJECT%" ^
    -batchmode ^
    -nographics ^
    -logFile "%RESULTS_DIR%\unity_%RUN_ID%.log" ^
    -executeMethod HeadlessEntryPoint.StartTraining

echo.
echo Training started. Monitor progress:
echo   Trainer log: %RESULTS_DIR%\trainer_%RUN_ID%.log
echo   Unity log:   %RESULTS_DIR%\unity_%RUN_ID%.log
echo   TensorBoard: tensorboard --logdir %RESULTS_DIR%\%RUN_ID%
echo.
echo Press Ctrl+C in the trainer window to stop training.

endlocal
