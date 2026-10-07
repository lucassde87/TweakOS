@echo off
cls
title DAMON NETWORK V1
if not exist "C:\temp\" mkdir "C:\temp\"
cls
set w=[97m
set p=[95m
set b=[96m
echo [-] Creating System Restore Point (Damons Network V3)
echo [-] Downloading Resources (MSI Util, Text Guide)
md C:\damons
md C:\damons\resources
powershell.exe -Command "Checkpoint-Computer -Description 'Damons Network V1' -RestorePointType 'MODIFY_SETTINGS'"
curl -g -k -L -# -o "C:\damons\resources\MSI_Utility_V3.exe" "https://r2.e-z.host/9e71d386-8c79-465c-9927-36ba4162c8f4/9tlvwpdm.exe"
curl -g -k -L -# -o "C:\damons\resources\Guide.txt" "https://files.catbox.moe/k2g8ic.txt"
:main
chcp 65001 >nul 2>&1
cls
mode 123,35
color 0
echo.
echo.
echo          DAMON NETWORK
echo.
echo                                                           Ethernet + Wifi Safe
echo.
echo.
echo                                               [1] MSI Utility *UPDATED
echo.
echo                                               [2] Adapter Settings *ETHERNET ONLY
echo.
echo                                               [3] Disable Limiting *UPDATED
echo.
echo                                               [4] Registry Settings *UPDATED
echo.
echo.
set /p choice="Choose an option:"
if "%choice%"=="1" goto msi
if "%choice%"=="2" goto ncpa
if "%choice%"=="3" goto limiting
if "%choice%"=="4" goto registry
if /i "%choice%"=="x" goto exit
goto main

:ncpa
cls
ncpa.cpl
start C:\damons\resources\Guide.txt
goto main

:limiting
echo.
netsh advfirewall firewall add rule name="StopThrottling" dir=in action=block remoteip=173.194.55.0/24,206.111.0.0/16 enable=yes
netsh interface tcp set heuristics disabled
echo Successfully Disabled Limiting
pause >nul
cls
goto main

:registry
echo.
Reg.exe add "HKLM\SYSTEM\CurrentControlSet\Services\AFD\Parameters" /v "DefaultReceiveWindow" /t REG_DWORD /d "16384" /f
Reg.exe add "HKLM\SYSTEM\CurrentControlSet\Services\AFD\Parameters" /v "DefaultSendWindow" /t REG_DWORD /d "16384" /f
Reg.exe add "HKLM\SYSTEM\CurrentControlSet\Services\AFD\Parameters" /v "FastCopyReceiveThreshold" /t REG_DWORD /d "16384" /f
Reg.exe add "HKLM\SYSTEM\CurrentControlSet\Services\AFD\Parameters" /v "FastSendDatagramThreshold" /t REG_DWORD /d "16384" /f
Reg.exe add "HKLM\SYSTEM\CurrentControlSet\Services\AFD\Parameters" /v "DynamicSendBufferDisable" /t REG_DWORD /d "0" /f
Reg.exe add "HKLM\SYSTEM\CurrentControlSet\Services\AFD\Parameters" /v "IgnorePushBitOnReceives" /t REG_DWORD /d "1" /f
Reg.exe add "HKLM\SYSTEM\CurrentControlSet\Services\AFD\Parameters" /v "NonBlockingSendSpecialBuffering" /t REG_DWORD /d "1" /f
Reg.exe add "HKLM\SYSTEM\CurrentControlSet\Services\AFD\Parameters" /v "DisableRawSecurity" /t REG_DWORD /d "1" /f
echo Successfully Applied Registry Changes
pause >nul
cls
goto main

:msi
powershell -Command "& {Add-Type -AssemblyName System.Windows.Forms; [System.Windows.Forms.MessageBox]::Show('Check MSI Mode for your network adapter and set High Priority and then hit apply in top right corner.', 'Damons Free Network Panel', 'OK', [System.Windows.Forms.MessageBoxIcon]::Information);}"
start C:\damons\resources\MSI_Utility_V3.exe
pause
goto main
