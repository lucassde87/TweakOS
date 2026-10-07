```csharp
using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using TweakOS.Models;
using TweakOS.Services;

namespace TweakOS
{
    public partial class MainWindow : Window
    {
        private readonly TweakCatalogService _catalogService;
        private readonly ReleaseService _releaseService;

        private readonly ObservableCollection<TweakDefinition> _allTweaks = new();

        private PerformanceCounter? _cpuCounter;
        private PerformanceCounter? _ramCounter;

        private readonly DispatcherTimer _performanceTimer;

        public MainWindow()
        {
            InitializeComponent();

            _catalogService = new TweakCatalogService();
            _releaseService = new ReleaseService();

            try
            {
                _cpuCounter = new PerformanceCounter(
                    "Processor",
                    "% Processor Time",
                    "_Total");

                _ramCounter = new PerformanceCounter(
                    "Memory",
                    "% Committed Bytes In Use");

                _cpuCounter.NextValue();
                _ramCounter.NextValue();
            }
            catch
            {
                _cpuCounter = null;
                _ramCounter = null;
            }

            _performanceTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(1)
            };

            _performanceTimer.Tick += PerformanceTimer_Tick;
            _performanceTimer.Start();

            _ = LoadTweaksAsync();

            ShowDashboard();
        }

        private async System.Threading.Tasks.Task LoadTweaksAsync()
        {
            try
            {
                var catalog = await _catalogService.LoadAsync();

                _allTweaks.Clear();

                foreach (var tweak in catalog.Tweaks)
                {
                    _allTweaks.Add(tweak);
                }

                RefreshTweakList();
            }
            catch (Exception ex)
            {
                TweakList.ItemsSource = null;

                MessageBox.Show(
                    "Der Tweak-Katalog konnte nicht geladen werden.\n\n" +
                    ex.Message,
                    "TweakOS",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
            }
        }

        private void RefreshTweakList()
        {
            var search = SearchBox?.Text?.Trim() ?? "";

            var filtered = string.IsNullOrWhiteSpace(search)
                ? _allTweaks
                : new ObservableCollection<TweakDefinition>(
                    _allTweaks.Where(x =>
                        x.Name.Contains(
                            search,
                            StringComparison.OrdinalIgnoreCase) ||
                        x.Description.Contains(
                            search,
                            StringComparison.OrdinalIgnoreCase)));

            TweakList.ItemsSource = filtered;
        }

        private void PerformanceTimer_Tick(
            object? sender,
            EventArgs e)
        {
            try
            {
                if (_cpuCounter != null)
                {
                    var cpu = _cpuCounter.NextValue();

                    CpuValue.Text = $"{cpu:0}%";
                    CpuPerformanceValue.Text = $"{cpu:0}%";
                }

                if (_ramCounter != null)
                {
                    var ram = _ramCounter.NextValue();

                    RamValue.Text = $"{ram:0}%";
                    RamPerformanceValue.Text = $"{ram:0}%";
                }
            }
            catch
            {
                CpuValue.Text = "--%";
                RamValue.Text = "--%";

                CpuPerformanceValue.Text = "--%";
                RamPerformanceValue.Text = "--%";
            }
        }

        private void Dashboard_Click(
            object sender,
            RoutedEventArgs e)
        {
            ShowDashboard();
        }

        private void Tweaks_Click(
            object sender,
            RoutedEventArgs e)
        {
            HideAllPanels();
            TweaksPanel.Visibility = Visibility.Visible;
        }

        private void Performance_Click(
            object sender,
            RoutedEventArgs e)
        {
            HideAllPanels();
            PerformancePanel.Visibility = Visibility.Visible;
        }

        private void Settings_Click(
            object sender,
            RoutedEventArgs e)
        {
            HideAllPanels();
            SettingsPanel.Visibility = Visibility.Visible;
        }

        private void ShowDashboard()
        {
            HideAllPanels();
            DashboardPanel.Visibility = Visibility.Visible;
        }

        private void HideAllPanels()
        {
            DashboardPanel.Visibility = Visibility.Collapsed;
            TweaksPanel.Visibility = Visibility.Collapsed;
            PerformancePanel.Visibility = Visibility.Collapsed;
            SettingsPanel.Visibility = Visibility.Collapsed;
        }

        private void SearchBox_TextChanged(
            object sender,
            TextChangedEventArgs e)
        {
            RefreshTweakList();
        }

        private async void Update_Click(
            object sender,
            RoutedEventArgs e)
        {
            try
            {
                var release =
                    await _releaseService.GetLatestAsync();

                if (release == null ||
                    string.IsNullOrWhiteSpace(release.TagName))
                {
                    MessageBox.Show(
                        "Kein Release gefunden.",
                        "TweakOS",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information);

                    return;
                }

                if (!ReleaseService.IsNewer(
                    release.TagName))
                {
                    MessageBox.Show(
                        "Du verwendest bereits die aktuelle Version.",
                        "TweakOS",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information);

                    return;
                }

                var result = MessageBox.Show(
                    $"Eine neue Version ist verfügbar:\n\n" +
                    $"{release.TagName}\n\n" +
                    "Möchtest du sie herunterladen?",
                    "TweakOS Update",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Information);

                if (result != MessageBoxResult.Yes)
                    return;

                var downloaded =
                    await _releaseService.DownloadUpdateAsync(
                        release);

                var updater =
                    ReleaseService.CreateUpdater(
                        downloaded);

                ReleaseService.StartUpdater(
                    updater);

                Application.Current.Shutdown();
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    "Update fehlgeschlagen:\n\n" +
                    ex.Message,
                    "TweakOS",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        private void Run_Click(
            object sender,
            RoutedEventArgs e)
        {
            if (sender is not Button button)
                return;

            if (button.Tag is not TweakDefinition tweak)
                return;

            try
            {
                if (string.IsNullOrWhiteSpace(
                    tweak.Script))
                {
                    MessageBox.Show(
                        "Für diesen Tweak wurde kein Script hinterlegt.",
                        "TweakOS",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information);

                    return;
                }

                var process = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = "powershell.exe",
                        Arguments =
                            "-NoProfile " +
                            "-ExecutionPolicy Bypass " +
                            "-Command " +
                            $"\"{tweak.Script}\"",

                        UseShellExecute = true,
                        Verb = "runas"
                    }
                };

                process.Start();

                MessageBox.Show(
                    $"{tweak.Name} wurde ausgeführt.",
                    "TweakOS",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    "Tweak konnte nicht ausgeführt werden:\n\n" +
                    ex.Message,
                    "TweakOS",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        protected override void OnClosed(
            EventArgs e)
        {
            _performanceTimer.Stop();

            _cpuCounter?.Dispose();
            _ramCounter?.Dispose();

            base.OnClosed(e);
        }
    }
}
```
