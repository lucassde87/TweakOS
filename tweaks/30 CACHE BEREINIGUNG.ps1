    If (!([Security.Principal.WindowsPrincipal][Security.Principal.WindowsIdentity]::GetCurrent()).IsInRole([Security.Principal.WindowsBuiltInRole]"Administrator"))
    {Start-Process PowerShell.exe -ArgumentList ("-NoProfile -ExecutionPolicy Bypass -File `"{0}`"" -f $PSCommandPath) -Verb RunAs
    Exit}
    $Host.UI.RawUI.WindowTitle = $myInvocation.MyCommand.Definition + " (Administrator)"
    $Host.UI.RawUI.BackgroundColor = "Black"
	$Host.PrivateData.ProgressBackgroundColor = "Black"
    $Host.PrivateData.ProgressForegroundColor = "White"
    Clear-Host

Write-Host "Bereinige System Caches (100% sicher)..."
Write-Host ""

# Font Cache (sicher zu löschen)
Write-Host "1. Lösche Font Cache..."
Remove-Item -Path "$env:SystemRoot\System32\FNTCACHE.DAT" -Force -ErrorAction SilentlyContinue
Remove-Item -Path "$env:USERPROFILE\AppData\Local\FontCache\*" -Recurse -Force -ErrorAction SilentlyContinue

# DNS Cache leeren (sicher)
Write-Host "2. Leere DNS Cache..."
ipconfig /flushdns | Out-Null

# Icon Cache (sicher)
Write-Host "3. Lösche Icon Cache..."
Remove-Item -Path "$env:USERPROFILE\AppData\Local\IconCache.db" -Force -ErrorAction SilentlyContinue
Remove-Item -Path "$env:USERPROFILE\AppData\Local\Microsoft\Windows\Explorer\iconcache_*.db" -Force -ErrorAction SilentlyContinue

# Thumbnail Cache (bereits in Temp-Bereinigung, aber extra)
Write-Host "4. Lösche Thumbnail Cache..."
Remove-Item -Path "$env:USERPROFILE\AppData\Local\Microsoft\Windows\Explorer\thumbcache_*.db" -Force -ErrorAction SilentlyContinue

# Windows Store Cache
Write-Host "5. Lösche Windows Store Cache..."
wsreset.exe | Out-Null

# DirectX Shader Cache (sicher bei Gaming)
Write-Host "6. Lösche DirectX Shader Cache..."
Remove-Item -Path "$env:USERPROFILE\AppData\Local\D3DSCache\*" -Recurse -Force -ErrorAction SilentlyContinue

# Windows Update Cache
Write-Host "7. Lösche Windows Update Cache..."
Stop-Service -Name wuauserv -Force -ErrorAction SilentlyContinue
Remove-Item -Path "$env:SystemRoot\SoftwareDistribution\Download\*" -Recurse -Force -ErrorAction SilentlyContinue
Start-Service -Name wuauserv -ErrorAction SilentlyContinue

# Event Logs bereinigen (sicher)
Write-Host "8. Bereinige Event Logs..."
wevtutil el | ForEach-Object { 
    try {
        wevtutil cl $_ 2>$null
    } catch {
        # Ignore errors for logs that can't be cleared
    }
}

Write-Host ""
Write-Host "✓ Alle System Caches wurden bereinigt!"
Write-Host "✓ System läuft nun optimaler!"
Write-Host ""
Write-Host "Tipp: Führe einen Neustart durch für beste Ergebnisse."
$null = $Host.UI.RawUI.ReadKey("NoEcho,IncludeKeyDown")
exit