@REM this script is used to generated the last git commit log to the file
@REM that will later be pakced into the application.
@echo off
git log -n 1 > %1