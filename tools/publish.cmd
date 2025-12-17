@echo off
setlocal

set RID=win-x64
set CONFIG=Release

if not "%~1"=="" set RID=%~1
if not "%~2"=="" set CONFIG=%~2

set REPO=%~dp0..
set APP=%REPO%\src\LIGHTNING.App\LIGHTNING.App.csproj
set OUT=%REPO%\artifacts\publish\%RID%\self-contained-folder

echo Publishing LIGHTNING.App...
echo   Config: %CONFIG%
echo   RID:    %RID%
echo   Out:    %OUT%

dotnet publish "%APP%" ^
  -c %CONFIG% ^
  -r %RID% ^
  --self-contained true ^
  -o "%OUT%" ^
  -p:PublishSingleFile=false ^
  -p:PublishTrimmed=false

endlocal
