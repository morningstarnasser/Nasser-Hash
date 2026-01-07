using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HashcatGUI.Models;
using Microsoft.Win32;

namespace HashcatGUI.ViewModels;

public partial class DashboardViewModel : ViewModelBase
{
    private MainViewModel? _mainViewModel;

    public void SetMainViewModel(MainViewModel mainViewModel)
    {
        _mainViewModel = mainViewModel;
    }

    [ObservableProperty]
    private string _hashFilePath = string.Empty;

    [ObservableProperty]
    private int _hashCount;

    [ObservableProperty]
    private HashMode? _selectedHashMode;

    [ObservableProperty]
    private string _hashModeSearchText = string.Empty;

    [ObservableProperty]
    private AttackMode? _selectedAttackMode;

    [ObservableProperty]
    private bool _isQuickStartReady;

    public ObservableCollection<HashMode> FilteredHashModes { get; } = new();
    public ObservableCollection<HashMode> AllHashModes { get; } = new();
    public ObservableCollection<AttackMode> AttackModes { get; } = new();
    public ObservableCollection<CrackedHash> RecentCrackedHashes { get; } = new();
    public ObservableCollection<string> RecentHashFiles { get; } = new();
    public ObservableCollection<DeviceInfo> Devices { get; } = new();

    public DashboardViewModel()
    {
        // Load hash modes
        foreach (var mode in HashMode.GetAllModes())
        {
            AllHashModes.Add(mode);
            FilteredHashModes.Add(mode);
        }

        // Load attack modes
        foreach (var mode in AttackMode.GetAllModes())
        {
            AttackModes.Add(mode);
        }

        SelectedAttackMode = AttackModes.FirstOrDefault();

        // Load recent files
        foreach (var file in App.Settings.Settings.RecentHashFiles.Take(5))
        {
            RecentHashFiles.Add(file);
        }

        // Load devices asynchronously
        _ = LoadDevicesAsync();
    }

    partial void OnHashModeSearchTextChanged(string value)
    {
        FilterHashModes(value);
    }

    partial void OnHashFilePathChanged(string value)
    {
        UpdateQuickStartReady();
        if (File.Exists(value))
        {
            try
            {
                HashCount = File.ReadLines(value).Count();
            }
            catch
            {
                HashCount = 0;
            }
        }
    }

    partial void OnSelectedHashModeChanged(HashMode? value)
    {
        UpdateQuickStartReady();
    }

    partial void OnSelectedAttackModeChanged(AttackMode? value)
    {
        UpdateQuickStartReady();
    }

    private void UpdateQuickStartReady()
    {
        IsQuickStartReady = !string.IsNullOrEmpty(HashFilePath) &&
                           File.Exists(HashFilePath) &&
                           SelectedHashMode != null &&
                           SelectedAttackMode != null;
    }

    private void FilterHashModes(string searchText)
    {
        FilteredHashModes.Clear();

        if (string.IsNullOrWhiteSpace(searchText))
        {
            foreach (var mode in AllHashModes)
                FilteredHashModes.Add(mode);
        }
        else
        {
            var search = searchText.ToLowerInvariant();
            foreach (var mode in AllHashModes.Where(m =>
                m.Name.ToLowerInvariant().Contains(search) ||
                m.Id.ToString().Contains(search) ||
                m.Category.ToLowerInvariant().Contains(search)))
            {
                FilteredHashModes.Add(mode);
            }
        }
    }

    private async Task LoadDevicesAsync()
    {
        try
        {
            var devices = await App.Hashcat.GetDevicesAsync();
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

    [RelayCommand]
    private void BrowseHashFile()
    {
        var dialog = new OpenFileDialog
        {
            Title = "Select Hash File",
            Filter = "All Files (*.*)|*.*|Hash Files (*.hash;*.txt)|*.hash;*.txt|Wallet Files (*.dat)|*.dat",
            FilterIndex = 1
        };

        if (dialog.ShowDialog() == true)
        {
            HashFilePath = dialog.FileName;
            App.Settings.AddRecentHashFile(dialog.FileName);
        }
    }

    [RelayCommand]
    private void LoadRecentFile(string path)
    {
        if (File.Exists(path))
        {
            HashFilePath = path;
        }
    }

    [RelayCommand]
    private async Task QuickStart()
    {
        if (!IsQuickStartReady || SelectedAttackMode == null || SelectedHashMode == null || _mainViewModel == null)
            return;

        // Navigate to attack view and configure
        _mainViewModel.AttackVM.HashFilePath = HashFilePath;
        _mainViewModel.AttackVM.SelectedHashMode = SelectedHashMode;
        _mainViewModel.AttackVM.SelectedAttackMode = SelectedAttackMode;

        _mainViewModel.NavigateToAttackCommand.Execute(null);
        await Task.CompletedTask;
    }

    [RelayCommand]
    private void SelectHashMode(HashMode mode)
    {
        SelectedHashMode = mode;
    }

    public void AddCrackedHash(CrackedHash hash)
    {
        RecentCrackedHashes.Insert(0, hash);
        if (RecentCrackedHashes.Count > 50)
            RecentCrackedHashes.RemoveAt(50);
    }
}
