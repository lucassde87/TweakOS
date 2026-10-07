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
    public const string CurrentVersion = "5.0.0";

    private const string ApiUrl =
        "https://api.github.com/repos/lucassde87/TweakOS/releases/latest";

    public async Task<ReleaseInfo?> GetLatestAsync()
    {
        using var client = new HttpClient
        {
            Timeout = TimeSpan.FromSeconds(10)
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
                    fileName.EndsWith(
                        ".zip",
                        StringComparison.OrdinalIgnoreCase
                    )
                    ||
                    fileName.EndsWith(
                        ".exe",
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
}
