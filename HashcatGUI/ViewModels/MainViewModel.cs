using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HashcatGUI.Models;
using HashcatGUI.Services;

namespace HashcatGUI.ViewModels;

public partial class MainViewModel : ViewModelBase
{
    [ObservableProperty]
    private ViewModelBase? _currentView;

    [ObservableProperty]
    private string _currentViewName = "Dashboard";

    [ObservableProperty]
    private bool _isHashcatRunning;

    [ObservableProperty]
    private string _hashcatStatus = "Ready";

    [ObservableProperty]
    private double _progressPercent;

    [ObservableProperty]
    private string _speedDisplay = "0 H/s";

    [ObservableProperty]
    private string _etaDisplay = "--:--:--";

    [ObservableProperty]
    private int _crackedCount;

    [ObservableProperty]
    private int _totalHashes;

    public DashboardViewModel DashboardVM { get; }
    public AttackViewModel AttackVM { get; }
    public WordlistsViewModel WordlistsVM { get; }
    public SettingsViewModel SettingsVM { get; }
    public BenchmarkViewModel BenchmarkVM { get; }
    public PotfileViewModel PotfileVM { get; }
    public WalletAnalyzerViewModel WalletAnalyzerVM { get; }
    public SmartAttackViewModel SmartAttackVM { get; }

    public ObservableCollection<string> OutputLog { get; } = new();

    public MainViewModel()
    {
        DashboardVM = new DashboardViewModel();
        AttackVM = new AttackViewModel();
        WordlistsVM = new WordlistsViewModel();
        SettingsVM = new SettingsViewModel();
        BenchmarkVM = new BenchmarkViewModel();
        PotfileVM = new PotfileViewModel();
        WalletAnalyzerVM = new WalletAnalyzerViewModel();
        SmartAttackVM = new SmartAttackViewModel();

        // Set parent references
        DashboardVM.SetMainViewModel(this);
        AttackVM.SetMainViewModel(this);
        SmartAttackVM.SetMainViewModel(this);

        CurrentView = DashboardVM;

        // Subscribe to hashcat events
        App.Hashcat.HashcatPath = App.Settings.Settings.HashcatPath;
        App.Hashcat.OutputReceived += OnOutputReceived;
        App.Hashcat.ErrorReceived += OnErrorReceived;
        App.Hashcat.StatusUpdated += OnStatusUpdated;
        App.Hashcat.HashCracked += OnHashCracked;
        App.Hashcat.ProcessExited += OnProcessExited;
    }

    [RelayCommand]
    private void NavigateToDashboard()
    {
        CurrentView = DashboardVM;
        CurrentViewName = "Dashboard";
    }

    [RelayCommand]
    private void NavigateToAttack()
    {
        CurrentView = AttackVM;
        CurrentViewName = "Attack";
    }

    [RelayCommand]
    private void NavigateToWordlists()
    {
        CurrentView = WordlistsVM;
        CurrentViewName = "Wordlists & Rules";
    }

    [RelayCommand]
    private void NavigateToBenchmark()
    {
        CurrentView = BenchmarkVM;
        CurrentViewName = "Benchmark";
    }

    [RelayCommand]
    private void NavigateToPotfile()
    {
        CurrentView = PotfileVM;
        CurrentViewName = "Potfile";
    }

    [RelayCommand]
    private void NavigateToSettings()
    {
        CurrentView = SettingsVM;
        CurrentViewName = "Settings";
    }

    [RelayCommand]
    private void NavigateToWalletAnalyzer()
    {
        CurrentView = WalletAnalyzerVM;
        CurrentViewName = "Wallet Analyzer";
    }

    [RelayCommand]
    private void NavigateToSmartAttack()
    {
        CurrentView = SmartAttackVM;
        CurrentViewName = "Smart Attack";
    }

    [RelayCommand]
    private void StartAttack()
    {
        AttackVM.StartAttackCommand.Execute(null);
    }

    [RelayCommand]
    private void StopAttack()
    {
        App.Hashcat.Stop();
    }

    [RelayCommand]
    private void PauseAttack()
    {
        App.Hashcat.Pause();
        HashcatStatus = "Paused";
    }

    [RelayCommand]
    private void ResumeAttack()
    {
        App.Hashcat.Resume();
        HashcatStatus = "Running";
    }

    private void OnOutputReceived(object? sender, string output)
    {
        Application.Current.Dispatcher.Invoke(() =>
        {
            OutputLog.Add(output);
            if (OutputLog.Count > 1000)
                OutputLog.RemoveAt(0);
        });
    }

    private void OnErrorReceived(object? sender, string error)
    {
        Application.Current.Dispatcher.Invoke(() =>
        {
            OutputLog.Add($"[ERROR] {error}");
        });
    }

    private void OnStatusUpdated(object? sender, HashcatStatusJson status)
    {
        Application.Current.Dispatcher.Invoke(() =>
        {
            // Mark as running when we receive status updates
            IsHashcatRunning = true;

            if (status.Progress != null && status.Progress.Length >= 2)
            {
                var current = status.Progress[0];
                var total = status.Progress[1];
                ProgressPercent = total > 0 ? (double)current / total * 100 : 0;
            }

            if (status.RecoveredHashes != null && status.RecoveredHashes.Length >= 2)
            {
                CrackedCount = status.RecoveredHashes[0];
                TotalHashes = status.RecoveredHashes[1];
            }

            if (status.Devices != null && status.Devices.Length > 0)
            {
                var totalSpeed = status.Devices.Sum(d => d.Speed);
                SpeedDisplay = FormatSpeed(totalSpeed);
            }

            if (status.EstimatedStop > 0)
            {
                var eta = DateTimeOffset.FromUnixTimeSeconds(status.EstimatedStop).LocalDateTime - DateTime.Now;
                EtaDisplay = eta > TimeSpan.Zero ? FormatTimeSpan(eta) : "Finishing...";
            }

            // Set status based on JSON status field or default to Running if we're getting updates
            if (status.Status > 0)
            {
                HashcatStatus = status.Status switch
                {
                    1 => "Running",
                    2 => "Paused",
                    3 => "Exhausted",
                    4 => "Cracked",
                    5 => "Aborted",
                    6 => "Quit",
                    7 => "Bypassed",
                    _ => "Running"
                };
            }
            else
            {
                // If status is 0 or not set but we're receiving updates, we're running
                HashcatStatus = "Running";
            }
        });
    }

    private void OnHashCracked(object? sender, CrackedHash hash)
    {
        Application.Current.Dispatcher.Invoke(() =>
        {
            CrackedCount++;
            DashboardVM.AddCrackedHash(hash);
        });
    }

    private void OnProcessExited(object? sender, int exitCode)
    {
        Application.Current.Dispatcher.Invoke(() =>
        {
            IsHashcatRunning = false;
            HashcatStatus = exitCode switch
            {
                0 => "Completed - Hash(es) cracked",
                1 => "Exhausted - All combinations tried",
                2 => "Aborted by user",
                _ => $"Finished (Exit code: {exitCode})"
            };
        });
    }

    private static string FormatSpeed(long speed)
    {
        if (speed >= 1_000_000_000_000)
            return $"{speed / 1_000_000_000_000.0:F2} TH/s";
        if (speed >= 1_000_000_000)
            return $"{speed / 1_000_000_000.0:F2} GH/s";
        if (speed >= 1_000_000)
            return $"{speed / 1_000_000.0:F2} MH/s";
        if (speed >= 1_000)
            return $"{speed / 1_000.0:F2} kH/s";
        return $"{speed} H/s";
    }

    private static string FormatTimeSpan(TimeSpan ts)
    {
        if (ts.TotalDays >= 1)
            return $"{(int)ts.TotalDays}d {ts.Hours:D2}:{ts.Minutes:D2}:{ts.Seconds:D2}";
        return $"{ts.Hours:D2}:{ts.Minutes:D2}:{ts.Seconds:D2}";
    }
}
