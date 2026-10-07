using System.Diagnostics;
namespace TweakOS.Services;
public sealed class ScriptRunner {
 public async Task<int> RunAsync(string path){
  var ext=Path.GetExtension(path).ToLowerInvariant();
  var psi=ext==".ps1"?new ProcessStartInfo{FileName="powershell.exe",Arguments=$"-NoProfile -ExecutionPolicy Bypass -File \"{path}\"",UseShellExecute=true,Verb="runas",WorkingDirectory=Path.GetDirectoryName(path)??AppContext.BaseDirectory}
   :new ProcessStartInfo{FileName="cmd.exe",Arguments=$"/c \"{path}\"",UseShellExecute=true,Verb="runas",WorkingDirectory=Path.GetDirectoryName(path)??AppContext.BaseDirectory};
  using var p=Process.Start(psi)??throw new InvalidOperationException("Script konnte nicht gestartet werden."); await p.WaitForExitAsync(); return p.ExitCode;
 }
}