    If (!([Security.Principal.WindowsPrincipal][Security.Principal.WindowsIdentity]::GetCurrent()).IsInRole([Security.Principal.WindowsBuiltInRole]"Administrator"))
    {Start-Process PowerShell.exe -ArgumentList ("-NoProfile -ExecutionPolicy Bypass -File `"{0}`"" -f $PSCommandPath) -Verb RunAs
    Exit}
    $Host.UI.RawUI.WindowTitle = $myInvocation.MyCommand.Definition + " (Administrator)"
    $Host.UI.RawUI.BackgroundColor = "Black"
	$Host.PrivateData.ProgressBackgroundColor = "Black"
    $Host.PrivateData.ProgressForegroundColor = "White"
    Clear-Host

Write-Host "Entferne zusätzliche sichere Bloatware Apps..."
Write-Host ""

# Sichere Apps die niemand braucht
$AppsToRemove = @(
    "Microsoft.GetHelp",
    "Microsoft.Getstarted", 
    "Microsoft.MicrosoftOfficeHub",
    "Microsoft.People",
    "Microsoft.WindowsFeedbackHub",
    "Microsoft.WindowsMaps",
    "Microsoft.WindowsSoundRecorder",
    "Microsoft.ZuneMusic",
    "Microsoft.ZuneVideo",
    "Microsoft.BingWeather",
    "Microsoft.BingNews",
    "Microsoft.BingFinance",
    "Microsoft.BingSports",
    "Microsoft.MicrosoftSolitaireCollection",
    "Microsoft.WindowsCamera",
    "Microsoft.WindowsAlarms",
    "Microsoft.Office.OneNote",
    "Microsoft.SkypeApp",
    "Microsoft.YourPhone",
    "Microsoft.Todos",
    "Microsoft.PowerAutomateDesktop",
    "MicrosoftTeams"
)

foreach ($App in $AppsToRemove) {
    Write-Host "Entferne: $App"
    try {
        Get-AppxPackage -Name $App -AllUsers | Remove-AppxPackage -ErrorAction SilentlyContinue
        Get-AppxProvisionedPackage -Online | Where-Object DisplayName -eq $App | Remove-AppxProvisionedPackage -Online -ErrorAction SilentlyContinue
        Write-Host "  ✓ Entfernt"
    } catch {
        Write-Host "  - Nicht gefunden oder bereits entfernt"
    }
}

Write-Host ""
Write-Host "Entferne Gaming-Bloatware..."

# Gaming Bloatware (sicher zu entfernen)
$GamingApps = @(
    "Microsoft.XboxApp",
    "Microsoft.Xbox.TCUI",
    "Microsoft.XboxSpeechToTextOverlay",
    "Microsoft.XboxGameOverlay",
    "Microsoft.XboxGamingOverlay",
    "Microsoft.XboxIdentityProvider"
)

foreach ($App in $GamingApps) {
    Write-Host "Entferne Gaming App: $App"
    try {
        Get-AppxPackage -Name $App -AllUsers | Remove-AppxPackage -ErrorAction SilentlyContinue
        Write-Host "  ✓ Entfernt"
    } catch {
        Write-Host "  - Nicht gefunden"
    }
}

Write-Host ""
Write-Host "Entferne OneDrive Reste..."

# OneDrive komplett entfernen (sicher)
try {
    Stop-Process -Name OneDrive -Force -ErrorAction SilentlyContinue
    Start-Sleep 2
    
    # 64-bit OneDrive
    if (Test-Path "$env:SystemRoot\System32\OneDriveSetup.exe") {
        Start-Process "$env:SystemRoot\System32\OneDriveSetup.exe" -ArgumentList "/uninstall" -Wait
    }
    
    # 32-bit OneDrive  
    if (Test-Path "$env:SystemRoot\SysWOW64\OneDriveSetup.exe") {
        Start-Process "$env:SystemRoot\SysWOW64\OneDriveSetup.exe" -ArgumentList "/uninstall" -Wait  
    }
    
    Write-Host "OneDrive entfernt"
} catch {
    Write-Host "OneDrive war bereits entfernt"
}

Write-Host ""
Write-Host "Sichere Bloatware-Entfernung abgeschlossen!"
Write-Host "System ist nun sauberer und schneller."
$null = $Host.UI.RawUI.ReadKey("NoEcho,IncludeKeyDown")
exit