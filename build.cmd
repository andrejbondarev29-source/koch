@echo off
setlocal
set "DOTNET_ROOT=%LOCALAPPDATA%\Microsoft\dotnet"
if not exist "%DOTNET_ROOT%\dotnet.exe" (
  echo .NET 8 SDK not found at "%DOTNET_ROOT%".
  pause
  exit /b 1
)
"%DOTNET_ROOT%\dotnet.exe" build "%~dp0DvorFight.csproj" -c Release %*
echo.
echo Build finished. Run run.cmd to play.
pause