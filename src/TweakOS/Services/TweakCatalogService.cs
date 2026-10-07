using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using TweakOS.Models;

namespace TweakOS.Services;

public sealed class TweakCatalog
{
    public int SchemaVersion { get; set; }

    public int CatalogVersion { get; set; }

    public string UpdatedAt { get; set; } = "";

    public List<string> ReleaseNotes { get; set; } = [];

    public List<TweakDefinition> Tweaks { get; set; } = [];
}

public sealed class TweakCatalogService
{
    private const string RemoteUrl =
        "https://raw.githubusercontent.com/lucassde87/TweakOS/main/catalog/tweaks.json";

    private readonly JsonSerializerOptions options = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public async Task<TweakCatalog> LoadAsync()
    {
        using var client = new HttpClient
        {
            Timeout = TimeSpan.FromSeconds(10)
        };

        try
        {
            var json = await client.GetStringAsync(RemoteUrl);

            var remoteCatalog =
                JsonSerializer.Deserialize<TweakCatalog>(json, options);

            if (remoteCatalog != null)
                return remoteCatalog;
        }
        catch
        {
            // Wenn der Online-Katalog nicht erreichbar ist,
            // wird automatisch der lokale Katalog verwendet.
        }

        var local =
            Path.Combine(
                AppContext.BaseDirectory,
                "catalog",
                "tweaks.json"
            );

        if (!File.Exists(local))
        {
            local = Path.GetFullPath(
                Path.Combine(
                    AppContext.BaseDirectory,
                    "..",
                    "..",
                    "..",
                    "..",
                    "catalog",
                    "tweaks.json"
                )
            );
        }

        if (!File.Exists(local))
        {
            throw new FileNotFoundException(
                "Lokaler Tweak-Katalog nicht gefunden.",
                local
            );
        }

        var localJson = await File.ReadAllTextAsync(local);

        return JsonSerializer.Deserialize<TweakCatalog>(
            localJson,
            options
        ) ?? throw new InvalidDataException(
            "Tweak-Katalog ist ungültig."
        );
    }
}
