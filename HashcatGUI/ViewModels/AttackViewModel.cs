using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HashcatGUI.Models;
using Microsoft.Win32;

namespace HashcatGUI.ViewModels;

public partial class AttackViewModel : ViewModelBase
{
    private MainViewModel? _mainViewModel;

    public void SetMainViewModel(MainViewModel mainViewModel)
    {
        _mainViewModel = mainViewModel;
    }

    // Basic Settings
    [ObservableProperty]
    private string _hashFilePath = string.Empty;

    [ObservableProperty]
    private HashMode? _selectedHashMode;

    [ObservableProperty]
    private AttackMode? _selectedAttackMode;

    [ObservableProperty]
    private string _hashModeSearchText = string.Empty;

    // Wordlist Settings
    [ObservableProperty]
    private string _wordlistPath = string.Empty;

    [ObservableProperty]
    private string _secondWordlistPath = string.Empty;

    // Mask Settings
    [ObservableProperty]
    private string _mask = "?a?a?a?a?a?a?a?a";

    [ObservableProperty]
    private string _customCharset1 = string.Empty;

    [ObservableProperty]
    private string _customCharset2 = string.Empty;

    [ObservableProperty]
    private string _customCharset3 = string.Empty;

    [ObservableProperty]
    private string _customCharset4 = string.Empty;

    [ObservableProperty]
    private bool _incrementMode;

    [ObservableProperty]
    private int _incrementMin = 1;

    [ObservableProperty]
    private int _incrementMax = 8;

    // Rules
    [ObservableProperty]
    private RuleFile? _selectedRule;

    // Performance
    [ObservableProperty]
    private WorkloadProfile? _selectedWorkloadProfile;

    [ObservableProperty]
    private bool _useOptimizedKernels = true;

    [ObservableProperty]
    private string _selectedDevices = string.Empty;

    [ObservableProperty]
    private int _temperatureLimit = 90;

    [ObservableProperty]
    private int? _runtimeLimit;

    // Output
    [ObservableProperty]
    private string _outputFilePath = string.Empty;

    [ObservableProperty]
    private OutputFormat? _selectedOutputFormat;

    [ObservableProperty]
    private bool _disablePotfile;

    // Advanced
    [ObservableProperty]
    private bool _forceMode;

    [ObservableProperty]
    private bool _loopbackMode;

    [ObservableProperty]
    private bool _keepGuessing;

    [ObservableProperty]
    private bool _slowCandidates;

    [ObservableProperty]
    private bool _markovDisable = true; // Default to disabled - hashcat.hcstat2 often missing

    [ObservableProperty]
    private int? _markovThreshold;

    // UI State
    [ObservableProperty]
    private bool _showWordlistSection;

    [ObservableProperty]
    private bool _showMaskSection;

    [ObservableProperty]
    private bool _showSecondWordlistSection;

    [ObservableProperty]
    private bool _showRulesSection;

    [ObservableProperty]
    private string _commandPreview = string.Empty;

    [ObservableProperty]
    private bool _canStartAttack;

    // Terminal State
    [ObservableProperty]
    private bool _isAttackRunning;

    [ObservableProperty]
    private bool _isAttackPaused;

    [ObservableProperty]
    private bool _autoScrollTerminal = true;

    [ObservableProperty]
    private string _currentSpeed = "0 H/s";

    [ObservableProperty]
    private double _currentProgress;

    [ObservableProperty]
    private string _currentEta = "--:--:--";

    [ObservableProperty]
    private int _crackedCount;

    [ObservableProperty]
    private string _walletImportStatus = string.Empty;

    public ObservableCollection<string> TerminalOutput { get; } = new();

    public ObservableCollection<HashMode> FilteredHashModes { get; } = new();
    public ObservableCollection<HashMode> AllHashModes { get; } = new();
    public ObservableCollection<AttackMode> AttackModes { get; } = new();
    public ObservableCollection<RuleFile> AvailableRules { get; } = new();
    public ObservableCollection<RuleFile> SelectedRules { get; } = new();
    public ObservableCollection<WorkloadProfile> WorkloadProfiles { get; } = new();
    public ObservableCollection<OutputFormat> OutputFormats { get; } = new();
    public ObservableCollection<CharsetInfo> BuiltInCharsets { get; } = new();

    public AttackViewModel()
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

        // Load workload profiles
        foreach (var profile in WorkloadProfile.GetAll())
        {
            WorkloadProfiles.Add(profile);
        }

        // Load output formats
        foreach (var format in OutputFormat.GetAll())
        {
            OutputFormats.Add(format);
        }

        // Load charsets
        foreach (var charset in CharsetInfo.GetBuiltIn())
        {
            BuiltInCharsets.Add(charset);
        }

        // Set defaults
        SelectedAttackMode = AttackModes.FirstOrDefault();
        SelectedWorkloadProfile = WorkloadProfiles.FirstOrDefault(p => p.Id == 2);
        SelectedOutputFormat = OutputFormats.FirstOrDefault(f => f.Id == 2);

        // Load available rules
        LoadAvailableRules();

        UpdateCommandPreview();

        // Subscribe to hashcat events
        App.Hashcat.OutputReceived += OnHashcatOutput;
        App.Hashcat.ErrorReceived += OnHashcatError;
        App.Hashcat.StatusUpdated += OnHashcatStatusUpdated;
        App.Hashcat.HashCracked += OnHashCracked;
        App.Hashcat.ProcessExited += OnHashcatExited;
    }

    partial void OnHashModeSearchTextChanged(string value)
    {
        FilterHashModes(value);
    }

    partial void OnSelectedAttackModeChanged(AttackMode? value)
    {
        UpdateSectionVisibility();
        UpdateCommandPreview();
        ValidateCanStart();
    }

    partial void OnHashFilePathChanged(string value)
    {
        UpdateCommandPreview();
        ValidateCanStart();
    }

    partial void OnSelectedHashModeChanged(HashMode? value)
    {
        UpdateCommandPreview();
        ValidateCanStart();
    }

    partial void OnWordlistPathChanged(string value)
    {
        UpdateCommandPreview();
        ValidateCanStart();
    }

    partial void OnSecondWordlistPathChanged(string value)
    {
        UpdateCommandPreview();
        ValidateCanStart();
    }

    partial void OnMaskChanged(string value)
    {
        UpdateCommandPreview();
        ValidateCanStart();
    }

    partial void OnIncrementModeChanged(bool value)
    {
        UpdateCommandPreview();
    }

    partial void OnIncrementMinChanged(int value)
    {
        UpdateCommandPreview();
    }

    partial void OnIncrementMaxChanged(int value)
    {
        UpdateCommandPreview();
    }

    partial void OnCustomCharset1Changed(string value) => UpdateCommandPreview();
    partial void OnCustomCharset2Changed(string value) => UpdateCommandPreview();
    partial void OnCustomCharset3Changed(string value) => UpdateCommandPreview();
    partial void OnCustomCharset4Changed(string value) => UpdateCommandPreview();
    partial void OnSelectedWorkloadProfileChanged(WorkloadProfile? value) => UpdateCommandPreview();
    partial void OnUseOptimizedKernelsChanged(bool value) => UpdateCommandPreview();
    partial void OnOutputFilePathChanged(string value) => UpdateCommandPreview();
    partial void OnSelectedOutputFormatChanged(OutputFormat? value) => UpdateCommandPreview();
    partial void OnDisablePotfileChanged(bool value) => UpdateCommandPreview();
    partial void OnForceModeChanged(bool value) => UpdateCommandPreview();
    partial void OnLoopbackModeChanged(bool value) => UpdateCommandPreview();
    partial void OnKeepGuessingChanged(bool value) => UpdateCommandPreview();
    partial void OnSlowCandidatesChanged(bool value) => UpdateCommandPreview();
    partial void OnMarkovDisableChanged(bool value) => UpdateCommandPreview();
    partial void OnRuntimeLimitChanged(int? value) => UpdateCommandPreview();
    partial void OnTemperatureLimitChanged(int value) => UpdateCommandPreview();

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

    private void UpdateSectionVisibility()
    {
        if (SelectedAttackMode == null)
        {
            ShowWordlistSection = false;
            ShowSecondWordlistSection = false;
            ShowMaskSection = false;
            ShowRulesSection = false;
            return;
        }

        ShowWordlistSection = SelectedAttackMode.RequiresWordlist;
        ShowSecondWordlistSection = SelectedAttackMode.RequiresSecondWordlist;
        ShowMaskSection = SelectedAttackMode.RequiresMask;
        ShowRulesSection = SelectedAttackMode.SupportsRules;
    }

    private void ValidateCanStart()
    {
        if (string.IsNullOrEmpty(HashFilePath) || !File.Exists(HashFilePath))
        {
            CanStartAttack = false;
            return;
        }

        if (SelectedHashMode == null || SelectedAttackMode == null)
        {
            CanStartAttack = false;
            return;
        }

        // Check attack mode requirements
        if (SelectedAttackMode.RequiresWordlist && (string.IsNullOrEmpty(WordlistPath) || !File.Exists(WordlistPath)))
        {
            CanStartAttack = false;
            return;
        }

        if (SelectedAttackMode.RequiresSecondWordlist && (string.IsNullOrEmpty(SecondWordlistPath) || !File.Exists(SecondWordlistPath)))
        {
            CanStartAttack = false;
            return;
        }

        if (SelectedAttackMode.RequiresMask && string.IsNullOrEmpty(Mask))
        {
            CanStartAttack = false;
            return;
        }

        CanStartAttack = true;
    }

    private void LoadAvailableRules()
    {
        AvailableRules.Clear();
        var rulesPath = App.Settings.Settings.DefaultRulesPath;

        if (!string.IsNullOrEmpty(rulesPath) && Directory.Exists(rulesPath))
        {
            var rules = App.Hashcat.GetAvailableRules(rulesPath);
            foreach (var rule in rules)
            {
                AvailableRules.Add(rule);
            }
        }
    }

    private void UpdateCommandPreview()
    {
        if (SelectedHashMode == null || SelectedAttackMode == null)
        {
            CommandPreview = "Configure attack settings to preview command...";
            return;
        }

        var config = BuildConfig();
        CommandPreview = $"hashcat.exe {App.Hashcat.BuildCommandLine(config)}";
    }

    private HashcatConfig BuildConfig()
    {
        return new HashcatConfig
        {
            AttackMode = SelectedAttackMode?.Id ?? 0,
            HashMode = SelectedHashMode?.Id ?? 0,
            HashFile = HashFilePath,
            Wordlist = WordlistPath,
            SecondWordlist = SecondWordlistPath,
            Mask = Mask,
            RuleFiles = SelectedRules.Select(r => r.FullPath).ToList(),
            CustomCharset1 = CustomCharset1,
            CustomCharset2 = CustomCharset2,
            CustomCharset3 = CustomCharset3,
            CustomCharset4 = CustomCharset4,
            IncrementMode = IncrementMode,
            IncrementMin = IncrementMin,
            IncrementMax = IncrementMax,
            WorkloadProfile = SelectedWorkloadProfile?.Id ?? 2,
            Devices = SelectedDevices,
            OptimizedKernels = UseOptimizedKernels,
            OutputFile = OutputFilePath,
            OutputFormat = SelectedOutputFormat?.Id ?? 2,
            DisablePotfile = DisablePotfile,
            ForceMode = ForceMode,
            StatusJson = true,
            StatusTimer = 5,
            RuntimeLimit = RuntimeLimit,
            TempAbort = TemperatureLimit,
            MarkovDisable = MarkovDisable,
            MarkovThreshold = MarkovThreshold,
            LoopbackMode = LoopbackMode,
            KeepGuessing = KeepGuessing,
            SlowCandidates = SlowCandidates
        };
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
    private void BrowseWordlist()
    {
        var dialog = new OpenFileDialog
        {
            Title = "Select Wordlist",
            Filter = "Wordlist Files (*.txt;*.dict;*.lst)|*.txt;*.dict;*.lst|All Files (*.*)|*.*",
            FilterIndex = 1,
            InitialDirectory = App.Settings.Settings.DefaultWordlistPath
        };

        if (dialog.ShowDialog() == true)
        {
            WordlistPath = dialog.FileName;
            App.Settings.AddRecentWordlist(dialog.FileName);
        }
    }

    [RelayCommand]
    private void BrowseSecondWordlist()
    {
        var dialog = new OpenFileDialog
        {
            Title = "Select Second Wordlist",
            Filter = "Wordlist Files (*.txt;*.dict;*.lst)|*.txt;*.dict;*.lst|All Files (*.*)|*.*",
            FilterIndex = 1,
            InitialDirectory = App.Settings.Settings.DefaultWordlistPath
        };

        if (dialog.ShowDialog() == true)
        {
            SecondWordlistPath = dialog.FileName;
        }
    }

    [RelayCommand]
    private void BrowseMaskFile()
    {
        var dialog = new OpenFileDialog
        {
            Title = "Select Mask File",
            Filter = "Mask Files (*.hcmask)|*.hcmask|All Files (*.*)|*.*",
            FilterIndex = 1,
            InitialDirectory = App.Settings.Settings.DefaultMasksPath
        };

        if (dialog.ShowDialog() == true)
        {
            Mask = dialog.FileName;
        }
    }

    [RelayCommand]
    private void BrowseOutputFile()
    {
        var dialog = new SaveFileDialog
        {
            Title = "Select Output File",
            Filter = "Text Files (*.txt)|*.txt|All Files (*.*)|*.*",
            FilterIndex = 1,
            InitialDirectory = App.Settings.Settings.DefaultOutputPath
        };

        if (dialog.ShowDialog() == true)
        {
            OutputFilePath = dialog.FileName;
        }
    }

    [RelayCommand]
    private void AddRule(RuleFile rule)
    {
        if (!SelectedRules.Contains(rule))
        {
            SelectedRules.Add(rule);
            UpdateCommandPreview();
        }
    }

    [RelayCommand]
    private void RemoveRule(RuleFile rule)
    {
        SelectedRules.Remove(rule);
        UpdateCommandPreview();
    }

    [RelayCommand]
    private void InsertCharset(string charset)
    {
        Mask += charset;
    }

    [RelayCommand]
    private async Task StartAttack()
    {
        if (!CanStartAttack)
            return;

        try
        {
            IsLoading = true;
            StatusMessage = "Starting attack...";

            // Clear terminal and reset stats
            TerminalOutput.Clear();
            CurrentSpeed = "0 H/s";
            CurrentProgress = 0;
            CurrentEta = "--:--:--";
            CrackedCount = 0;

            AddTerminalLine("=== HASHCAT ATTACK STARTED ===");
            AddTerminalLine($"Hash File: {HashFilePath}");
            AddTerminalLine($"Hash Mode: {SelectedHashMode?.DisplayName}");
            AddTerminalLine($"Attack Mode: {SelectedAttackMode?.Name}");
            AddTerminalLine("");

            var config = BuildConfig();
            if (_mainViewModel != null)
            {
                _mainViewModel.IsHashcatRunning = true;
                _mainViewModel.HashcatStatus = "Starting...";
            }

            var success = await App.Hashcat.StartAsync(config);

            if (success)
            {
                IsAttackRunning = true;
                IsAttackPaused = false;
                StatusMessage = "Attack started successfully";
                AddTerminalLine("[INFO] Attack started successfully");
                if (_mainViewModel != null)
                    _mainViewModel.HashcatStatus = "Running";
            }
            else
            {
                StatusMessage = "Failed to start attack";
                AddTerminalLine("[ERROR] Failed to start attack");
                if (_mainViewModel != null)
                    _mainViewModel.IsHashcatRunning = false;
            }
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error: {ex.Message}";
            AddTerminalLine($"[ERROR] {ex.Message}");
            if (_mainViewModel != null)
                _mainViewModel.IsHashcatRunning = false;
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private void CopyCommand()
    {
        try
        {
            System.Windows.Clipboard.SetText(CommandPreview);
            StatusMessage = "Command copied to clipboard";
        }
        catch
        {
            StatusMessage = "Failed to copy command";
        }
    }

    // ===== TERMINAL COMMANDS =====

    [RelayCommand]
    private void ClearTerminal()
    {
        TerminalOutput.Clear();
    }

    [RelayCommand]
    private void CopyTerminal()
    {
        try
        {
            var text = string.Join(Environment.NewLine, TerminalOutput);
            System.Windows.Clipboard.SetText(text);
            StatusMessage = "Terminal output copied to clipboard";
        }
        catch
        {
            StatusMessage = "Failed to copy terminal output";
        }
    }

    [RelayCommand]
    private void PauseAttack()
    {
        App.Hashcat.Pause();
        IsAttackPaused = true;
        AddTerminalLine("[PAUSED] Attack paused by user");
        StatusMessage = "Attack paused";
    }

    [RelayCommand]
    private void ResumeAttack()
    {
        App.Hashcat.Resume();
        IsAttackPaused = false;
        AddTerminalLine("[RESUMED] Attack resumed");
        StatusMessage = "Attack resumed";
    }

    [RelayCommand]
    private void StopAttack()
    {
        App.Hashcat.Stop();
        IsAttackRunning = false;
        IsAttackPaused = false;
        AddTerminalLine("[STOPPED] Attack stopped by user");
        StatusMessage = "Attack stopped";
    }

    // ===== WALLET IMPORT =====

    [RelayCommand]
    private async Task ImportWallet()
    {
        var dialog = new OpenFileDialog
        {
            Title = "Import Bitcoin Wallet",
            Filter = "Bitcoin Wallet (*.dat)|*.dat|All Files (*.*)|*.*",
            FilterIndex = 1
        };

        if (dialog.ShowDialog() != true)
            return;

        try
        {
            WalletImportStatus = "Extracting hash from wallet...";
            StatusMessage = "Importing wallet...";

            var walletPath = dialog.FileName;
            var hashOutputPath = Path.Combine(
                Path.GetDirectoryName(walletPath) ?? Environment.CurrentDirectory,
                Path.GetFileNameWithoutExtension(walletPath) + ".hash");

            // Use bitcoin2john or hashcat's built-in extractor
            var extracted = await ExtractWalletHashAsync(walletPath, hashOutputPath);

            if (extracted)
            {
                HashFilePath = hashOutputPath;

                // Auto-select Bitcoin wallet hash mode (11300)
                var bitcoinMode = AllHashModes.FirstOrDefault(m => m.Id == 11300);
                if (bitcoinMode != null)
                {
                    SelectedHashMode = bitcoinMode;
                }

                WalletImportStatus = $"Hash extracted to: {Path.GetFileName(hashOutputPath)}";
                StatusMessage = "Wallet imported successfully! Hash mode set to Bitcoin/Litecoin wallet.dat";
                AddTerminalLine($"[WALLET] Imported: {walletPath}");
                AddTerminalLine($"[WALLET] Hash saved to: {hashOutputPath}");
            }
            else
            {
                WalletImportStatus = "Failed to extract hash - using wallet directly";
                HashFilePath = walletPath;

                // Try to set Bitcoin mode anyway
                var bitcoinMode = AllHashModes.FirstOrDefault(m => m.Id == 11300);
                if (bitcoinMode != null)
                {
                    SelectedHashMode = bitcoinMode;
                }

                StatusMessage = "Wallet loaded directly (extraction failed, hashcat will try to extract)";
            }
        }
        catch (Exception ex)
        {
            WalletImportStatus = $"Error: {ex.Message}";
            StatusMessage = $"Import failed: {ex.Message}";
        }
    }

    private async Task<bool> ExtractWalletHashAsync(string walletPath, string outputPath)
    {
        try
        {
            AddTerminalLine($"[WALLET] Processing: {Path.GetFileName(walletPath)}");
            AddTerminalLine($"[WALLET] Output will be: {Path.GetFileName(outputPath)}");

            // First, validate it's a Bitcoin wallet
            if (!await Services.BitcoinWalletExtractor.IsValidWalletAsync(walletPath))
            {
                AddTerminalLine("[WALLET] ERROR: Invalid wallet file - no mkey/ckey markers found");
                AddTerminalLine("[WALLET] This file does not appear to be an encrypted Bitcoin Core wallet");
                return false;
            }

            AddTerminalLine("[WALLET] Valid Bitcoin wallet detected (mkey/ckey markers found)");
            AddTerminalLine("[WALLET] Extracting hash for hashcat mode 11300...");

            // Try to extract hash using our extractor
            var hash = await Services.BitcoinWalletExtractor.ExtractHashAsync(walletPath);

            if (!string.IsNullOrEmpty(hash))
            {
                // Validate hash format
                if (!hash.StartsWith("$bitcoin$"))
                {
                    AddTerminalLine("[WALLET] WARNING: Extracted hash has unexpected format");
                }

                // Write the extracted hash to file
                await File.WriteAllTextAsync(outputPath, hash);

                AddTerminalLine($"[WALLET] SUCCESS! Hash extracted ({hash.Length} chars)");
                AddTerminalLine($"[WALLET] Format: {hash[..Math.Min(60, hash.Length)]}...");
                AddTerminalLine($"[WALLET] Saved to: {outputPath}");
                return true;
            }

            AddTerminalLine("[WALLET] Primary extraction returned null - trying fallback...");

            // Try simple extraction as fallback
            hash = await Services.BitcoinWalletExtractor.ExtractSimpleHashAsync(walletPath);

            if (!string.IsNullOrEmpty(hash))
            {
                await File.WriteAllTextAsync(outputPath, hash);
                AddTerminalLine($"[WALLET] Fallback extraction succeeded: {hash[..Math.Min(60, hash.Length)]}...");
                return true;
            }

            AddTerminalLine("[WALLET] FAILED: Could not extract hash from wallet");
            AddTerminalLine("[WALLET] The wallet encryption format may not be supported");
            AddTerminalLine("[WALLET] Try using an external tool like bitcoin2john.py");
            return false;
        }
        catch (Exception ex)
        {
            AddTerminalLine($"[WALLET] EXCEPTION: {ex.Message}");
            AddTerminalLine($"[WALLET] Stack: {ex.StackTrace?.Split('\n').FirstOrDefault()}");
            return false;
        }
    }

    // ===== HASHCAT EVENT HANDLERS =====

    private void OnHashcatOutput(object? sender, string output)
    {
        System.Windows.Application.Current.Dispatcher.Invoke(() =>
        {
            AddTerminalLine(output);
        });
    }

    private void OnHashcatError(object? sender, string error)
    {
        System.Windows.Application.Current.Dispatcher.Invoke(() =>
        {
            AddTerminalLine($"[ERROR] {error}");
        });
    }

    private void OnHashcatStatusUpdated(object? sender, HashcatStatusJson status)
    {
        System.Windows.Application.Current.Dispatcher.Invoke(() =>
        {
            // Update progress
            if (status.Progress != null && status.Progress.Length >= 2)
            {
                var current = status.Progress[0];
                var total = status.Progress[1];
                CurrentProgress = total > 0 ? (double)current / total * 100 : 0;
            }

            // Update speed
            if (status.Devices != null && status.Devices.Length > 0)
            {
                var totalSpeed = status.Devices.Sum(d => d.Speed);
                CurrentSpeed = FormatSpeed(totalSpeed);
            }

            // Update ETA
            if (status.EstimatedStop > 0)
            {
                var eta = DateTimeOffset.FromUnixTimeSeconds(status.EstimatedStop).LocalDateTime - DateTime.Now;
                CurrentEta = eta > TimeSpan.Zero ? FormatTimeSpan(eta) : "Finishing...";
            }

            // Update cracked count
            if (status.RecoveredHashes != null && status.RecoveredHashes.Length >= 1)
            {
                CrackedCount = status.RecoveredHashes[0];
            }
        });
    }

    private void OnHashCracked(object? sender, CrackedHash hash)
    {
        System.Windows.Application.Current.Dispatcher.Invoke(() =>
        {
            CrackedCount++;
            AddTerminalLine($"[CRACKED] {hash.Hash} : {hash.Password}");
        });
    }

    private void OnHashcatExited(object? sender, int exitCode)
    {
        System.Windows.Application.Current.Dispatcher.Invoke(() =>
        {
            IsAttackRunning = false;
            IsAttackPaused = false;

            var exitMessage = exitCode switch
            {
                0 => "Attack completed - Hash(es) cracked!",
                1 => "Attack exhausted - All combinations tried",
                2 => "Attack aborted by user",
                _ => $"Attack finished (Exit code: {exitCode})"
            };

            AddTerminalLine($"[FINISHED] {exitMessage}");
            StatusMessage = exitMessage;

            if (_mainViewModel != null)
            {
                _mainViewModel.IsHashcatRunning = false;
                _mainViewModel.HashcatStatus = exitMessage;
            }
        });
    }

    private void AddTerminalLine(string line)
    {
        TerminalOutput.Add($"[{DateTime.Now:HH:mm:ss}] {line}");

        // Keep max 1000 lines
        while (TerminalOutput.Count > 1000)
        {
            TerminalOutput.RemoveAt(0);
        }
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
