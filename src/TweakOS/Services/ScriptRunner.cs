using System.Diagnostics;
using System.IO;

namespace TweakOS.Services;

public sealed class ScriptRunner
{
    public async Task<int> RunAsync(string path)
    {
        var fullPath = Path.IsPathRooted(path)
            ? path
            : Path.Combine(AppContext.BaseDirectory, path);

        fullPath = Path.GetFullPath(fullPath);

        if (!File.Exists(fullPath))
            throw new FileNotFoundException(
                $"Tool nicht gefunden: {fullPath}",
                fullPath);

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
                WorkingDirectory =
                    Path.GetDirectoryName(fullPath) ?? AppContext.BaseDirectory
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
                WorkingDirectory =
                    Path.GetDirectoryName(fullPath) ?? AppContext.BaseDirectory
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
                WorkingDirectory =
                    Path.GetDirectoryName(fullPath) ?? AppContext.BaseDirectory
            };
        }
        else
        {
            psi = new ProcessStartInfo
            {
                FileName = fullPath,
                UseShellExecute = true,
                Verb = "runas",
                WorkingDirectory =
                    Path.GetDirectoryName(fullPath) ?? AppContext.BaseDirectory
            };
        }

        using var process = Process.Start(psi)
            ?? throw new InvalidOperationException(
                "Tool konnte nicht gestartet werden.");

        await process.WaitForExitAsync();

        return process.ExitCode;
    }
}
