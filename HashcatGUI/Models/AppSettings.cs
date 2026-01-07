using System.Collections.Generic;

namespace HashcatGUI.Models;

public class AppSettings
{
    public string HashcatPath { get; set; } = string.Empty;
    public string DefaultWordlistPath { get; set; } = string.Empty;
    public string DefaultRulesPath { get; set; } = string.Empty;
    public string DefaultMasksPath { get; set; } = string.Empty;
    public string DefaultCharsetsPath { get; set; } = string.Empty;
    public string DefaultOutputPath { get; set; } = string.Empty;
    public string PotfilePath { get; set; } = string.Empty;
    public int DefaultWorkloadProfile { get; set; } = 2;
    public string DefaultDevices { get; set; } = string.Empty;
    public bool UseOptimizedKernels { get; set; } = true;
    public int DefaultOutputFormat { get; set; } = 2;
    public int StatusUpdateInterval { get; set; } = 5;
    public int TemperatureLimit { get; set; } = 90;
    public bool AutoSaveSession { get; set; } = true;
    public bool ShowNotifications { get; set; } = true;
    public bool DarkMode { get; set; } = true;
    public string Language { get; set; } = "en";
    public List<string> RecentHashFiles { get; set; } = new();
    public List<string> RecentWordlists { get; set; } = new();
    public List<string> FavoriteHashModes { get; set; } = new();
    public WindowState LastWindowState { get; set; } = new();
}

public class WindowState
{
    public double Width { get; set; } = 1400;
    public double Height { get; set; } = 900;
    public double Left { get; set; } = 100;
    public double Top { get; set; } = 100;
    public bool IsMaximized { get; set; }
}
