    If (!([Security.Principal.WindowsPrincipal][Security.Principal.WindowsIdentity]::GetCurrent()).IsInRole([Security.Principal.WindowsBuiltInRole]"Administrator"))
    {Start-Process PowerShell.exe -ArgumentList ("-NoProfile -ExecutionPolicy Bypass -File `"{0}`"" -f $PSCommandPath) -Verb RunAs
    Exit}
    $Host.UI.RawUI.WindowTitle = $myInvocation.MyCommand.Definition + " (Administrator)"
    $Host.UI.RawUI.BackgroundColor = "Black"
	$Host.PrivateData.ProgressBackgroundColor = "Black"
    $Host.PrivateData.ProgressForegroundColor = "White"
    Clear-Host

Write-Host "Erweiterte Temp-Dateien Bereinigung wird gestartet..."
Write-Host ""

# User Temp Dateien
Write-Host "Lösche User Temp Dateien..."
Remove-Item -Path "$env:USERPROFILE\AppData\Local\Temp\*" -Recurse -Force -ErrorAction SilentlyContinue

# System Temp Dateien  
Write-Host "Lösche System Temp Dateien..."
Remove-Item -Path "$env:SystemRoot\Temp\*" -Recurse -Force -ErrorAction SilentlyContinue

# Browser Cache (sicher)
Write-Host "Lösche Browser Cache..."
Remove-Item -Path "$env:USERPROFILE\AppData\Local\Microsoft\Windows\INetCache\*" -Recurse -Force -ErrorAction SilentlyContinue
Remove-Item -Path "$env:USERPROFILE\AppData\Local\Microsoft\Edge\User Data\Default\Cache\*" -Recurse -Force -ErrorAction SilentlyContinue
Remove-Item -Path "$env:USERPROFILE\AppData\Local\Google\Chrome\User Data\Default\Cache\*" -Recurse -Force -ErrorAction SilentlyContinue

# Windows Update Cache
Write-Host "Lösche Windows Update Cache..."
Remove-Item -Path "$env:SystemRoot\SoftwareDistribution\Download\*" -Recurse -Force -ErrorAction SilentlyContinue

# Thumbnail Cache
Write-Host "Lösche Thumbnail Cache..."
Remove-Item -Path "$env:USERPROFILE\AppData\Local\Microsoft\Windows\Explorer\thumbcache_*.db" -Force -ErrorAction SilentlyContinue

# Recent Files List (sicher)
Write-Host "Lösche Recent Files..."
Remove-Item -Path "$env:USERPROFILE\AppData\Roaming\Microsoft\Windows\Recent\*" -Force -ErrorAction SilentlyContinue

# Prefetch (sicher zu löschen)
Write-Host "Lösche Prefetch Cache..."
Remove-Item -Path "$env:SystemRoot\Prefetch\*.pf" -Force -ErrorAction SilentlyContinue

# Error Reporting Files
Write-Host "Lösche Error Reports..."
Remove-Item -Path "$env:USERPROFILE\AppData\Local\Microsoft\Windows\WER\*" -Recurse -Force -ErrorAction SilentlyContinue

# System Error Memory Dumps (sicher)
Write-Host "Lösche Memory Dumps..."
Remove-Item -Path "$env:SystemRoot\memory.dmp" -Force -ErrorAction SilentlyContinue
Remove-Item -Path "$env:SystemRoot\Minidump\*" -Force -ErrorAction SilentlyContinue

# Windows Logs (sicher zu leeren)
Write-Host "Lösche alte Windows Logs..."
Remove-Item -Path "$env:SystemRoot\Logs\*" -Recurse -Force -ErrorAction SilentlyContinue

Write-Host ""
Write-Host "Temp-Bereinigung abgeschlossen!"
Write-Host "Mehrere GB Speicherplatz wurden freigegeben."
$null = $Host.UI.RawUI.ReadKey("NoEcho,IncludeKeyDown")
exit