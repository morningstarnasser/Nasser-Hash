using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HashcatGUI.Models;
using HashcatGUI.Services;
using Microsoft.Win32;

namespace HashcatGUI.ViewModels;

public partial class SmartAttackViewModel : ViewModelBase
{
    private MainViewModel? _mainViewModel;

    [ObservableProperty]
    private string _walletFilePath = string.Empty;

    [ObservableProperty]
    private bool _isAnalyzing;

    [ObservableProperty]
    private bool _isAttackRunning;

    [ObservableProperty]
    private WalletAnalysis? _currentAnalysis;

    [ObservableProperty]
    private SmartAttackProfile? _currentProfile;

    [ObservableProperty]
    private string _queueStatusText = "No wallets in queue";

    [ObservableProperty]
    private string _currentPhaseName = "Not started";

    [ObservableProperty]
    private double _overallProgress;

    [ObservableProperty]
    private string _estimatedTimeRemaining = "--:--:--";

    [ObservableProperty]
    private string _gpuTemperature = "--";

    [ObservableProperty]
    private string _gpuUtilization = "--";

    [ObservableProperty]
    private string _gpuSpeed = "0 H/s";

    [ObservableProperty]
    private GpuProfile _selectedGpuProfile = GpuProfile.Balanced;

    [ObservableProperty]
    private string _gpuProfileDescription = "Ausgewogen - Gute Performance bei sicheren Temperaturen";

    [ObservableProperty]
    private double _successProbability;

    [ObservableProperty]
    private string _successProbabilityText = "0%";

    [ObservableProperty]
    private SavedSession? _currentSession;

    [ObservableProperty]
    private bool _hasSavedSessions;

    public ObservableCollection<QueuedWallet> WalletQueue { get; } = new();
    public ObservableCollection<string> AttackLog { get; } = new();
    public ObservableCollection<SavedSession> SavedSessions { get; } = new();
    public Array GpuProfiles => Enum.GetValues(typeof(GpuProfile));

    public void SetMainViewModel(MainViewModel mainViewModel)
    {
        _mainViewModel = mainViewModel;
        LoadSavedSessions();
        SmartAttackService.Initialize();

        // Subscribe to hashcat status updates
        App.Hashcat.StatusUpdated += OnHashcatStatusUpdated;
        App.Hashcat.OutputReceived += OnHashcatOutputReceived;
        App.Hashcat.ProcessExited += OnHashcatProcessExited;
    }

    private void OnHashcatStatusUpdated(object? sender, HashcatStatusJson status)
    {
        Application.Current.Dispatcher.Invoke(() =>
        {
            // Update GPU info from status
            if (status.Devices != null && status.Devices.Length > 0)
            {
                var device = status.Devices[0];
                GpuTemperature = device.Temp?.ToString() ?? "--";
                GpuUtilization = device.Util?.ToString() ?? "--";

                var totalSpeed = status.Devices.Sum(d => d.Speed);
                GpuSpeed = FormatSpeed(totalSpeed);

                // Debug: Show that we received status
                AddLog($"[STATUS] Temp: {GpuTemperature}°C, Util: {GpuUtilization}%, Speed: {GpuSpeed}");
            }

            // Update progress
            if (status.Progress != null && status.Progress.Length >= 2)
            {
                var current = status.Progress[0];
                var total = status.Progress[1];
                OverallProgress = total > 0 ? (double)current / total * 100 : 0;
            }

            // Update ETA
            if (status.EstimatedStop > 0)
            {
                var eta = DateTimeOffset.FromUnixTimeSeconds(status.EstimatedStop).LocalDateTime - DateTime.Now;
                EstimatedTimeRemaining = eta > TimeSpan.Zero ? FormatTimeSpan(eta) : "Finishing...";
            }
        });
    }

    private void OnHashcatOutputReceived(object? sender, string output)
    {
        if (IsAttackRunning)
        {
            AddLog(output);
        }
    }

    private void OnHashcatProcessExited(object? sender, int exitCode)
    {
        Application.Current.Dispatcher.Invoke(() =>
        {
            IsAttackRunning = false;
            var resultMessage = exitCode switch
            {
                0 => "SUCCESS! Password cracked!",
                1 => "Exhausted - All combinations tried",
                2 => "Aborted by user",
                _ => $"Finished (Exit code: {exitCode})"
            };
            AddLog($"Attack finished: {resultMessage}");
            CurrentPhaseName = resultMessage;

            // Update session state
            if (CurrentSession != null)
            {
                var newState = exitCode == 0 ? SessionState.Completed : SessionState.Exhausted;
                SessionService.UpdateSessionState(CurrentSession, newState);
            }
        });
    }

    private static string FormatTimeSpan(TimeSpan ts)
    {
        if (ts.TotalDays >= 1)
            return $"{(int)ts.TotalDays}d {ts.Hours:D2}:{ts.Minutes:D2}:{ts.Seconds:D2}";
        return $"{ts.Hours:D2}:{ts.Minutes:D2}:{ts.Seconds:D2}";
    }

    partial void OnSelectedGpuProfileChanged(GpuProfile value)
    {
        GpuProfileDescription = GpuOptimizationService.GetProfileDescription(value);
        UpdateSuccessProbability();
    }

    private void LoadSavedSessions()
    {
        SavedSessions.Clear();
        var sessions = SessionService.GetAllSessions();
        foreach (var session in sessions)
        {
            SavedSessions.Add(session);
        }
        HasSavedSessions = SavedSessions.Count > 0;
    }

    private void UpdateSuccessProbability()
    {
        if (CurrentProfile == null)
        {
            SuccessProbability = 0;
            SuccessProbabilityText = "0%";
            return;
        }

        var baseProbability = CurrentProfile.SuccessProbability;
        var gpuMultiplier = GpuOptimizationService.GetSpeedMultiplier(SelectedGpuProfile);

        // Higher GPU performance slightly increases success by trying more combinations
        var adjustedProbability = Math.Min(baseProbability * (1 + (gpuMultiplier - 1) * 0.1), 0.95);

        SuccessProbability = adjustedProbability;
        SuccessProbabilityText = $"{adjustedProbability:P0}";
    }

    [RelayCommand]
    private void BrowseWallet()
    {
        var dialog = new OpenFileDialog
        {
            Title = "Select Bitcoin Wallet File",
            Filter = "Wallet Files (*.dat)|*.dat|All Files (*.*)|*.*",
            CheckFileExists = true
        };

        if (dialog.ShowDialog() == true)
        {
            WalletFilePath = dialog.FileName;
        }
    }

    [RelayCommand]
    private async Task AddWalletToQueue()
    {
        if (string.IsNullOrWhiteSpace(WalletFilePath) || !File.Exists(WalletFilePath))
        {
            return;
        }

        var wallet = new QueuedWallet
        {
            FilePath = WalletFilePath,
            FileName = Path.GetFileName(WalletFilePath),
            Status = QueueStatus.Pending
        };

        WalletQueue.Add(wallet);
        UpdateQueueStatus();

        // Start analysis in background
        wallet.Status = QueueStatus.Analyzing;
        try
        {
            wallet.Analysis = await WalletAnalyzerService.AnalyzeWalletAsync(WalletFilePath);
            if (wallet.Analysis.IsValid)
            {
                wallet.Status = QueueStatus.Ready;
                wallet.HashFile = await CreateTempHashFile(wallet.Analysis.HashcatHash);
            }
            else
            {
                wallet.Status = QueueStatus.Failed;
                wallet.ErrorMessage = wallet.Analysis.ErrorMessage;
            }
        }
        catch (Exception ex)
        {
            wallet.Status = QueueStatus.Failed;
            wallet.ErrorMessage = ex.Message;
        }

        UpdateQueueStatus();
        WalletFilePath = string.Empty;
    }

    [RelayCommand]
    private void RemoveFromQueue(QueuedWallet? wallet)
    {
        if (wallet != null && !IsAttackRunning)
        {
            WalletQueue.Remove(wallet);
            UpdateQueueStatus();
        }
    }

    [RelayCommand]
    private async Task StartSmartAttack()
    {
        if (CurrentAnalysis == null || string.IsNullOrEmpty(CurrentAnalysis.HashcatHash))
        {
            AddLog("No wallet analyzed. Please analyze a wallet first.");
            return;
        }

        IsAttackRunning = true;
        CurrentProfile = SmartAttackService.GenerateProfile(CurrentAnalysis, App.Settings.Settings.HashcatPath);
        UpdateSuccessProbability();

        var gpuArgs = GpuOptimizationService.GetOptimizedArguments(SelectedGpuProfile);
        var speedMultiplier = GpuOptimizationService.GetSpeedMultiplier(SelectedGpuProfile);

        AddLog($"Starting Smart Attack for {CurrentAnalysis.EstimatedEra} wallet");
        AddLog($"Profile: {CurrentProfile.Name}");
        AddLog($"GPU Mode: {SelectedGpuProfile} (Speed x{speedMultiplier:F1})");
        AddLog($"Estimated duration: {CurrentProfile.EstimatedDurationMinutes / speedMultiplier:F0} minutes");
        AddLog($"Success probability: {SuccessProbabilityText}");
        AddLog($"GPU Arguments: {string.Join(" ", gpuArgs)}");

        // Create session for this attack
        var hashFile = WalletQueue.FirstOrDefault(w => w.FilePath == WalletFilePath)?.HashFile;
        if (!string.IsNullOrEmpty(hashFile))
        {
            CurrentSession = SessionService.CreateSession(WalletFilePath, CurrentAnalysis, CurrentProfile, hashFile);
            SessionService.UpdateSessionState(CurrentSession, SessionState.Running);
        }

        // TODO: Implement actual attack execution with GPU args
        await Task.Delay(1000);

        IsAttackRunning = false;
    }

    [RelayCommand]
    private async Task StartQueueProcessing()
    {
        if (WalletQueue.Count == 0)
        {
            AddLog("No wallets in queue.");
            return;
        }

        var readyWallets = WalletQueue.Where(w => w.Status == QueueStatus.Ready).ToList();
        if (readyWallets.Count == 0)
        {
            AddLog("No ready wallets in queue.");
            return;
        }

        IsAttackRunning = true;

        foreach (var wallet in readyWallets)
        {
            if (!IsAttackRunning) break; // User stopped

            wallet.Status = QueueStatus.Running;
            CurrentPhaseName = $"Processing: {wallet.FileName}";

            if (wallet.Analysis != null && !string.IsNullOrEmpty(wallet.HashFile))
            {
                var profile = SmartAttackService.GenerateProfile(wallet.Analysis, App.Settings.Settings.HashcatPath);
                CurrentProfile = profile;
                UpdateSuccessProbability();

                AddLog($"=== Starting attack on {wallet.FileName} ===");
                AddLog($"Profile: {profile.Name}");
                AddLog($"Phases: {profile.Phases.Count}");
                AddLog($"Success probability: {profile.SuccessProbability:P0}");

                // Execute each phase
                for (int i = 0; i < profile.Phases.Count && IsAttackRunning; i++)
                {
                    var phase = profile.Phases[i];
                    CurrentPhaseName = $"Phase {i + 1}/{profile.Phases.Count}: {phase.Name}";
                    AddLog($"--- Phase {i + 1}: {phase.Name} ---");

                    var success = await ExecutePhaseAsync(wallet.HashFile, phase);

                    if (success)
                    {
                        wallet.Status = QueueStatus.Completed;
                        AddLog($"SUCCESS! Password found for {wallet.FileName}");
                        break;
                    }
                }

                if (wallet.Status != QueueStatus.Completed)
                {
                    wallet.Status = QueueStatus.Failed;
                    wallet.ErrorMessage = "All phases exhausted";
                }
            }
        }

        IsAttackRunning = false;
        CurrentPhaseName = "Queue completed";
        AddLog("=== Queue processing finished ===");
    }

    private async Task<bool> ExecutePhaseAsync(string hashFile, AttackPhase phase)
    {
        var hashcatDir = Path.GetDirectoryName(App.Settings.Settings.HashcatPath) ?? ".";

        var config = new HashcatConfig
        {
            HashFile = hashFile,
            HashMode = 11300, // Bitcoin wallet
            AttackMode = phase.AttackMode,
            Wordlist = phase.Wordlist,
            SecondWordlist = phase.SecondWordlist,
            Mask = phase.Mask,
            RuleFiles = phase.Rules,
            IncrementMode = phase.IncrementMode,
            IncrementMin = phase.IncrementMin,
            IncrementMax = phase.IncrementMax,
            StatusJson = true,
            StatusTimer = 1, // Update every second for responsive UI
            WorkloadProfile = SelectedGpuProfile switch
            {
                GpuProfile.Conservative => 1,
                GpuProfile.Balanced => 2,
                GpuProfile.Performance => 3,
                GpuProfile.Insane => 4,
                _ => 2
            },
            OptimizedKernels = SelectedGpuProfile >= GpuProfile.Performance
        };

        // Set temperature limits based on GPU profile
        config.TempAbort = SelectedGpuProfile switch
        {
            GpuProfile.Conservative => 85,
            GpuProfile.Balanced => 90,
            GpuProfile.Performance => 95,
            GpuProfile.Insane => 100,
            _ => 90
        };

        try
        {
            AddLog($"Executing: Mode {phase.AttackMode}, Wordlist: {Path.GetFileName(phase.Wordlist ?? "N/A")}");

            App.Hashcat.HashcatPath = App.Settings.Settings.HashcatPath;
            await App.Hashcat.StartAsync(config);

            // Wait for process to complete
            while (App.Hashcat.IsRunning && IsAttackRunning)
            {
                await Task.Delay(500);
            }

            // Check if password was cracked (exit code 0)
            return false; // Will be set by ProcessExited event
        }
        catch (Exception ex)
        {
            AddLog($"Error: {ex.Message}");
            return false;
        }
    }

    [RelayCommand]
    private void StopAttack()
    {
        IsAttackRunning = false;
        App.Hashcat.Stop();
        AddLog("Attack stopped by user.");

        // Reset GPU values
        GpuTemperature = "--";
        GpuUtilization = "--";
        GpuSpeed = "0 H/s";
    }

    [RelayCommand]
    private void ClearLog()
    {
        AttackLog.Clear();
    }

    [RelayCommand]
    private void SaveCurrentSession()
    {
        if (CurrentAnalysis == null || CurrentProfile == null || string.IsNullOrEmpty(WalletFilePath))
        {
            AddLog("No active session to save.");
            return;
        }

        var hashFile = WalletQueue.FirstOrDefault(w => w.FilePath == WalletFilePath)?.HashFile;
        if (string.IsNullOrEmpty(hashFile))
        {
            AddLog("Error: No hash file found for current wallet.");
            return;
        }

        CurrentSession = SessionService.CreateSession(WalletFilePath, CurrentAnalysis, CurrentProfile, hashFile);
        SessionService.SaveSession(CurrentSession);
        LoadSavedSessions();
        AddLog($"Session saved: {CurrentSession.Name}");
    }

    [RelayCommand]
    private void LoadSession(SavedSession? session)
    {
        if (session == null) return;

        CurrentSession = session;

        if (!string.IsNullOrEmpty(session.WalletPath) && File.Exists(session.WalletPath))
        {
            WalletFilePath = session.WalletPath;
        }

        CurrentProfile = session.Profile;
        UpdateSuccessProbability();

        AddLog($"Session loaded: {session.Name}");
        AddLog($"State: {session.State}, Phase: {session.CurrentPhaseIndex + 1}/{session.Profile?.Phases.Count ?? 0}");

        if (session.State == SessionState.Paused)
        {
            AddLog("Resuming from paused state...");
        }
    }

    [RelayCommand]
    private void DeleteSession(SavedSession? session)
    {
        if (session == null) return;

        SessionService.DeleteSession(session.Id);
        SavedSessions.Remove(session);
        HasSavedSessions = SavedSessions.Count > 0;
        AddLog($"Session deleted: {session.Name}");
    }

    [RelayCommand]
    private void RefreshSessions()
    {
        LoadSavedSessions();
        AddLog("Sessions refreshed.");
    }

    public void UpdateGpuInfo(DeviceInfo device)
    {
        Application.Current.Dispatcher.Invoke(() =>
        {
            GpuTemperature = device.Temp?.ToString() ?? "--";
            GpuUtilization = device.Util?.ToString() ?? "--";
            GpuSpeed = FormatSpeed(device.Speed);
        });
    }

    private void UpdateQueueStatus()
    {
        var ready = 0;
        var pending = 0;
        var completed = 0;

        foreach (var w in WalletQueue)
        {
            switch (w.Status)
            {
                case QueueStatus.Ready:
                    ready++;
                    break;
                case QueueStatus.Pending:
                case QueueStatus.Analyzing:
                    pending++;
                    break;
                case QueueStatus.Completed:
                    completed++;
                    break;
            }
        }

        QueueStatusText = $"{WalletQueue.Count} wallets ({ready} ready, {pending} pending, {completed} done)";
    }

    private void AddLog(string message)
    {
        Application.Current.Dispatcher.Invoke(() =>
        {
            AttackLog.Add($"[{DateTime.Now:HH:mm:ss}] {message}");
        });
    }

    private static async Task<string> CreateTempHashFile(string hash)
    {
        var tempPath = Path.Combine(Path.GetTempPath(), $"wallet_hash_{Guid.NewGuid():N}.txt");
        await File.WriteAllTextAsync(tempPath, hash);
        return tempPath;
    }

    private static string FormatSpeed(long speed)
    {
        if (speed >= 1_000_000_000)
            return $"{speed / 1_000_000_000.0:F2} GH/s";
        if (speed >= 1_000_000)
            return $"{speed / 1_000_000.0:F2} MH/s";
        if (speed >= 1_000)
            return $"{speed / 1_000.0:F2} kH/s";
        return $"{speed} H/s";
    }
}
