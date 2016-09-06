@echo off
IF NOT EXIST current GOTO MKLINK
.\current\LogSearchShipper.exe uninstall
rmdir current
:MKLINK
mklink /j current 1.13.260

SETX /m EnvironmentType LIVE
SETX /m EnvironmentName PROD-DESKTOP

.\current\LogSearchShipper.exe install
net start LogSearchShipper
Pause