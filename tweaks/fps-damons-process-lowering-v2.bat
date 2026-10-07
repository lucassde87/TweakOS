@echo off
cls
title DAMON PROCESS LOWERING V2
if not exist "C:\temp\" mkdir "C:\temp\"
echo [-] Creating System Restore Point (Damon V2 Restore Point)
echo [-] Download Resources (MSI Util, Wub, Power Plan)
md C:\damons\resources
curl -g -k -L -# -o "C:\damons\resources\Wub.exe" "https://r2.e-z.host/9e71d386-8c79-465c-9927-36ba4162c8f4/9cllihtz.exe"
curl -g -k -L -# -o "C:\damons\resources\DQ_Guide.txt" "https://github.com/Damonstweaks/data-queue-size-txt/releases/download/text/Data.Queue.Size.Guide.txt"
curl -g -k -L -# -o "C:\damons\resources\MSI_Utility_V3.exe" "https://r2.e-z.host/9e71d386-8c79-465c-9927-36ba4162c8f4/9tlvwpdm.exe"
powershell.exe -Command "Checkpoint-Computer -Description 'Damon V2' -RestorePointType 'MODIFY_SETTINGS'"
echo.
echo This legacy FPS package contains multiple system, registry, power-plan,
echo debloat, Windows Update and network changes.
echo It also downloads third-party executables.
echo.
echo It is disabled by default in TweakOS until each operation is reviewed.
echo.
pause
