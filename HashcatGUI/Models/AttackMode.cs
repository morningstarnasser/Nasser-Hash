using System.Collections.Generic;

namespace HashcatGUI.Models;

public class AttackMode
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Icon { get; set; } = string.Empty;
    public bool RequiresWordlist { get; set; }
    public bool RequiresMask { get; set; }
    public bool RequiresSecondWordlist { get; set; }
    public bool SupportsRules { get; set; }

    public string DisplayName => $"{Name} (Attack Mode {Id})";

    public static List<AttackMode> GetAllModes() => new()
    {
        new()
        {
            Id = 0,
            Name = "Straight",
            Description = "Dictionary attack using wordlists with optional rules",
            Icon = "FormatListBulleted",
            RequiresWordlist = true,
            RequiresMask = false,
            RequiresSecondWordlist = false,
            SupportsRules = true
        },
        new()
        {
            Id = 1,
            Name = "Combination",
            Description = "Combines words from two dictionaries",
            Icon = "VectorCombine",
            RequiresWordlist = true,
            RequiresMask = false,
            RequiresSecondWordlist = true,
            SupportsRules = false
        },
        new()
        {
            Id = 3,
            Name = "Brute-Force",
            Description = "Mask-based brute force attack with custom character sets",
            Icon = "ShieldKey",
            RequiresWordlist = false,
            RequiresMask = true,
            RequiresSecondWordlist = false,
            SupportsRules = false
        },
        new()
        {
            Id = 6,
            Name = "Hybrid Wordlist + Mask",
            Description = "Appends mask patterns to dictionary words",
            Icon = "MergeCells",
            RequiresWordlist = true,
            RequiresMask = true,
            RequiresSecondWordlist = false,
            SupportsRules = false
        },
        new()
        {
            Id = 7,
            Name = "Hybrid Mask + Wordlist",
            Description = "Prepends mask patterns to dictionary words",
            Icon = "TableMergeCells",
            RequiresWordlist = true,
            RequiresMask = true,
            RequiresSecondWordlist = false,
            SupportsRules = false
        },
        new()
        {
            Id = 9,
            Name = "Association",
            Description = "Uses wordlist with hints for WPA/WPA2 attacks",
            Icon = "LinkVariant",
            RequiresWordlist = true,
            RequiresMask = false,
            RequiresSecondWordlist = false,
            SupportsRules = true
        }
    };
}

public class WorkloadProfile
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string RuntimeMs { get; set; } = string.Empty;

    public static List<WorkloadProfile> GetAll() => new()
    {
        new() { Id = 1, Name = "Low", Description = "Minimal desktop impact", RuntimeMs = "2ms" },
        new() { Id = 2, Name = "Default", Description = "Balanced performance", RuntimeMs = "12ms" },
        new() { Id = 3, Name = "High", Description = "Significant system impact", RuntimeMs = "96ms" },
        new() { Id = 4, Name = "Nightmare", Description = "Maximum performance, system unresponsive", RuntimeMs = "480ms" }
    };
}

public class DeviceType
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;

    public static List<DeviceType> GetAll() => new()
    {
        new() { Id = 1, Name = "CPU" },
        new() { Id = 2, Name = "GPU" },
        new() { Id = 3, Name = "FPGA" }
    };
}

public class OutputFormat
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;

    public static List<OutputFormat> GetAll() => new()
    {
        new() { Id = 1, Name = "hash[:salt]", Description = "Hash with optional salt" },
        new() { Id = 2, Name = "plain", Description = "Plain password only" },
        new() { Id = 3, Name = "hex_plain", Description = "Hexadecimal password" },
        new() { Id = 4, Name = "crack_pos", Description = "Hash:Position:Length:Password" },
        new() { Id = 5, Name = "timestamp_absolute", Description = "UNIX timestamp" },
        new() { Id = 6, Name = "timestamp_relative", Description = "Elapsed seconds" }
    };
}
