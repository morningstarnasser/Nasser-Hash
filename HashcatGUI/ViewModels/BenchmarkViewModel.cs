using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HashcatGUI.Models;

namespace HashcatGUI.ViewModels;

public partial class BenchmarkViewModel : ViewModelBase
{
    [ObservableProperty]
    private bool _isRunning;

    [ObservableProperty]
    private HashMode? _selectedHashMode;

    [ObservableProperty]
    private bool _benchmarkAllModes;

    [ObservableProperty]
    private string _searchText = string.Empty;

    [ObservableProperty]
    private double _progressPercent;

    [ObservableProperty]
    private string _currentlyBenchmarking = string.Empty;

    private CancellationTokenSource? _cancellationTokenSource;

    public ObservableCollection<HashMode> FilteredHashModes { get; } = new();
    public ObservableCollection<BenchmarkResult> Results { get; } = new();
    public ObservableCollection<string> BenchmarkLog { get; } = new();

    public BenchmarkViewModel()
    {
        foreach (var mode in HashMode.GetAllModes())
        {
            FilteredHashModes.Add(mode);
        }
    }

    partial void OnSearchTextChanged(string value)
    {
        FilterHashModes(value);
    }

    private void FilterHashModes(string searchText)
    {
        FilteredHashModes.Clear();
        var allModes = HashMode.GetAllModes();

        if (string.IsNullOrWhiteSpace(searchText))
        {
            foreach (var mode in allModes)
                FilteredHashModes.Add(mode);
        }
        else
        {
            var search = searchText.ToLowerInvariant();
            foreach (var mode in allModes.Where(m =>
                m.Name.ToLowerInvariant().Contains(search) ||
                m.Id.ToString().Contains(search) ||
                m.Category.ToLowerInvariant().Contains(search)))
            {
                FilteredHashModes.Add(mode);
            }
        }
    }

    [RelayCommand]
    private async Task RunBenchmark()
    {
        if (IsRunning)
            return;

        if (!BenchmarkAllModes && SelectedHashMode == null)
        {
            StatusMessage = "Please select a hash mode or enable 'Benchmark All Modes'";
            return;
        }

        IsRunning = true;
        Results.Clear();
        BenchmarkLog.Clear();
        ProgressPercent = 0;
        _cancellationTokenSource = new CancellationTokenSource();

        App.Hashcat.OutputReceived += OnBenchmarkOutput;

        try
        {
            if (BenchmarkAllModes)
            {
                var modes = HashMode.GetAllModes();
                var totalModes = modes.Count;
                var currentIndex = 0;

                foreach (var mode in modes)
                {
                    if (_cancellationTokenSource.Token.IsCancellationRequested)
                        break;

                    CurrentlyBenchmarking = mode.DisplayName;
                    ProgressPercent = (double)currentIndex / totalModes * 100;

                    try
                    {
                        var results = await App.Hashcat.RunBenchmarkAsync(mode.Id, _cancellationTokenSource.Token);
                        foreach (var result in results)
                        {
                            Results.Add(result);
                        }
                    }
                    catch (OperationCanceledException)
                    {
                        break;
                    }
                    catch
                    {
                        // Skip failed benchmarks
                    }

                    currentIndex++;
                }
            }
            else if (SelectedHashMode != null)
            {
                CurrentlyBenchmarking = SelectedHashMode.DisplayName;
                var results = await App.Hashcat.RunBenchmarkAsync(SelectedHashMode.Id, _cancellationTokenSource.Token);
                foreach (var result in results)
                {
                    Results.Add(result);
                }
            }

            ProgressPercent = 100;
            CurrentlyBenchmarking = string.Empty;
            StatusMessage = $"Benchmark completed. {Results.Count} result(s)";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Benchmark failed: {ex.Message}";
        }
        finally
        {
            App.Hashcat.OutputReceived -= OnBenchmarkOutput;
            IsRunning = false;
            _cancellationTokenSource = null;
        }
    }

    [RelayCommand]
    private void StopBenchmark()
    {
        _cancellationTokenSource?.Cancel();
        StatusMessage = "Benchmark cancelled";
    }

    [RelayCommand]
    private void ClearResults()
    {
        Results.Clear();
        BenchmarkLog.Clear();
        ProgressPercent = 0;
        StatusMessage = string.Empty;
    }

    [RelayCommand]
    private void ExportResults()
    {
        if (!Results.Any())
        {
            StatusMessage = "No results to export";
            return;
        }

        var dialog = new Microsoft.Win32.SaveFileDialog
        {
            Title = "Export Benchmark Results",
            Filter = "CSV Files (*.csv)|*.csv|Text Files (*.txt)|*.txt",
            DefaultExt = ".csv"
        };

        if (dialog.ShowDialog() == true)
        {
            try
            {
                using var writer = new System.IO.StreamWriter(dialog.FileName);
                writer.WriteLine("Hash Mode,Hash Name,Speed,Speed Formatted");

                foreach (var result in Results)
                {
                    writer.WriteLine($"{result.HashMode},\"{result.HashName}\",{result.Speed},{result.SpeedFormatted}");
                }

                StatusMessage = $"Results exported to {dialog.FileName}";
            }
            catch (Exception ex)
            {
                StatusMessage = $"Export failed: {ex.Message}";
            }
        }
    }

    private void OnBenchmarkOutput(object? sender, string output)
    {
        System.Windows.Application.Current.Dispatcher.Invoke(() =>
        {
            BenchmarkLog.Add(output);
            if (BenchmarkLog.Count > 500)
                BenchmarkLog.RemoveAt(0);
        });
    }
}
