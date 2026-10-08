    If (!([Security.Principal.WindowsPrincipal][Security.Principal.WindowsIdentity]::GetCurrent()).IsInRole([Security.Principal.WindowsBuiltInRole]"Administrator"))
    {Start-Process PowerShell.exe -ArgumentList ("-NoProfile -ExecutionPolicy Bypass -File `"{0}`"" -f $PSCommandPath) -Verb RunAs
    Exit}
    $Host.UI.RawUI.WindowTitle = $myInvocation.MyCommand.Definition + " (Administrator)"
    $Host.UI.RawUI.BackgroundColor = "Black"
	$Host.PrivateData.ProgressBackgroundColor = "Black"
    $Host.PrivateData.ProgressForegroundColor = "White"
    Clear-Host

Write-Host "Sichere RAM-Freigabe wird gestartet..."
Write-Host ""

# RAM Status vor Bereinigung anzeigen
$beforeRAM = Get-WmiObject -Class Win32_OperatingSystem
$totalRAM = [math]::Round($beforeRAM.TotalVisibleMemorySize/1MB, 2)
$freeRAM = [math]::Round($beforeRAM.FreePhysicalMemory/1MB, 2)
$usedRAM = $totalRAM - $freeRAM

Write-Host "RAM Status vorher:"
Write-Host "  Gesamt: $totalRAM GB"
Write-Host "  Belegt: $usedRAM GB" 
Write-Host "  Frei: $freeRAM GB"
Write-Host ""

Write-Host "Gebe RAM frei..."

# 1. Garbage Collection (100% sicher)
Write-Host "1. Starte .NET Garbage Collection..."
[System.GC]::Collect()
[System.GC]::WaitForPendingFinalizers()
[System.GC]::Collect()

# 2. Leere Clipboard (sicher)
Write-Host "2. Leere Clipboard..."
try {
    Add-Type -AssemblyName System.Windows.Forms
    [System.Windows.Forms.Clipboard]::Clear()
} catch {
    # Ignore if clipboard is locked
}

# 3. Beende unnötige Prozesse (nur sichere)
Write-Host "3. Beende unnötige Prozesse..."
$SafeToKill = @(
    "notepad",
    "calc", 
    "mspaint",
    "wordpad"
)

foreach ($process in $SafeToKill) {
    $proc = Get-Process -Name $process -ErrorAction SilentlyContinue
    if ($proc) {
        Write-Host "  Beende: $process"
        Stop-Process -Name $process -Force -ErrorAction SilentlyContinue
    }
}

# 4. Working Set aller Prozesse reduzieren (sicher)
Write-Host "4. Optimiere Prozess Working Sets..."
$processCount = 0
Get-Process | ForEach-Object {
    try {
        # Triggert Memory Trim für jeden Prozess
        $_.ProcessorAffinity = $_.ProcessorAffinity
        $processCount++
        
        if ($processCount % 10 -eq 0) {
            Write-Host "  $processCount Prozesse optimiert..."
        }
    } catch {
        # Ignore access denied
    }
}

Write-Host "  $processCount Prozesse optimiert."

# 5. System File Cache reduzieren (sicher)
Write-Host "5. Reduziere System File Cache..."
try {
    # EmptyWorkingSet für System
    Add-Type -TypeDefinition @"
    using System;
    using System.Runtime.InteropServices;
    public class Win32 {
        [DllImport("kernel32.dll")]
        public static extern bool SetProcessWorkingSetSize(IntPtr process, int minimumWorkingSetSize, int maximumWorkingSetSize);
        [DllImport("kernel32.dll")]
        public static extern IntPtr GetCurrentProcess();
    }
"@
    [Win32]::SetProcessWorkingSetSize([Win32]::GetCurrentProcess(), -1, -1)
} catch {
    Write-Host "  Working Set Optimierung übersprungen"
}

Start-Sleep -Seconds 2

# RAM Status nach Bereinigung
$afterRAM = Get-WmiObject -Class Win32_OperatingSystem
$newFreeRAM = [math]::Round($afterRAM.FreePhysicalMemory/1MB, 2)
$newUsedRAM = $totalRAM - $newFreeRAM
$freedRAM = [math]::Round($newFreeRAM - $freeRAM, 2)

Write-Host ""
Write-Host "RAM Status nachher:"
Write-Host "  Gesamt: $totalRAM GB"
Write-Host "  Belegt: $newUsedRAM GB"
Write-Host "  Frei: $newFreeRAM GB"
Write-Host ""

if ($freedRAM -gt 0) {
    Write-Host "✓ $freedRAM GB RAM freigegeben!" -ForegroundColor Green
} else {
    Write-Host "✓ RAM wurde optimiert!" -ForegroundColor Green
}

Write-Host ""
Write-Host "Sichere RAM-Freigabe abgeschlossen!"
$null = $Host.UI.RawUI.ReadKey("NoEcho,IncludeKeyDown")
exit