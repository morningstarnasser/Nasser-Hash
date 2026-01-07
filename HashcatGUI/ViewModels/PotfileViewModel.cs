using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HashcatGUI.Models;

namespace HashcatGUI.ViewModels;

public partial class PotfileViewModel : ViewModelBase
{
    [ObservableProperty]
    private string _potfilePath = string.Empty;

    [ObservableProperty]
    private string _searchText = string.Empty;

    [ObservableProperty]
    private CrackedHash? _selectedHash;

    [ObservableProperty]
    private int _totalCount;

    public ObservableCollection<CrackedHash> AllHashes { get; } = new();
    public ObservableCollection<CrackedHash> FilteredHashes { get; } = new();

    public PotfileViewModel()
    {
        PotfilePath = App.Settings.Settings.PotfilePath ?? string.Empty;
        // Load asynchronously after initialization
        if (!string.IsNullOrEmpty(PotfilePath))
        {
            _ = LoadPotfileSafeAsync();
        }
    }

    private async Task LoadPotfileSafeAsync()
    {
        try
        {
            await Task.Delay(100); // Small delay to let UI initialize
            await LoadPotfileAsync();
        }
        catch
        {
            StatusMessage = "Error loading potfile";
        }
    }

    partial void OnPotfilePathChanged(string value)
    {
        _ = LoadPotfileSafeAsync();
    }

    partial void OnSearchTextChanged(string value)
    {
        FilterHashes(value);
    }

    private async Task LoadPotfileAsync()
    {
        AllHashes.Clear();
        FilteredHashes.Clear();

        if (string.IsNullOrEmpty(PotfilePath) || !File.Exists(PotfilePath))
        {
            StatusMessage = "Potfile not found";
            TotalCount = 0;
            return;
        }

        try
        {
            IsLoading = true;
            StatusMessage = "Loading potfile...";

            var hashes = await App.Hashcat.ReadPotfileAsync(PotfilePath);

            foreach (var hash in hashes)
            {
                AllHashes.Add(hash);
                FilteredHashes.Add(hash);
            }

            TotalCount = AllHashes.Count;
            StatusMessage = $"Loaded {TotalCount} cracked hashes";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error loading potfile: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    private void FilterHashes(string searchText)
    {
        FilteredHashes.Clear();

        if (string.IsNullOrWhiteSpace(searchText))
        {
            foreach (var hash in AllHashes)
                FilteredHashes.Add(hash);
        }
        else
        {
            var search = searchText.ToLowerInvariant();
            foreach (var hash in AllHashes.Where(h =>
                h.Hash.ToLowerInvariant().Contains(search) ||
                h.Password.ToLowerInvariant().Contains(search)))
            {
                FilteredHashes.Add(hash);
            }
        }
    }

    [RelayCommand]
    private void BrowsePotfile()
    {
        var dialog = new Microsoft.Win32.OpenFileDialog
        {
            Title = "Select Potfile",
            Filter = "Potfile (*.potfile)|*.potfile|All Files (*.*)|*.*",
            InitialDirectory = Path.GetDirectoryName(App.Settings.Settings.HashcatPath)
        };

        if (dialog.ShowDialog() == true)
        {
            PotfilePath = dialog.FileName;
            App.Settings.Settings.PotfilePath = dialog.FileName;
            App.Settings.Save();
        }
    }

    [RelayCommand]
    private async Task RefreshPotfile()
    {
        await LoadPotfileAsync();
    }

    [RelayCommand]
    private void CopyHash()
    {
        if (SelectedHash != null)
        {
            try
            {
                System.Windows.Clipboard.SetText(SelectedHash.Hash);
                StatusMessage = "Hash copied to clipboard";
            }
            catch
            {
                StatusMessage = "Failed to copy hash";
            }
        }
    }

    [RelayCommand]
    private void CopyPassword()
    {
        if (SelectedHash != null)
        {
            try
            {
                System.Windows.Clipboard.SetText(SelectedHash.Password);
                StatusMessage = "Password copied to clipboard";
            }
            catch
            {
                StatusMessage = "Failed to copy password";
            }
        }
    }

    [RelayCommand]
    private void CopyAll()
    {
        if (SelectedHash != null)
        {
            try
            {
                System.Windows.Clipboard.SetText($"{SelectedHash.Hash}:{SelectedHash.Password}");
                StatusMessage = "Hash:Password copied to clipboard";
            }
            catch
            {
                StatusMessage = "Failed to copy";
            }
        }
    }

    [RelayCommand]
    private void ExportPotfile()
    {
        if (!FilteredHashes.Any())
        {
            StatusMessage = "No hashes to export";
            return;
        }

        var dialog = new Microsoft.Win32.SaveFileDialog
        {
            Title = "Export Potfile",
            Filter = "CSV Files (*.csv)|*.csv|Text Files (*.txt)|*.txt",
            DefaultExt = ".csv"
        };

        if (dialog.ShowDialog() == true)
        {
            try
            {
                using var writer = new StreamWriter(dialog.FileName);
                writer.WriteLine("Hash,Password");

                foreach (var hash in FilteredHashes)
                {
                    writer.WriteLine($"\"{hash.Hash}\",\"{hash.Password}\"");
                }

                StatusMessage = $"Exported {FilteredHashes.Count} hashes to {dialog.FileName}";
            }
            catch (Exception ex)
            {
                StatusMessage = $"Export failed: {ex.Message}";
            }
        }
    }

    [RelayCommand]
    private async Task ClearPotfile()
    {
        if (!File.Exists(PotfilePath))
            return;

        var result = System.Windows.MessageBox.Show(
            "Are you sure you want to clear the potfile? This action cannot be undone.",
            "Clear Potfile",
            System.Windows.MessageBoxButton.YesNo,
            System.Windows.MessageBoxImage.Warning);

        if (result == System.Windows.MessageBoxResult.Yes)
        {
            try
            {
                // Backup first
                var backupPath = PotfilePath + ".bak";
                File.Copy(PotfilePath, backupPath, true);

                // Clear the file
                await File.WriteAllTextAsync(PotfilePath, string.Empty);

                AllHashes.Clear();
                FilteredHashes.Clear();
                TotalCount = 0;

                StatusMessage = $"Potfile cleared. Backup saved to {backupPath}";
            }
            catch (Exception ex)
            {
                StatusMessage = $"Failed to clear potfile: {ex.Message}";
            }
        }
    }
}
