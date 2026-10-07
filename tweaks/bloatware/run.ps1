param([string]$Action = "run")
$ErrorActionPreference = "Stop"
$id=[Security.Principal.WindowsIdentity]::GetCurrent()
$p=New-Object Security.Principal.WindowsPrincipal($id)
if(-not $p.IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)){
  $args="-NoProfile -ExecutionPolicy Bypass -File `"$PSCommandPath`" -Action $Action"
  Start-Process powershell.exe -Verb RunAs -ArgumentList $args
  exit
}
Write-Host "TweakOS - Systemoptimierung" -ForegroundColor Cyan
Write-Host "Dieser Tweak kann Windows-Systemeinstellungen mit Administratorrechten ändern." -ForegroundColor Yellow
Write-Host "Antivirus-Software kann administrative Optimierungswerkzeuge heuristisch markieren."
Write-Host "Eine Erkennung allein beweist nicht, dass eine Datei Malware enthält."
Write-Host ""
$legacy=Join-Path $PSScriptRoot "legacy-source.ps1"
if(-not(Test-Path $legacy)){ throw "legacy-source.ps1 fehlt." }
& powershell.exe -NoProfile -ExecutionPolicy Bypass -File $legacy
exit $LASTEXITCODE
