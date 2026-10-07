using System.Diagnostics;
using System.IO;
using System.IO.Compression;
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
    public const string CurrentVersion = "5.0.0";

    private const string ApiUrl =
        "https://api.github.com/repos/lucassde87/TweakOS/releases/latest";

    public async Task<ReleaseInfo?> GetLatestAsync()
    {
        using var client = new HttpClient
        {
            Timeout = TimeSpan.FromSeconds(15)
        };

        client.DefaultRequestHeaders.UserAgent.ParseAdd(
            "TweakOS/5.0"
        );

        using var document =
            JsonDocument.Parse(
                await client.GetStringAsync(ApiUrl)
            );

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

                if (
                    fileName.Equals(
                        "TweakOS-win-x64.zip",
                        StringComparison.OrdinalIgnoreCase
                    )
                )
                {
                    release.DownloadUrl =
                        asset.GetProperty(
                            "browser_download_url"
                        ).GetString() ?? "";

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
                out var remoteVersion
            )
            &&
            Version.TryParse(
                CurrentVersion,
                out var currentVersion
            )
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
                }
            );
        }
    }

    public async Task<string> DownloadUpdateAsync(
        ReleaseInfo release)
    {
        if (string.IsNullOrWhiteSpace(release.DownloadUrl))
        {
            throw new InvalidOperationException(
                "Für dieses Release wurde kein TweakOS-Download gefunden."
            );
        }

        var tempDirectory =
            Path.Combine(
                Path.GetTempPath(),
                "TweakOS",
                "Update"
            );

        Directory.CreateDirectory(tempDirectory);

        var zipPath =
            Path.Combine(
                tempDirectory,
                "TweakOS-update.zip"
            );

        using var client = new HttpClient();

        client.DefaultRequestHeaders.UserAgent.ParseAdd(
            "TweakOS/5.0"
        );

        var data =
            await client.GetByteArrayAsync(
                release.DownloadUrl
            );

        await File.WriteAllBytesAsync(
            zipPath,
            data
        );

        return zipPath;
    }

    public static string CreateUpdater(
        string zipPath)
    {
        var tempDirectory =
            Path.GetDirectoryName(zipPath)
            ?? Path.GetTempPath();

        var updaterPath =
            Path.Combine(
                tempDirectory,
                "TweakOS-Updater.cmd"
            );

        var currentDirectory =
            AppContext.BaseDirectory.TrimEnd(
                Path.DirectorySeparatorChar
            );

        var zip =
            zipPath.Replace(
                "\"",
                "\"\""
            );

        var app =
            Path.Combine(
                currentDirectory,
                "TweakOS.exe"
            );

        var script = $"""
@echo off

timeout /t 2 /nobreak >nul

powershell.exe -NoProfile -ExecutionPolicy Bypass -Command ^
"$zip='{zip}'; ^
$target='{currentDirectory}'; ^
$temp=Join-Path $env:TEMP 'TweakOS_Update'; ^
Remove-Item $temp -Recurse -Force -ErrorAction SilentlyContinue; ^
New-Item -ItemType Directory -Path $temp -Force | Out-Null; ^
Expand-Archive -Path $zip -DestinationPath $temp -Force; ^
Copy-Item (Join-Path $temp '*') $target -Recurse -Force; ^
Remove-Item $zip -Force -ErrorAction SilentlyContinue; ^
Remove-Item $temp -Recurse -Force -ErrorAction SilentlyContinue; ^
Start-Process '{app}'"

del "%~f0"
""";

        File.WriteAllText(
            updaterPath,
            script
        );

        return updaterPath;
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
            }
        );
    }
}
