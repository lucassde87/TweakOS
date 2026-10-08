using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Controls;
using System.Windows.Threading;
using TweakOS.Models;
using TweakOS.Services;

namespace TweakOS;

public partial class MainWindow : Window
{
    private readonly TweakCatalogService _catalogService = new();
    private readonly ReleaseService _releaseService = new();
    private readonly ScriptRunner _scriptRunner = new();
    private readonly ObservableCollection<TweakDefinition> _allTweaks = new();

    private PerformanceCounter? _cpuCounter;
    private PerformanceCounter? _ramCounter;
    private readonly DispatcherTimer _performanceTimer;
    private string _selectedCategory = "FPS Tweaks";

    public MainWindow()
    {
        InitializeComponent();

        try
        {
            _cpuCounter = new PerformanceCounter("Processor", "% Processor Time", "_Total");
            _ramCounter = new PerformanceCounter("Memory", "% Committed Bytes In Use");
            _cpuCounter.NextValue();
            _ramCounter.NextValue();
        }
        catch
        {
            _cpuCounter = null;
            _ramCounter = null;
        }

        _performanceTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
        _performanceTimer.Tick += PerformanceTimer_Tick;
        _performanceTimer.Start();

        _ = LoadTweaksAsync();
        ShowDashboard();
    }

    private async Task LoadTweaksAsync()
    {
        try
        {
            var catalog = await _catalogService.LoadAsync();
            _allTweaks.Clear();

            foreach (var tweak in catalog.Tweaks.Where(x => x.Enabled))
                _allTweaks.Add(tweak);

            RefreshTweakList();
        }
        catch (Exception ex)
        {
            TweakList.ItemsSource = null;
            MessageBox.Show("Der Tweak-Katalog konnte nicht geladen werden.\n\n" + ex.Message,
                "TweakOS", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    private void RefreshTweakList()
    {
        var search = SearchBox?.Text?.Trim() ?? "";

        var filtered = _allTweaks.Where(x =>
            x.Category.Equals(_selectedCategory, StringComparison.OrdinalIgnoreCase) &&
            (string.IsNullOrWhiteSpace(search) ||
             x.Name.Contains(search, StringComparison.OrdinalIgnoreCase) ||
             x.Description.Contains(search, StringComparison.OrdinalIgnoreCase)));

        TweakList.ItemsSource = new ObservableCollection<TweakDefinition>(filtered);
    }

    private void PerformanceTimer_Tick(object? sender, EventArgs e)
    {
        try
        {
            var cpu = _cpuCounter?.NextValue();
            var ram = _ramCounter?.NextValue();

            CpuValue.Text = cpu.HasValue ? $"{cpu.Value:0}%" : "--%";
            CpuPerformanceValue.Text = CpuValue.Text;
            RamValue.Text = ram.HasValue ? $"{ram.Value:0}%" : "--%";
            RamPerformanceValue.Text = RamValue.Text;
        }
        catch
        {
            CpuValue.Text = CpuPerformanceValue.Text = "--%";
            RamValue.Text = RamPerformanceValue.Text = "--%";
        }
    }


    private void TitleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ChangedButton == MouseButton.Left)
        {
            try { DragMove(); } catch { }
        }
    }

    private void SetActiveNav(Button active)
    {
        foreach (var nav in new[] { DashboardNav, TweaksNav, PerformanceNav, SettingsNav })
        {
            nav.Tag = "";
            nav.Foreground = (Brush)new BrushConverter().ConvertFromString("#8A8998");
        }

        active.Tag = "Active";
        active.Foreground = Brushes.White;
    }

    private void Dashboard_Click(object sender, RoutedEventArgs e) => ShowDashboard();

    private void Tweaks_Click(object sender, RoutedEventArgs e)
    {
        HideAllPanels();
        TweaksPanel.Visibility = Visibility.Visible;
        SetActiveNav(TweaksNav);
        PageTitle.Text = "TWEAK CENTER";
        PageSubtitle.Text = "Performance tools grouped by category.";
        RefreshTweakList();
    }

    private void Category_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button button)
        {
            var content = button.Content?.ToString() ?? string.Empty;
            _selectedCategory = content.Contains("FPS", StringComparison.OrdinalIgnoreCase) ? "FPS Tweaks"
                : content.Contains("Network", StringComparison.OrdinalIgnoreCase) ? "Network"
                : "Clearen";

            FpsCategory.Tag = _selectedCategory == "FPS Tweaks" ? "Active" : "";
            NetworkCategory.Tag = _selectedCategory == "Network" ? "Active" : "";
            ClearenCategory.Tag = _selectedCategory == "Clearen" ? "Active" : "";
            RefreshTweakList();
        }
    }

    private void Performance_Click(object sender, RoutedEventArgs e)
    {
        HideAllPanels();
        PerformancePanel.Visibility = Visibility.Visible;
        SetActiveNav(PerformanceNav);
        PageTitle.Text = "PERFORMANCE";
        PageSubtitle.Text = "Live system telemetry.";
    }

    private void Settings_Click(object sender, RoutedEventArgs e)
    {
        HideAllPanels();
        SettingsPanel.Visibility = Visibility.Visible;
        SetActiveNav(SettingsNav);
        PageTitle.Text = "SETTINGS";
        PageSubtitle.Text = "TweakOS application settings.";
    }

    private void ShowDashboard()
    {
        HideAllPanels();
        DashboardPanel.Visibility = Visibility.Visible;
        SetActiveNav(DashboardNav);
        PageTitle.Text = "PERFORMANCE CENTER";
        PageSubtitle.Text = "System optimized.";
    }

    private void HideAllPanels()
    {
        DashboardPanel.Visibility = Visibility.Collapsed;
        TweaksPanel.Visibility = Visibility.Collapsed;
        PerformancePanel.Visibility = Visibility.Collapsed;
        SettingsPanel.Visibility = Visibility.Collapsed;
    }

    private void SearchBox_TextChanged(object sender, TextChangedEventArgs e) => RefreshTweakList();

    private async void Run_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not Button button || button.Tag is not TweakDefinition tweak)
            return;

        try
        {
            button.IsEnabled = false;
            var exitCode = await _scriptRunner.RunAsync(tweak.Script);

            MessageBox.Show(
                exitCode == 0
                    ? $"{tweak.Name} wurde erfolgreich ausgeführt."
                    : $"{tweak.Name} wurde beendet (Exit-Code {exitCode}).",
                "TweakOS",
                MessageBoxButton.OK,
                exitCode == 0 ? MessageBoxImage.Information : MessageBoxImage.Warning);
        }
        catch (System.ComponentModel.Win32Exception)
        {
            MessageBox.Show("Der Start wurde abgebrochen oder die Administratorfreigabe wurde verweigert.",
                "TweakOS", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
        catch (Exception ex)
        {
            MessageBox.Show("Tweak konnte nicht ausgeführt werden:\n\n" + ex.Message,
                "TweakOS", MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally
        {
            button.IsEnabled = true;
        }
    }

    private async void Update_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var release = await _releaseService.GetLatestAsync();
            if (release == null || string.IsNullOrWhiteSpace(release.TagName))
            {
                MessageBox.Show("Kein Release gefunden.", "TweakOS");
                return;
            }

            if (!ReleaseService.IsNewer(release.TagName))
            {
                MessageBox.Show("Du verwendest bereits die aktuelle Version.", "TweakOS");
                return;
            }

            if (MessageBox.Show($"Eine neue Version ist verfügbar:\n\n{release.TagName}\n\nHerunterladen?",
                    "TweakOS Update", MessageBoxButton.YesNo, MessageBoxImage.Information) != MessageBoxResult.Yes)
                return;

            var downloaded = await _releaseService.DownloadUpdateAsync(release);
            var updater = ReleaseService.CreateUpdater(downloaded);
            ReleaseService.StartUpdater(updater);
            Application.Current.Shutdown();
        }
        catch (Exception ex)
        {
            MessageBox.Show("Update fehlgeschlagen:\n\n" + ex.Message,
                "TweakOS", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void Minimize_Click(object sender, RoutedEventArgs e) => WindowState = WindowState.Minimized;

    private void Maximize_Click(object sender, RoutedEventArgs e)
    {
        WindowState = WindowState == WindowState.Maximized ? WindowState.Normal : WindowState.Maximized;
    }

    private void Close_Click(object sender, RoutedEventArgs e) => Close();

    protected override void OnClosed(EventArgs e)
    {
        _performanceTimer.Stop();
        _cpuCounter?.Dispose();
        _ramCounter?.Dispose();
        base.OnClosed(e);
    }
}
