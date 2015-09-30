cd %~dp0\bin
del *.nupkg
%~dp0\src\.nuget\nuget.exe pack %~dp0\src\LogSearchShipper\LogSearchShipper.csproj
%~dp0\src\.nuget\nuget.exe push *.nupkg