using System.Diagnostics;
using System.IO;

namespace TweakOS.Services;

public sealed class ScriptRunner
{
    public async Task<int> RunAsync(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
            throw new ArgumentException("Kein Tool-Pfad angegeben.", nameof(path));

        if (!Path.IsPathFullyQualified(path))
            path = Path.Combine(AppContext.BaseDirectory, path);

        path = Path.GetFullPath(path);

        if (!File.Exists(path))
            throw new FileNotFoundException(
                $"Tool nicht gefunden: {path}", path);

        var ext = Path.GetExtension(path).ToLowerInvariant();
        ProcessStartInfo psi;

        if (ext == ".ps1")
        {
            psi = new ProcessStartInfo
            {
                FileName = "powershell.exe",
                Arguments = $"-NoProfile -ExecutionPolicy Bypass -File \"{path}\"",
                UseShellExecute = true,
                Verb = "runas",
                WorkingDirectory = Path.GetDirectoryName(path) ?? AppContext.BaseDirectory
            };
        }
        else if (ext is ".bat" or ".cmd")
        {
            psi = new ProcessStartInfo
            {
                FileName = "cmd.exe",
                Arguments = $"/c \"\"{path}\"\"",
                UseShellExecute = true,
                Verb = "runas",
                WorkingDirectory = Path.GetDirectoryName(path) ?? AppContext.BaseDirectory
            };
        }
        else
        {
            psi = new ProcessStartInfo
            {
                FileName = path,
                UseShellExecute = true,
                Verb = "runas",
                WorkingDirectory = Path.GetDirectoryName(path) ?? AppContext.BaseDirectory
            };
        }

        using var process = Process.Start(psi)
            ?? throw new InvalidOperationException("Tool konnte nicht gestartet werden.");

        await process.WaitForExitAsync();
        return process.ExitCode;
    }
}
