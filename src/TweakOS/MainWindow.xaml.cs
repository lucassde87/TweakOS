using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using TweakOS.Models;
using TweakOS.Services;

namespace TweakOS;

public partial class MainWindow : Window
{
    readonly TweakCatalogService catalog = new();
    readonly ScriptRunner runner = new();

    List<TweakDefinition> all = [];

    public MainWindow()
    {
        InitializeComponent();

        VersionText.Text =
            $"v{ReleaseService.CurrentVersion}";

        Loaded += async (_, _) =>
            await LoadAsync();
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
                                StringComparison.OrdinalIgnoreCase
                            )
                            ||
                            x.Category.Contains(
                                q,
                                StringComparison.OrdinalIgnoreCase
                            )
                            ||
                            x.Description.Contains(
                                q,
                                StringComparison.OrdinalIgnoreCase
                            )
                        )
                    )
                    .ToList();
    }

    void SearchBox_TextChanged(
        object s,
        System.Windows.Controls.TextChangedEventArgs e)
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
                    release.TagName
                )
            )
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
                        "TweakOS"
                    );
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
                    MessageBoxImage.Warning
                );
            }
        }
    }

    async void Update_Click(
        object s,
        RoutedEventArgs e)
    {
        /*
         * Falls bereits ein Update gefunden wurde,
         * direkt den Update-Vorgang starten.
         */

        if (
            UpdateButton.Tag is ReleaseInfo release
            &&
            ReleaseService.IsNewer(
                release.TagName
            )
        )
        {
            await InstallUpdate(release);
            return;
        }

        /*
         * Noch kein Update bekannt:
         * GitHub prüfen.
         */

        await CheckUpdate(true);

        /*
         * Nach der Prüfung erneut kontrollieren,
         * ob jetzt ein Update vorhanden ist.
         */

        if (
            UpdateButton.Tag is ReleaseInfo newRelease
            &&
            ReleaseService.IsNewer(
                newRelease.TagName
            )
        )
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
                MessageBoxImage.Information
            );

        if (
            result != MessageBoxResult.Yes
        )
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

            /*
             * ZIP herunterladen
             */

            var zipPath =
                await releaseService.DownloadUpdateAsync(
                    release
                );

            /*
             * Prüfen, ob die Datei wirklich existiert.
             */

            if (!File.Exists(zipPath))
            {
                throw new FileNotFoundException(
                    "Das Update-ZIP wurde nicht gefunden.",
                    zipPath
                );
            }

            /*
             * Updater-Script erstellen.
             */

            var updaterPath =
                ReleaseService.CreateUpdater(
                    zipPath
                );

            /*
             * Updater starten.
             */

            ReleaseService.StartUpdater(
                updaterPath
            );

            /*
             * Anwendung schließen.
             * Der externe Updater übernimmt jetzt.
             */

            Close();
        }
        catch (Exception ex)
        {
            UpdateButton.IsEnabled = true;

            UpdateButton.Content =
                "Update erneut versuchen";

            MessageBox.Show(
                "Das Update konnte nicht installiert werden.\n\n" +
                ex.Message,
                "TweakOS Update",
                MessageBoxButton.OK,
                MessageBoxImage.Error
            );
        }
    }

    async void Run_Click(
        object s,
        RoutedEventArgs e)
    {
        if (
            s is not FrameworkElement
            {
                Tag: TweakDefinition t
            }
        )
        {
            return;
        }

        var ok =
            MessageBox.Show(
                $"Tweak: {t.Name}\n" +
                $"Risiko: {t.Risk}\n\n" +
                $"{t.Description}\n\n" +
                "Jetzt ausführen?",
                "Tweak bestätigen",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning
            );

        if (
            ok != MessageBoxResult.Yes
        )
        {
            return;
        }

        var path =
            Path.Combine(
                AppContext.BaseDirectory,
                t.Script.Replace(
                    "/",
                    Path.DirectorySeparatorChar.ToString()
                )
            );

        if (!File.Exists(path))
        {
            path =
                Path.GetFullPath(
                    Path.Combine(
                        AppContext.BaseDirectory,
                        "..",
                        "..",
                        "..",
                        "..",
                        t.Script
                    )
                );
        }

        if (!File.Exists(path))
        {
            MessageBox.Show(
                $"Script nicht gefunden:\n{path}",
                "Fehler",
                MessageBoxButton.OK,
                MessageBoxImage.Error
            );

            return;
        }

        try
        {
            var code =
                await runner.RunAsync(path);

            MessageBox.Show(
                $"Tweak beendet. Exit-Code: {code}",
                "Fertig"
            );
        }
        catch (
            System.ComponentModel.Win32Exception
        )
        {
            MessageBox.Show(
                "Ausführung abgebrochen oder benötigt " +
                "Administratorrechte.",
                "Nicht ausgeführt"
            );
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                ex.Message,
                "Fehler",
                MessageBoxButton.OK,
                MessageBoxImage.Error
            );
        }
    }
}
