using System.Diagnostics;
using System.Net.Http;
using System.Text.Json;

namespace TweakOS.Services;

public sealed class ReleaseInfo
{
    public string TagName { get; set; } = "";
    public string Name { get; set; } = "";
    public string HtmlUrl { get; set; } = "";
    public string Body { get; set; } = "";
    public string DownloadUrl { get; set; } = "";
}

public sealed class ReleaseService
{
    public const string CurrentVersion = "5.0.2";

    private const string ApiUrl =
        "https://api.github.com/repos/lucassde87/TweakOS/releases/latest";

    private const string UpdateFileName =
        "TweakOS-update.exe";

    public async Task<ReleaseInfo?> GetLatestAsync()
    {
        using var client = new HttpClient
        {
            Timeout = TimeSpan.FromSeconds(15)
        };

        client.DefaultRequestHeaders.UserAgent.ParseAdd("TweakOS/5.0");

        using var response = await client.GetAsync(ApiUrl);

        response.EnsureSuccessStatusCode();

        using var stream = await response.Content.ReadAsStreamAsync();

        using var document = await JsonDocument.ParseAsync(stream);

        var root = document.RootElement;

        var release = new ReleaseInfo
        {
            TagName =
                root.TryGetProperty("tag_name", out var tag)
                    ? tag.GetString() ?? ""
                    : "",

            Name =
                root.TryGetProperty("name", out var name)
                    ? name.GetString() ?? ""
                    : "",

            HtmlUrl =
                root.TryGetProperty("html_url", out var html)
                    ? html.GetString() ?? ""
                    : "",

            Body =
                root.TryGetProperty("body", out var body)
                    ? body.GetString() ?? ""
                    : ""
        };

        if (root.TryGetProperty("assets", out var assets))
        {
            foreach (var asset in assets.EnumerateArray())
            {
                var fileName =
                    asset.TryGetProperty("name", out var assetName)
                        ? assetName.GetString() ?? ""
                        : "";

                if (fileName.Equals(
                    "TweakOS.exe",
                    StringComparison.OrdinalIgnoreCase))
                {
                    release.DownloadUrl =
                        asset.TryGetProperty(
                            "browser_download_url",
                            out var download)
                                ? download.GetString() ?? ""
                                : "";

                    break;
                }
            }
        }

        return release;
    }

    public static bool IsNewer(string tag)
    {
        return
            Version.TryParse(
                tag.Trim().TrimStart('v', 'V'),
                out var remoteVersion)
            &&
            Version.TryParse(
                CurrentVersion,
                out var currentVersion)
            &&
            remoteVersion > currentVersion;
    }

    public static void OpenUrl(string url)
    {
        if (!string.IsNullOrWhiteSpace(url))
        {
            Process.Start(
                new ProcessStartInfo
                {
                    FileName = url,
                    UseShellExecute = true
                });
        }
    }

    public async Task<string> DownloadUpdateAsync(
        ReleaseInfo release)
    {
        if (string.IsNullOrWhiteSpace(release.DownloadUrl))
        {
            throw new InvalidOperationException(
                "Für dieses Release wurde keine TweakOS.exe gefunden.");
        }

        var tempDirectory =
            Path.Combine(
                Path.GetTempPath(),
                "TweakOS",
                "Update");

        Directory.CreateDirectory(tempDirectory);

        var exePath =
            Path.Combine(
                tempDirectory,
                UpdateFileName);

        using var client = new HttpClient();

        client.Timeout = TimeSpan.FromMinutes(5);

        client.DefaultRequestHeaders.UserAgent.ParseAdd(
            "TweakOS/5.0");

        using var response =
            await client.GetAsync(
                release.DownloadUrl,
                HttpCompletionOption.ResponseHeadersRead);

        response.EnsureSuccessStatusCode();

        await using var input =
            await response.Content.ReadAsStreamAsync();

        await using var output =
            new FileStream(
                exePath,
                FileMode.Create,
                FileAccess.Write,
                FileShare.None);

        await input.CopyToAsync(output);

        return exePath;
    }

    public static string CreateUpdater(
        string downloadedExePath)
    {
        var tempDirectory =
            Path.GetDirectoryName(downloadedExePath)
            ?? Path.GetTempPath();

        var updaterPath =
            Path.Combine(
                tempDirectory,
                "TweakOS-Updater.cmd");

        var currentDirectory =
            AppContext.BaseDirectory.TrimEnd(
                Path.DirectorySeparatorChar,
                Path.AltDirectorySeparatorChar);

        var currentApp =
            Path.Combine(
                currentDirectory,
                "TweakOS.exe");

        var source =
            EscapePowerShell(downloadedExePath);

        var target =
            EscapePowerShell(currentApp);

        var script = $"""
@echo off
setlocal

timeout /t 2 /nobreak >nul

powershell.exe -NoProfile -ExecutionPolicy Bypass -Command ^
"$source='{source}'; ^
$target='{target}'; ^
Start-Sleep -Seconds 1; ^
for($i=0;$i -lt 20;$i++) {{ ^
    try {{ ^
        if(Test-Path $target) {{ ^
            Remove-Item $target -Force -ErrorAction Stop ^
        }} ^
        Copy-Item $source $target -Force -ErrorAction Stop; ^
        break ^
    }} catch {{ ^
        Start-Sleep -Milliseconds 500 ^
    }} ^
}}; ^
Remove-Item $source -Force -ErrorAction SilentlyContinue; ^
Start-Process '{target}'"

del "%~f0"
""";

        File.WriteAllText(
            updaterPath,
            script);

        return updaterPath;
    }

    private static string EscapePowerShell(
        string value)
    {
        return value.Replace("'", "''");
    }

    public static void StartUpdater(
        string updaterPath)
    {
        Process.Start(
            new ProcessStartInfo
            {
                FileName = updaterPath,
                UseShellExecute = true,
                CreateNoWindow = true,
                WindowStyle = ProcessWindowStyle.Hidden
            });
    }
}
