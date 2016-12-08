@REM This script can fix the symbolic links used in this project in Windows,
@REM especially for fixing the cases where symbolic links are checked out as plain text files.
@ECHO OFF
IF NOT EXIST WikiDiffSummary\ (
	DEL WikiDiffSummary
	MKLINK /d /j WikiDiffSummary "modules/WikiDiffSummary/WikiDiffSummary"
	GIT update-index --assume-unchanged WikiDiffSummary
)