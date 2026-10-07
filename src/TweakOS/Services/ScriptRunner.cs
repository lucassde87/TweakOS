using System.Diagnostics;
using System.IO;

namespace TweakOS.Services;

public sealed class ScriptRunner
{
    public async Task<int> RunAsync(string path)
    {
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
        else
        {
            psi = new ProcessStartInfo
            {
                FileName = "cmd.exe",
                Arguments = $"/c \"{path}\"",
                UseShellExecute = true,
                Verb = "runas",
                WorkingDirectory = Path.GetDirectoryName(path) ?? AppContext.BaseDirectory
            };
        }

        using var process = Process.Start(psi)
            ?? throw new InvalidOperationException("Script konnte nicht gestartet werden.");

        await process.WaitForExitAsync();

        return process.ExitCode;
    }
}
