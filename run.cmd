@echo off
setlocal
set "DOTNET_ROOT=%LOCALAPPDATA%\Microsoft\dotnet"
set "BIN=%~dp0bin\Release\net8.0"
if not exist "%BIN%\DvorFight.exe" set "BIN=%~dp0bin\Debug\net8.0"
if not exist "%BIN%\DvorFight.exe" (
  echo Game is not built yet. Run build.cmd first.
  pause
  exit /b 1
)
if /i "%~1"=="--selftest" (
  "%DOTNET_ROOT%\dotnet.exe" "%BIN%\DvorFight.dll" --selftest
  pause
  exit /b 0
)
start "" /D "%BIN%" "%BIN%\DvorFight.exe"