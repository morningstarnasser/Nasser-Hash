using System.Collections.ObjectModel;
using System.IO;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HashcatGUI.Helpers;
using HashcatGUI.Models;

namespace HashcatGUI.ViewModels;

public partial class SettingsViewModel : ViewModelBase
{
    [ObservableProperty]
    private string _hashcatPath = string.Empty;

    [ObservableProperty]
    private string _defaultWordlistPath = string.Empty;

    [ObservableProperty]
    private string _defaultRulesPath = string.Empty;

    [ObservableProperty]
    private string _defaultMasksPath = string.Empty;

    [ObservableProperty]
    private string _defaultCharsetsPath = string.Empty;

    [ObservableProperty]
    private string _defaultOutputPath = string.Empty;

    [ObservableProperty]
    private string _potfilePath = string.Empty;

    [ObservableProperty]
    private int _defaultWorkloadProfile = 2;

    [ObservableProperty]
    private bool _useOptimizedKernels = true;

    [ObservableProperty]
    private int _defaultOutputFormat = 2;

    [ObservableProperty]
    private int _statusUpdateInterval = 5;

    [ObservableProperty]
    private int _temperatureLimit = 90;

    [ObservableProperty]
    private bool _autoSaveSession = true;

    [ObservableProperty]
    private bool _showNotifications = true;

    [ObservableProperty]
    private bool _darkMode = true;

    [ObservableProperty]
    private string _hashcatVersion = "Unknown";

    [ObservableProperty]
    private bool _hashcatFound;

    public ObservableCollection<WorkloadProfile> WorkloadProfiles { get; } = new();
    public ObservableCollection<OutputFormat> OutputFormats { get; } = new();
    public ObservableCollection<DeviceInfo> Devices { get; } = new();

    public SettingsViewModel()
    {
        foreach (var profile in WorkloadProfile.GetAll())
            WorkloadProfiles.Add(profile);

        foreach (var format in OutputFormat.GetAll())
            OutputFormats.Add(format);

        LoadSettings();
        _ = LoadDevicesAsync();
        _ = CheckHashcatVersionAsync();
    }

    private void LoadSettings()
    {
        var settings = App.Settings.Settings;

        HashcatPath = settings.HashcatPath;
        DefaultWordlistPath = settings.DefaultWordlistPath;
        DefaultRulesPath = settings.DefaultRulesPath;
        DefaultMasksPath = settings.DefaultMasksPath;
        DefaultCharsetsPath = settings.DefaultCharsetsPath;
        DefaultOutputPath = settings.DefaultOutputPath;
        PotfilePath = settings.PotfilePath;
        DefaultWorkloadProfile = settings.DefaultWorkloadProfile;
        UseOptimizedKernels = settings.UseOptimizedKernels;
        DefaultOutputFormat = settings.DefaultOutputFormat;
        StatusUpdateInterval = settings.StatusUpdateInterval;
        TemperatureLimit = settings.TemperatureLimit;
        AutoSaveSession = settings.AutoSaveSession;
        ShowNotifications = settings.ShowNotifications;
        DarkMode = settings.DarkMode;
    }

    private async Task LoadDevicesAsync()
    {
        try
        {
            var devices = await App.Hashcat.GetDevicesAsync();
            Devices.Clear();
            foreach (var device in devices)
            {
                Devices.Add(device);
            }
        }
        catch
        {
            // Devices will remain empty
        }
    }

    private async Task CheckHashcatVersionAsync()
    {
        if (string.IsNullOrEmpty(HashcatPath) || !File.Exists(HashcatPath))
        {
            HashcatFound = false;
            HashcatVersion = "Not found";
            return;
        }

        try
        {
            var startInfo = new System.Diagnostics.ProcessStartInfo
            {
                FileName = HashcatPath,
                Arguments = "--version",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                CreateNoWindow = true
            };

            using var process = System.Diagnostics.Process.Start(startInfo);
            if (process != null)
            {
                var output = await process.StandardOutput.ReadToEndAsync();
                await process.WaitForExitAsync();

                HashcatVersion = output.Trim();
                HashcatFound = true;
            }
        }
        catch
        {
            HashcatFound = false;
            HashcatVersion = "Error checking version";
        }
    }

    [RelayCommand]
    private void BrowseHashcat()
    {
        var dialog = new Microsoft.Win32.OpenFileDialog
        {
            Title = "Select Hashcat Executable",
            Filter = "Executable (hashcat.exe)|hashcat.exe|All Files (*.*)|*.*",
            FileName = "hashcat.exe"
        };

        if (dialog.ShowDialog() == true)
        {
            HashcatPath = dialog.FileName;
            _ = CheckHashcatVersionAsync();

            // Auto-detect related paths
            var hashcatDir = Path.GetDirectoryName(dialog.FileName);
            if (!string.IsNullOrEmpty(hashcatDir))
            {
                if (string.IsNullOrEmpty(DefaultWordlistPath))
                    DefaultWordlistPath = hashcatDir;

                var rulesDir = Path.Combine(hashcatDir, "rules");
                if (Directory.Exists(rulesDir) && string.IsNullOrEmpty(DefaultRulesPath))
                    DefaultRulesPath = rulesDir;

                var masksDir = Path.Combine(hashcatDir, "masks");
                if (Directory.Exists(masksDir) && string.IsNullOrEmpty(DefaultMasksPath))
                    DefaultMasksPath = masksDir;

                var charsetsDir = Path.Combine(hashcatDir, "charsets");
                if (Directory.Exists(charsetsDir) && string.IsNullOrEmpty(DefaultCharsetsPath))
                    DefaultCharsetsPath = charsetsDir;

                var potfile = Path.Combine(hashcatDir, "hashcat.potfile");
                if (string.IsNullOrEmpty(PotfilePath))
                    PotfilePath = potfile;

                if (string.IsNullOrEmpty(DefaultOutputPath))
                    DefaultOutputPath = hashcatDir;
            }
        }
    }

    [RelayCommand]
    private void BrowseWordlistPath()
    {
        var folder = DialogHelper.BrowseForFolder("Select Default Wordlist Folder");
        if (!string.IsNullOrEmpty(folder))
        {
            DefaultWordlistPath = folder;
        }
    }

    [RelayCommand]
    private void BrowseRulesPath()
    {
        var folder = DialogHelper.BrowseForFolder("Select Default Rules Folder");
        if (!string.IsNullOrEmpty(folder))
        {
            DefaultRulesPath = folder;
        }
    }

    [RelayCommand]
    private void BrowseMasksPath()
    {
        var folder = DialogHelper.BrowseForFolder("Select Default Masks Folder");
        if (!string.IsNullOrEmpty(folder))
        {
            DefaultMasksPath = folder;
        }
    }

    [RelayCommand]
    private void BrowseOutputPath()
    {
        var folder = DialogHelper.BrowseForFolder("Select Default Output Folder");
        if (!string.IsNullOrEmpty(folder))
        {
            DefaultOutputPath = folder;
        }
    }

    [RelayCommand]
    private void BrowsePotfile()
    {
        var dialog = new Microsoft.Win32.OpenFileDialog
        {
            Title = "Select Potfile",
            Filter = "Potfile (*.potfile)|*.potfile|All Files (*.*)|*.*"
        };

        if (dialog.ShowDialog() == true)
        {
            PotfilePath = dialog.FileName;
        }
    }

    [RelayCommand]
    private void SaveSettings()
    {
        var settings = App.Settings.Settings;

        settings.HashcatPath = HashcatPath;
        settings.DefaultWordlistPath = DefaultWordlistPath;
        settings.DefaultRulesPath = DefaultRulesPath;
        settings.DefaultMasksPath = DefaultMasksPath;
        settings.DefaultCharsetsPath = DefaultCharsetsPath;
        settings.DefaultOutputPath = DefaultOutputPath;
        settings.PotfilePath = PotfilePath;
        settings.DefaultWorkloadProfile = DefaultWorkloadProfile;
        settings.UseOptimizedKernels = UseOptimizedKernels;
        settings.DefaultOutputFormat = DefaultOutputFormat;
        settings.StatusUpdateInterval = StatusUpdateInterval;
        settings.TemperatureLimit = TemperatureLimit;
        settings.AutoSaveSession = AutoSaveSession;
        settings.ShowNotifications = ShowNotifications;
        settings.DarkMode = DarkMode;

        App.Settings.Save();
        App.Hashcat.HashcatPath = HashcatPath;

        StatusMessage = "Settings saved successfully";
    }

    [RelayCommand]
    private void ResetSettings()
    {
        var result = System.Windows.MessageBox.Show(
            "Are you sure you want to reset all settings to default values?",
            "Reset Settings",
            System.Windows.MessageBoxButton.YesNo,
            System.Windows.MessageBoxImage.Question);

        if (result == System.Windows.MessageBoxResult.Yes)
        {
            App.Settings.Settings = new AppSettings();
            LoadSettings();
            SaveSettings();
            StatusMessage = "Settings reset to defaults";
        }
    }

    [RelayCommand]
    private async Task RefreshDevices()
    {
        IsLoading = true;
        StatusMessage = "Refreshing devices...";

        await LoadDevicesAsync();

        IsLoading = false;
        StatusMessage = $"Found {Devices.Count} device(s)";
    }

    [RelayCommand]
    private void OpenHashcatFolder()
    {
        if (!string.IsNullOrEmpty(HashcatPath) && File.Exists(HashcatPath))
        {
            var folder = Path.GetDirectoryName(HashcatPath);
            if (!string.IsNullOrEmpty(folder))
            {
                System.Diagnostics.Process.Start("explorer.exe", folder);
            }
        }
    }
}
