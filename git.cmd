@echo off
set "GIT=%LOCALAPPDATA%\Programs\MinGit\cmd\git.exe"
if not exist "%GIT%" set "GIT=git"
"%GIT%" %*
