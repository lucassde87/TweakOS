using System.Diagnostics;
using System.IO;

namespace TweakOS.Services;

public sealed class ScriptRunner
{
    public async Task<int> RunAsync(string path)
    {
        var fullPath = ResolveTweakPath(path);

        if (!File.Exists(fullPath))
        {
            throw new FileNotFoundException(
                $"Tool nicht gefunden:\n\n{fullPath}",
                fullPath);
        }

        var ext = Path.GetExtension(fullPath).ToLowerInvariant();

        ProcessStartInfo psi;

        if (ext == ".ps1")
        {
            psi = new ProcessStartInfo
            {
                FileName = "powershell.exe",
                Arguments = $"-NoProfile -ExecutionPolicy Bypass -File \"{fullPath}\"",
                UseShellExecute = true,
                Verb = "runas",
                WorkingDirectory = Path.GetDirectoryName(fullPath)!
            };
        }
        else if (ext is ".bat" or ".cmd")
        {
            psi = new ProcessStartInfo
            {
                FileName = "cmd.exe",
                Arguments = $"/c \"{fullPath}\"",
                UseShellExecute = true,
                Verb = "runas",
                WorkingDirectory = Path.GetDirectoryName(fullPath)!
            };
        }
        else if (ext == ".reg")
        {
            psi = new ProcessStartInfo
            {
                FileName = "regedit.exe",
                Arguments = $"\"{fullPath}\"",
                UseShellExecute = true,
                Verb = "runas",
                WorkingDirectory = Path.GetDirectoryName(fullPath)!
            };
        }
        else
        {
            psi = new ProcessStartInfo
            {
                FileName = fullPath,
                UseShellExecute = true,
                Verb = "runas",
                WorkingDirectory = Path.GetDirectoryName(fullPath)!
            };
        }

        using var process = Process.Start(psi)
            ?? throw new InvalidOperationException(
                "Tool konnte nicht gestartet werden.");

        await process.WaitForExitAsync();

        return process.ExitCode;
    }

    private static string ResolveTweakPath(string path)
    {
        // Absoluter Pfad -> direkt verwenden
        if (Path.IsPathRooted(path))
            return Path.GetFullPath(path);

        var relativePath = path.Replace('/', Path.DirectorySeparatorChar);

        // 1. Neben der TweakOS-EXE suchen
        var appPath = Path.GetFullPath(
            Path.Combine(AppContext.BaseDirectory, relativePath));

        if (File.Exists(appPath))
            return appPath;

        // 2. Auf dem Desktop suchen
        var desktop = Environment.GetFolderPath(
            Environment.SpecialFolder.DesktopDirectory);

        // catalog path: tweaks/FPS Tweaks/...
        // Desktop path: FPS Tweaks/...
        if (relativePath.StartsWith(
                "tweaks" + Path.DirectorySeparatorChar,
                StringComparison.OrdinalIgnoreCase))
        {
            relativePath = relativePath[
                ("tweaks" + Path.DirectorySeparatorChar).Length..];
        }

        var desktopPath = Path.GetFullPath(
            Path.Combine(desktop, relativePath));

        if (File.Exists(desktopPath))
            return desktopPath;

        // 3. Downloads als zusätzliche Möglichkeit
        var downloads = Path.Combine(
            Environment.GetFolderPath(
                Environment.SpecialFolder.UserProfile),
            "Downloads");

        var downloadsPath = Path.GetFullPath(
            Path.Combine(downloads, relativePath));

        if (File.Exists(downloadsPath))
            return downloadsPath;

        // Wenn nichts gefunden wurde, Desktop-Pfad zurückgeben,
        // damit die Fehlermeldung verständlich bleibt.
        return desktopPath;
    }
}
