@echo off
echo Building BeaconOfHope mod...

:: Find MSBuild - try both VS2022 and VS2019 paths
set MSBUILD_PATH="C:\Program Files\Microsoft Visual Studio\2022\Community\MSBuild\Current\Bin\MSBuild.exe"
if not exist %MSBUILD_PATH% set MSBUILD_PATH="C:\Program Files (x86)\Microsoft Visual Studio\2019\Community\MSBuild\Current\Bin\MSBuild.exe"

:: Build the solution
cd Source
if exist %MSBUILD_PATH% (
    %MSBUILD_PATH% BeaconOfHope.sln /p:Configuration=Release
    if %ERRORLEVEL% NEQ 0 (
        echo Build failed! Press any key to exit...
        pause > nul
        exit /b %ERRORLEVEL%
    )
    echo Build completed successfully!
) else (
    echo MSBuild not found. Skipping build step.
    echo If you want to build, please install Visual Studio or set the correct path in this batch file.
)

:: Return to mod root directory
cd ..

echo Starting RimWorld in QuickTest mode...
start "" "c:\Program Files (x86)\Steam\steamapps\common\RimWorld\RimWorldWin64.exe" -quicktest
echo RimWorld launched!
