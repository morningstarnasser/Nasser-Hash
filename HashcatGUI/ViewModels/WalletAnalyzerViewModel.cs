using System;
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

public partial class WalletAnalyzerViewModel : ViewModelBase
{
    [ObservableProperty]
    private string _walletFilePath = string.Empty;

    [ObservableProperty]
    private bool _isAnalyzing;

    [ObservableProperty]
    private bool _hasAnalysis;

    [ObservableProperty]
    private WalletAnalysis? _analysis;

    [ObservableProperty]
    private string _statusMessage = "Select a wallet.dat file to analyze";

    [ObservableProperty]
    private string? _selectedAddress;

    // Display properties for UI binding
    public string IterationCountDisplay => Analysis != null ? $"{Analysis.IterationCount:N0}" : "--";
    public string FileSizeDisplay => Analysis != null ? FormatFileSize(Analysis.FileSize) : "--";
    public string FileCreatedDisplay => Analysis?.FileCreated?.ToString("yyyy-MM-dd HH:mm:ss") ?? "--";
    public string FileModifiedDisplay => Analysis?.FileModified?.ToString("yyyy-MM-dd HH:mm:ss") ?? "--";
    public string KeyCountDisplay => Analysis != null ? Analysis.KeyCount.ToString() : "--";
    public string AddressCountDisplay => Analysis != null ? Analysis.AddressCount.ToString() : "--";
    public string SaltDisplay => Analysis?.Salt ?? "--";
    public string EraDisplay => Analysis?.Recommendations?.EraDescription ?? "--";
    public string YearRangeDisplay => Analysis?.EstimatedYearRange ?? "--";
    public string PasswordStyleDisplay => Analysis?.Recommendations?.PasswordStyleDescription ?? "--";
    public string ComplexityScoreDisplay => Analysis != null ? $"{Analysis.Recommendations.ComplexityScore:F1} / 10" : "--";
    public string MinLengthDisplay => Analysis != null ? Analysis.Recommendations.EstimatedMinLength.ToString() : "--";
    public string MaxLengthDisplay => Analysis != null ? Analysis.Recommendations.EstimatedMaxLength.ToString() : "--";
    public string AttackRecommendation => Analysis?.Recommendations?.AttackStrategyRecommendation ?? "--";
    public string HashcatHashDisplay => Analysis?.HashcatHash ?? "--";

    // Complexity bar width (0-100%)
    public double ComplexityBarWidth => Analysis != null ? Analysis.Recommendations.ComplexityScore * 10 : 0;

    // Has addresses to show
    public bool HasAddresses => Analysis?.Addresses?.Count > 0;

    // Color based on complexity
    public string ComplexityColor => Analysis != null ? GetComplexityColor(Analysis.Recommendations.ComplexityScore) : "#808080";

    // Era icon
    public string EraIcon => Analysis?.EstimatedEra switch
    {
        WalletEra.VeryOld => "ClockTimeFour",
        WalletEra.Old => "ClockTimeThree",
        WalletEra.Middle => "ClockTimeTwoThirty",
        WalletEra.Recent => "ClockTimeTwo",
        WalletEra.Modern => "ClockTimeOne",
        WalletEra.Current => "Clock",
        _ => "HelpCircle"
    };

    partial void OnAnalysisChanged(WalletAnalysis? value)
    {
        OnPropertyChanged(nameof(IterationCountDisplay));
        OnPropertyChanged(nameof(FileSizeDisplay));
        OnPropertyChanged(nameof(FileCreatedDisplay));
        OnPropertyChanged(nameof(FileModifiedDisplay));
        OnPropertyChanged(nameof(KeyCountDisplay));
        OnPropertyChanged(nameof(AddressCountDisplay));
        OnPropertyChanged(nameof(SaltDisplay));
        OnPropertyChanged(nameof(EraDisplay));
        OnPropertyChanged(nameof(YearRangeDisplay));
        OnPropertyChanged(nameof(PasswordStyleDisplay));
        OnPropertyChanged(nameof(ComplexityScoreDisplay));
        OnPropertyChanged(nameof(MinLengthDisplay));
        OnPropertyChanged(nameof(MaxLengthDisplay));
        OnPropertyChanged(nameof(AttackRecommendation));
        OnPropertyChanged(nameof(HashcatHashDisplay));
        OnPropertyChanged(nameof(ComplexityBarWidth));
        OnPropertyChanged(nameof(ComplexityColor));
        OnPropertyChanged(nameof(EraIcon));
        OnPropertyChanged(nameof(HasAddresses));
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
            AnalyzeWalletCommand.Execute(null);
        }
    }

    [RelayCommand]
    private async Task AnalyzeWallet()
    {
        if (string.IsNullOrWhiteSpace(WalletFilePath) || !File.Exists(WalletFilePath))
        {
            StatusMessage = "Please select a valid wallet.dat file";
            return;
        }

        IsAnalyzing = true;
        HasAnalysis = false;
        StatusMessage = "Analyzing wallet...";

        try
        {
            Analysis = await WalletAnalyzerService.AnalyzeWalletAsync(WalletFilePath);

            if (Analysis.IsValid)
            {
                HasAnalysis = true;
                StatusMessage = $"Analysis complete! Wallet from {Analysis.EstimatedYearRange}";
            }
            else
            {
                StatusMessage = $"Analysis failed: {Analysis.ErrorMessage}";
            }
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error: {ex.Message}";
        }
        finally
        {
            IsAnalyzing = false;
        }
    }

    [RelayCommand]
    private void CopyHash()
    {
        if (Analysis != null && !string.IsNullOrEmpty(Analysis.HashcatHash))
        {
            Clipboard.SetText(Analysis.HashcatHash);
            StatusMessage = "Hash copied to clipboard!";
        }
    }

    [RelayCommand]
    private async Task SaveHashToFile()
    {
        if (Analysis == null || string.IsNullOrEmpty(Analysis.HashcatHash))
            return;

        var dialog = new SaveFileDialog
        {
            Title = "Save Hash File",
            Filter = "Text Files (*.txt)|*.txt|Hash Files (*.hash)|*.hash|All Files (*.*)|*.*",
            DefaultExt = ".txt",
            FileName = $"wallet_hash_{DateTime.Now:yyyyMMdd_HHmmss}.txt"
        };

        if (dialog.ShowDialog() == true)
        {
            await File.WriteAllTextAsync(dialog.FileName, Analysis.HashcatHash);
            StatusMessage = $"Hash saved to {Path.GetFileName(dialog.FileName)}";
        }
    }

    [RelayCommand]
    private async Task GenerateTokenFile()
    {
        if (Analysis == null)
            return;

        var dialog = new SaveFileDialog
        {
            Title = "Save Token File for BTCRecover",
            Filter = "Text Files (*.txt)|*.txt|All Files (*.*)|*.*",
            DefaultExt = ".txt",
            FileName = $"tokens_{DateTime.Now:yyyyMMdd_HHmmss}.txt"
        };

        if (dialog.ShowDialog() == true)
        {
            var content = GenerateTokenFileContent();
            await File.WriteAllTextAsync(dialog.FileName, content);
            StatusMessage = $"Token file saved to {Path.GetFileName(dialog.FileName)}";
        }
    }

    [RelayCommand]
    private void CopySelectedAddress()
    {
        if (!string.IsNullOrEmpty(SelectedAddress))
        {
            Clipboard.SetText(SelectedAddress);
            StatusMessage = $"Address copied: {SelectedAddress}";
        }
    }

    [RelayCommand]
    private void CopyAllAddresses()
    {
        if (Analysis?.Addresses?.Count > 0)
        {
            var allAddresses = string.Join(Environment.NewLine, Analysis.Addresses);
            Clipboard.SetText(allAddresses);
            StatusMessage = $"{Analysis.Addresses.Count} addresses copied to clipboard!";
        }
    }

    [RelayCommand]
    private void CopyRecommendations()
    {
        if (Analysis == null)
            return;

        var text = $"""
            WALLET ANALYSIS REPORT
            ======================
            File: {Analysis.FileName}
            Era: {Analysis.Recommendations.EraDescription}
            Year Range: {Analysis.EstimatedYearRange}

            ENCRYPTION DETAILS
            ------------------
            Iteration Count: {Analysis.IterationCount:N0}
            Salt: {Analysis.Salt}
            Keys Found: {Analysis.KeyCount}

            PASSWORD ANALYSIS
            -----------------
            Complexity Score: {Analysis.Recommendations.ComplexityScore:F1}/10
            Estimated Length: {Analysis.Recommendations.EstimatedMinLength}-{Analysis.Recommendations.EstimatedMaxLength} characters

            Style: {Analysis.Recommendations.PasswordStyleDescription}

            LIKELY PATTERNS:
            {string.Join("\n", Analysis.Recommendations.LikelyPatterns.Select(p => $"  - {p}"))}

            SUGGESTED MASKS:
            {string.Join("\n", Analysis.Recommendations.SuggestedMasks.Select(m => $"  - {m}"))}

            RECOMMENDED WORDLISTS:
            {string.Join("\n", Analysis.Recommendations.RecommendedWordlists.Select(w => $"  - {w}"))}

            RECOMMENDED RULES:
            {string.Join("\n", Analysis.Recommendations.RecommendedRules.Select(r => $"  - {r}"))}

            ATTACK STRATEGY:
            {Analysis.Recommendations.AttackStrategyRecommendation}

            HASHCAT HASH:
            {Analysis.HashcatHash}
            """;

        Clipboard.SetText(text);
        StatusMessage = "Full analysis report copied to clipboard!";
    }

    private string GenerateTokenFileContent()
    {
        if (Analysis == null)
            return string.Empty;

        var lines = new System.Collections.Generic.List<string>
        {
            "# BTCRecover Token File",
            $"# Generated for wallet from {Analysis.EstimatedYearRange}",
            $"# Iteration count: {Analysis.IterationCount}",
            $"# Complexity score: {Analysis.Recommendations.ComplexityScore}/10",
            "",
            "# Common base words for this era"
        };

        // Add era-specific tokens
        switch (Analysis.EstimatedEra)
        {
            case WalletEra.VeryOld:
            case WalletEra.Old:
                lines.AddRange(new[]
                {
                    "password Password PASSWORD",
                    "bitcoin Bitcoin BITCOIN",
                    "satoshi Satoshi SATOSHI",
                    "wallet Wallet WALLET",
                    "123 1234 12345 123456",
                    "qwerty QWERTY",
                    "abc123"
                });
                break;

            case WalletEra.Middle:
                lines.AddRange(new[]
                {
                    "bitcoin Bitcoin BITCOIN",
                    "blockchain Blockchain BLOCKCHAIN",
                    "crypto Crypto CRYPTO",
                    "wallet Wallet WALLET",
                    "2014 2015 2016 2017",
                    "! @ # $"
                });
                break;

            case WalletEra.Recent:
                lines.AddRange(new[]
                {
                    "hodl HODL",
                    "moon Moon MOON",
                    "btc BTC",
                    "crypto Crypto CRYPTO",
                    "lambo Lambo",
                    "2018 2019 2020",
                    "! @ # $ %"
                });
                break;

            default:
                lines.AddRange(new[]
                {
                    "bitcoin Bitcoin BITCOIN",
                    "wallet Wallet WALLET",
                    "crypto Crypto CRYPTO",
                    "! @ # $ %"
                });
                break;
        }

        lines.Add("");
        lines.Add("# Add your personal tokens below (names, dates, etc.)");
        lines.Add("# ^token means required at start");
        lines.Add("# token$ means required at end");
        lines.Add("# + between tokens means required anchor");

        return string.Join(Environment.NewLine, lines);
    }

    private static string FormatFileSize(long bytes)
    {
        if (bytes >= 1_000_000)
            return $"{bytes / 1_000_000.0:F2} MB";
        if (bytes >= 1_000)
            return $"{bytes / 1_000.0:F2} KB";
        return $"{bytes} bytes";
    }

    private static string GetComplexityColor(double score)
    {
        return score switch
        {
            <= 3 => "#4CAF50",  // Green - Easy
            <= 5 => "#8BC34A",  // Light Green
            <= 6 => "#FFC107",  // Yellow
            <= 7 => "#FF9800",  // Orange
            <= 8 => "#FF5722",  // Deep Orange
            _ => "#F44336"      // Red - Very Hard
        };
    }
}
