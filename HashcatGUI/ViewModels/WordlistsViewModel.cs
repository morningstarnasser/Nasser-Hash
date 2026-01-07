using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HashcatGUI.Helpers;
using HashcatGUI.Models;
using Microsoft.Win32;

namespace HashcatGUI.ViewModels;

public partial class WordlistsViewModel : ViewModelBase
{
    [ObservableProperty]
    private string _wordlistSearchPath = string.Empty;

    [ObservableProperty]
    private string _rulesSearchPath = string.Empty;

    [ObservableProperty]
    private WordlistFile? _selectedWordlist;

    [ObservableProperty]
    private RuleFile? _selectedRule;

    [ObservableProperty]
    private string _wordlistPreview = string.Empty;

    [ObservableProperty]
    private string _rulePreview = string.Empty;

    public ObservableCollection<WordlistFile> Wordlists { get; } = new();
    public ObservableCollection<RuleFile> Rules { get; } = new();

    public WordlistsViewModel()
    {
        WordlistSearchPath = App.Settings.Settings.DefaultWordlistPath;
        RulesSearchPath = App.Settings.Settings.DefaultRulesPath;

        LoadWordlists();
        LoadRules();
    }

    partial void OnWordlistSearchPathChanged(string value)
    {
        LoadWordlists();
    }

    partial void OnRulesSearchPathChanged(string value)
    {
        LoadRules();
    }

    partial void OnSelectedWordlistChanged(WordlistFile? value)
    {
        if (value != null)
        {
            LoadWordlistPreview(value.FullPath);
        }
        else
        {
            WordlistPreview = string.Empty;
        }
    }

    partial void OnSelectedRuleChanged(RuleFile? value)
    {
        if (value != null)
        {
            LoadRulePreview(value.FullPath);
        }
        else
        {
            RulePreview = string.Empty;
        }
    }

    private void LoadWordlists()
    {
        Wordlists.Clear();

        if (string.IsNullOrEmpty(WordlistSearchPath) || !Directory.Exists(WordlistSearchPath))
            return;

        var wordlists = App.Hashcat.GetAvailableWordlists(WordlistSearchPath);
        foreach (var wordlist in wordlists)
        {
            Wordlists.Add(wordlist);
        }
    }

    private void LoadRules()
    {
        Rules.Clear();

        if (string.IsNullOrEmpty(RulesSearchPath) || !Directory.Exists(RulesSearchPath))
            return;

        var rules = App.Hashcat.GetAvailableRules(RulesSearchPath);
        foreach (var rule in rules)
        {
            Rules.Add(rule);
        }
    }

    private void LoadWordlistPreview(string path)
    {
        try
        {
            var lines = File.ReadLines(path).Take(100).ToList();
            WordlistPreview = string.Join(Environment.NewLine, lines);

            if (File.ReadLines(path).Skip(100).Any())
            {
                WordlistPreview += Environment.NewLine + "... (truncated)";
            }
        }
        catch (Exception ex)
        {
            WordlistPreview = $"Error loading preview: {ex.Message}";
        }
    }

    private void LoadRulePreview(string path)
    {
        try
        {
            var lines = File.ReadLines(path).Take(50).ToList();
            RulePreview = string.Join(Environment.NewLine, lines);

            if (File.ReadLines(path).Skip(50).Any())
            {
                RulePreview += Environment.NewLine + "... (truncated)";
            }
        }
        catch (Exception ex)
        {
            RulePreview = $"Error loading preview: {ex.Message}";
        }
    }

    [RelayCommand]
    private void BrowseWordlistFolder()
    {
        var folder = DialogHelper.BrowseForFolder("Select Wordlists Folder");
        if (!string.IsNullOrEmpty(folder))
        {
            WordlistSearchPath = folder;
            App.Settings.Settings.DefaultWordlistPath = folder;
            App.Settings.Save();
        }
    }

    [RelayCommand]
    private void BrowseRulesFolder()
    {
        var folder = DialogHelper.BrowseForFolder("Select Rules Folder");
        if (!string.IsNullOrEmpty(folder))
        {
            RulesSearchPath = folder;
            App.Settings.Settings.DefaultRulesPath = folder;
            App.Settings.Save();
        }
    }

    [RelayCommand]
    private void RefreshWordlists()
    {
        LoadWordlists();
    }

    [RelayCommand]
    private void RefreshRules()
    {
        LoadRules();
    }

    [RelayCommand]
    private void OpenWordlistInExplorer()
    {
        if (SelectedWordlist != null && File.Exists(SelectedWordlist.FullPath))
        {
            System.Diagnostics.Process.Start("explorer.exe", $"/select,\"{SelectedWordlist.FullPath}\"");
        }
    }

    [RelayCommand]
    private void OpenRuleInExplorer()
    {
        if (SelectedRule != null && File.Exists(SelectedRule.FullPath))
        {
            System.Diagnostics.Process.Start("explorer.exe", $"/select,\"{SelectedRule.FullPath}\"");
        }
    }

    [RelayCommand]
    private async Task ImportWordlist()
    {
        var dialog = new OpenFileDialog
        {
            Title = "Import Wordlist",
            Filter = "Wordlist Files (*.txt;*.dict;*.lst)|*.txt;*.dict;*.lst|All Files (*.*)|*.*",
            Multiselect = true
        };

        if (dialog.ShowDialog() == true)
        {
            foreach (var file in dialog.FileNames)
            {
                var destPath = Path.Combine(WordlistSearchPath, Path.GetFileName(file));
                if (!File.Exists(destPath))
                {
                    await Task.Run(() => File.Copy(file, destPath));
                }
            }

            LoadWordlists();
            StatusMessage = $"Imported {dialog.FileNames.Length} wordlist(s)";
        }
    }

    [RelayCommand]
    private async Task ImportRule()
    {
        var dialog = new OpenFileDialog
        {
            Title = "Import Rule File",
            Filter = "Rule Files (*.rule)|*.rule|All Files (*.*)|*.*",
            Multiselect = true
        };

        if (dialog.ShowDialog() == true)
        {
            foreach (var file in dialog.FileNames)
            {
                var destPath = Path.Combine(RulesSearchPath, Path.GetFileName(file));
                if (!File.Exists(destPath))
                {
                    await Task.Run(() => File.Copy(file, destPath));
                }
            }

            LoadRules();
            StatusMessage = $"Imported {dialog.FileNames.Length} rule file(s)";
        }
    }
}
