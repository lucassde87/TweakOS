using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using TweakOS.Models;
using TweakOS.Services;

namespace TweakOS;

public partial class MainWindow : Window
{
readonly TweakCatalogService catalog = new();
readonly ScriptRunner runner = new();

```
List<TweakDefinition> all = [];

readonly PerformanceCounter cpuCounter =
    new("Processor", "% Processor Time", "_Total");

readonly PerformanceCounter ramCounter =
    new("Memory", "% Committed Bytes In Use");

readonly DispatcherTimer performanceTimer = new();

public MainWindow()
{
    InitializeComponent();

    VersionText.Text =
        $"v{ReleaseService.CurrentVersion}";

    SettingsVersion.Text =
        $"v{ReleaseService.CurrentVersion}";

    performanceTimer.Interval =
        TimeSpan.FromSeconds(1);

    performanceTimer.Tick +=
        UpdatePerformance;

    Loaded += async (_, _) =>
    {
        performanceTimer.Start();
        await LoadAsync();
    };

    Closed += (_, _) =>
    {
        performanceTimer.Stop();

        cpuCounter.Dispose();
        ramCounter.Dispose();
    };
}

async Task LoadAsync()
{
    try
    {
        var c =
            await catalog.LoadAsync();

        all = c.Tweaks;

        Filter();

        CatalogStatus.Text =
            $"Katalog v{c.CatalogVersion} • " +
            $"{c.UpdatedAt} • " +
            $"{c.Tweaks.Count} Tweaks";

        await CheckUpdate(false);
    }
    catch (Exception e)
    {
        CatalogStatus.Text =
            "Katalog konnte nicht geladen werden: " +
            e.Message;
    }
}

void UpdatePerformance(
    object? sender,
    EventArgs e)
{
    try
    {
        var cpu =
            Math.Clamp(
                cpuCounter.NextValue(),
                0,
                100);

        var ram =
            Math.Clamp(
                ramCounter.NextValue(),
                0,
                100);

        var cpuText =
            $"{cpu:0}%";

        var ramText =
            $"{ram:0}%";

        CpuText.Text = cpuText;
        RamText.Text = ramText;

        PerformanceCpu.Text = cpuText;
        PerformanceRam.Text = ramText;
    }
    catch
    {
        CpuText.Text = "--%";
        RamText.Text = "--%";

        PerformanceCpu.Text = "--%";
        PerformanceRam.Text = "--%";
    }
}

void ShowPanel(
    Grid panel,
    string title,
    string subtitle)
{
    DashboardPanel.Visibility =
        Visibility.Collapsed;

    TweaksPanel.Visibility =
        Visibility.Collapsed;

    PerformancePanel.Visibility =
        Visibility.Collapsed;

    SettingsPanel.Visibility =
        Visibility.Collapsed;

    panel.Visibility =
        Visibility.Visible;

    PageTitle.Text =
        title;

    PageSubtitle.Text =
        subtitle;
}

void Dashboard_Click(
    object sender,
    RoutedEventArgs e)
{
    ShowPanel(
        DashboardPanel,
        "Performance Center",
        "System optimized.");
}

void Tweaks_Click(
    object sender,
    RoutedEventArgs e)
{
    ShowPanel(
        TweaksPanel,
        "Tweaks",
        "Windows für Gaming und Performance optimieren.");
}

void Performance_Click(
    object sender,
    RoutedEventArgs e)
{
    ShowPanel(
        PerformancePanel,
        "Performance",
        "Live-Systemübersicht.");
}

void Settings_Click(
    object sender,
    RoutedEventArgs e)
{
    ShowPanel(
        SettingsPanel,
        "Settings",
        "TweakOS Einstellungen.");
}

void Filter()
{
    var q =
        SearchBox?.Text?.Trim() ?? "";

    TweakList.ItemsSource =
        string.IsNullOrWhiteSpace(q)
            ? all
                .Where(x => x.Enabled)
                .ToList()
            : all
                .Where(x =>
                    x.Enabled &&
                    (
                        x.Name.Contains(
                            q,
                            StringComparison.OrdinalIgnoreCase)
                        ||
                        x.Category.Contains(
                            q,
                            StringComparison.OrdinalIgnoreCase)
                        ||
                        x.Description.Contains(
                            q,
                            StringComparison.OrdinalIgnoreCase)
                    ))
                .ToList();
}

void SearchBox_TextChanged(
    object sender,
    TextChangedEventArgs e)
{
    Filter();
}

async Task CheckUpdate(bool show)
{
    try
    {
        var releaseService =
            new ReleaseService();

        var release =
            await releaseService.GetLatestAsync();

        if (
            release != null &&
            ReleaseService.IsNewer(
                release.TagName))
        {
            UpdateButton.Content =
                $"Update verfügbar: {release.TagName}";

            UpdateButton.Tag =
                release;
        }
        else
        {
            UpdateButton.Content =
                "Nach Updates suchen";

            UpdateButton.Tag =
                null;

            if (show)
            {
                MessageBox.Show(
                    $"Du verwendest bereits die aktuelle Version " +
                    $"v{ReleaseService.CurrentVersion}.",
                    "TweakOS");
            }
        }
    }
    catch
    {
        if (show)
        {
            MessageBox.Show(
                "Update-Prüfung war nicht möglich.\n\n" +
                "Prüfe deine Internetverbindung.",
                "TweakOS",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
        }
    }
}

async void Update_Click(
    object sender,
    RoutedEventArgs e)
{
    if (
        UpdateButton.Tag is ReleaseInfo release &&
        ReleaseService.IsNewer(
            release.TagName))
    {
        await InstallUpdate(release);
        return;
    }

    await CheckUpdate(true);

    if (
        UpdateButton.Tag is ReleaseInfo newRelease &&
        ReleaseService.IsNewer(
            newRelease.TagName))
    {
        await InstallUpdate(newRelease);
    }
}

async Task InstallUpdate(
    ReleaseInfo release)
{
    var version =
        release.TagName;

    var result =
        MessageBox.Show(
            $"Eine neue TweakOS-Version ist verfügbar.\n\n" +
            $"Aktuell: v{ReleaseService.CurrentVersion}\n" +
            $"Neu: {version}\n\n" +
            "TweakOS wird das Update herunterladen " +
            "und anschließend neu starten.\n\n" +
            "Jetzt aktualisieren?",
            "TweakOS Update",
            MessageBoxButton.YesNo,
            MessageBoxImage.Information);

    if (
        result != MessageBoxResult.Yes)
    {
        return;
    }

    try
    {
        UpdateButton.IsEnabled = false;

        UpdateButton.Content =
            "Update wird heruntergeladen...";

        var releaseService =
            new ReleaseService();

        var exePath =
            await releaseService.DownloadUpdateAsync(
```
