@echo on 
:: Delete all log files in the current directory and its subdirectories
del /a /s /q *.log

:: Remove all files from the Windows temp directory
del /s /f /q C:\Windows\Temp\*.*

:: Clear the Prefetch folder
del /s /f /q C:\Windows\Prefetch\*

:: Clear the temporary files in the user's temp directory
del /s /f /q %temp%\*.*

:: Flush the DNS cache
ipconfig /flushdns

:: Stop Windows Update services
net stop wuauserv
net stop UsoSvc

:: Remove the SoftwareDistribution folder and recreate it
rd /s /q C:\Windows\SoftwareDistribution
md C:\Windows\SoftwareDistribution

:: Remove all files from the user's temp directory and recreate it
rd /s /q %temp%
mkdir %temp%

:: Take ownership of the temp directories
takeown /f "%temp%" /r /d y
takeown /f "C:\Windows\Temp" /r /d y

:: Remove the Windows Temp directory and recreate it
rd /s /q C:\Windows\Temp
mkdir C:\Windows\Temp

:: Take ownership of the Windows Temp directory again
takeown /f "C:\Windows\Temp" /r /d y
takeown /f %temp% /r /d y

:: Delete temporary files and folders using the deltree command
deltree /y C:\Windows\Temp\*
deltree /y C:\Windows\Temp
deltree /y C:\Windows\Tmp
deltree /y C:\Windows\FF*.tmp
deltree /y C:\Windows\History
deltree /y C:\Windows\Cookies
deltree /y C:\Windows\Recent
deltree /y C:\Windows\Spool\Printers
@echo off
cls
echo.
echo Cleaned All Temp Files
echo ======================
echo.  
pause
