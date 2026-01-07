using System;
using System.IO;
using System.Text.Json;
using HashcatGUI.Models;

namespace HashcatGUI.Services;

public class SettingsService
{
    private static readonly string SettingsPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "Nasser-Hash",
        "settings.json");

    public AppSettings Settings { get; set; } = new();

    public void Load()
    {
        try
        {
            if (File.Exists(SettingsPath))
            {
                var json = File.ReadAllText(SettingsPath);
                Settings = JsonSerializer.Deserialize<AppSettings>(json) ?? new AppSettings();
            }
            else
            {
                Settings = new AppSettings();
                AutoDetectPaths();
            }
        }
        catch
        {
            Settings = new AppSettings();
            AutoDetectPaths();
        }
    }

    public void Save()
    {
        try
        {
            var directory = Path.GetDirectoryName(SettingsPath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            var json = JsonSerializer.Serialize(Settings, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(SettingsPath, json);
        }
        catch
        {
            // Silently fail
        }
    }

    private void AutoDetectPaths()
    {
        // Try to find hashcat in common locations
        var possiblePaths = new[]
        {
            Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "hashcat-6.2.6"),
            Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "hashcat-6.2.6"),
            @"C:\hashcat-6.2.6",
            @"C:\Program Files\hashcat",
            @"C:\Tools\hashcat-6.2.6",
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Downloads", "btc_test", "hashcat-6.2.6")
        };

        foreach (var path in possiblePaths)
        {
            var exePath = Path.Combine(path, "hashcat.exe");
            if (File.Exists(exePath))
            {
                Settings.HashcatPath = exePath;
                Settings.DefaultWordlistPath = path;
                Settings.DefaultRulesPath = Path.Combine(path, "rules");
                Settings.DefaultMasksPath = Path.Combine(path, "masks");
                Settings.DefaultCharsetsPath = Path.Combine(path, "charsets");
                Settings.PotfilePath = Path.Combine(path, "hashcat.potfile");
                Settings.DefaultOutputPath = path;
                break;
            }
        }
    }

    public void AddRecentHashFile(string path)
    {
        Settings.RecentHashFiles.Remove(path);
        Settings.RecentHashFiles.Insert(0, path);
        if (Settings.RecentHashFiles.Count > 10)
            Settings.RecentHashFiles.RemoveAt(10);
        Save();
    }

    public void AddRecentWordlist(string path)
    {
        Settings.RecentWordlists.Remove(path);
        Settings.RecentWordlists.Insert(0, path);
        if (Settings.RecentWordlists.Count > 10)
            Settings.RecentWordlists.RemoveAt(10);
        Save();
    }

    public void ToggleFavoriteHashMode(string modeId)
    {
        if (Settings.FavoriteHashModes.Contains(modeId))
            Settings.FavoriteHashModes.Remove(modeId);
        else
            Settings.FavoriteHashModes.Add(modeId);
        Save();
    }
}
