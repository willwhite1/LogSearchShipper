@echo off
IF NOT EXIST current GOTO MKLINK
.\current\LogSearchShipper.exe uninstall
rmdir current
:MKLINK
mklink /j current {{TARGETDIR}}

SETX /m EnvironmentType LIVE
SETX /m EnvironmentName {{ENVIRONMENTNAME}}

.\current\LogSearchShipper.exe install
net start LogSearchShipper
Pause