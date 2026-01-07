using System.Collections.Generic;

namespace HashcatGUI.Models;

public class RuleFile
{
    public string Name { get; set; } = string.Empty;
    public string FullPath { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public long Size { get; set; }
    public int? RuleCount { get; set; }

    public string SizeFormatted => FormatSize(Size);

    private static string FormatSize(long bytes)
    {
        if (bytes >= 1_073_741_824)
            return $"{bytes / 1_073_741_824.0:F2} GB";
        if (bytes >= 1_048_576)
            return $"{bytes / 1_048_576.0:F2} MB";
        if (bytes >= 1024)
            return $"{bytes / 1024.0:F2} KB";
        return $"{bytes} B";
    }

    public static Dictionary<string, string> KnownDescriptions => new()
    {
        { "best64.rule", "Best 64 rules for general password cracking" },
        { "combinator.rule", "Combinator attack rules" },
        { "d3ad0ne.rule", "D3ad0ne's comprehensive rule set" },
        { "dive.rule", "Large comprehensive rule set (99k+ rules)" },
        { "generated.rule", "Generated random rules" },
        { "generated2.rule", "Generated random rules v2" },
        { "Incisive-leetspeak.rule", "Leetspeak character substitutions" },
        { "InsidePro-HashManager.rule", "InsidePro HashManager compatible rules" },
        { "InsidePro-PasswordsPro.rule", "InsidePro PasswordsPro compatible rules" },
        { "leetspeak.rule", "Basic leetspeak transformations" },
        { "OneRuleToRuleThemAll.rule", "Comprehensive combined rule set" },
        { "oscommerce.rule", "osCommerce specific rules" },
        { "rockyou-30000.rule", "Top 30k rules optimized for RockYou" },
        { "specific.rule", "Specific pattern rules" },
        { "T0XlC.rule", "T0XlC comprehensive rule set" },
        { "T0XlCv2.rule", "T0XlC rule set version 2" },
        { "toggles1.rule", "Toggle case - 1 character" },
        { "toggles2.rule", "Toggle case - 2 characters" },
        { "toggles3.rule", "Toggle case - 3 characters" },
        { "toggles4.rule", "Toggle case - 4 characters" },
        { "toggles5.rule", "Toggle case - 5 characters" },
        { "unix-ninja-leetspeak.rule", "Unix-ninja leetspeak rules" }
    };
}

public class WordlistFile
{
    public string Name { get; set; } = string.Empty;
    public string FullPath { get; set; } = string.Empty;
    public long Size { get; set; }
    public long? LineCount { get; set; }

    public string SizeFormatted => FormatSize(Size);

    private static string FormatSize(long bytes)
    {
        if (bytes >= 1_073_741_824)
            return $"{bytes / 1_073_741_824.0:F2} GB";
        if (bytes >= 1_048_576)
            return $"{bytes / 1_048_576.0:F2} MB";
        if (bytes >= 1024)
            return $"{bytes / 1024.0:F2} KB";
        return $"{bytes} B";
    }
}

public class MaskFile
{
    public string Name { get; set; } = string.Empty;
    public string FullPath { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;

    public static Dictionary<string, string> KnownDescriptions => new()
    {
        { "hashcat-default.hcmask", "Default hashcat masks for common patterns" },
        { "8char-1l-1u-1d-1s-compliant.hcmask", "8 chars: 1 lower, 1 upper, 1 digit, 1 special (compliant)" },
        { "8char-1l-1u-1d-1s-noncompliant.hcmask", "8 chars: 1 lower, 1 upper, 1 digit, 1 special (non-compliant)" },
        { "rockyou-1-60.hcmask", "RockYou Markov masks (1-60 seconds)" },
        { "rockyou-2-1800.hcmask", "RockYou Markov masks (2-1800 seconds)" },
        { "rockyou-3-3600.hcmask", "RockYou Markov masks (3-3600 seconds)" },
        { "rockyou-4-43200.hcmask", "RockYou Markov masks (4-43200 seconds)" },
        { "rockyou-5-86400.hcmask", "RockYou Markov masks (5-86400 seconds)" },
        { "rockyou-6-864000.hcmask", "RockYou Markov masks (6-864000 seconds)" },
        { "rockyou-7-2592000.hcmask", "RockYou Markov masks (7-2592000 seconds)" }
    };
}

public class CharsetInfo
{
    public string Symbol { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Characters { get; set; } = string.Empty;
    public int Count { get; set; }

    public static List<CharsetInfo> GetBuiltIn() => new()
    {
        new() { Symbol = "?l", Name = "Lowercase", Characters = "abcdefghijklmnopqrstuvwxyz", Count = 26 },
        new() { Symbol = "?u", Name = "Uppercase", Characters = "ABCDEFGHIJKLMNOPQRSTUVWXYZ", Count = 26 },
        new() { Symbol = "?d", Name = "Digits", Characters = "0123456789", Count = 10 },
        new() { Symbol = "?h", Name = "Hex lowercase", Characters = "0123456789abcdef", Count = 16 },
        new() { Symbol = "?H", Name = "Hex uppercase", Characters = "0123456789ABCDEF", Count = 16 },
        new() { Symbol = "?s", Name = "Special", Characters = "!\"#$%&'()*+,-./:;<=>?@[\\]^_`{|}~", Count = 33 },
        new() { Symbol = "?a", Name = "All printable", Characters = "?l?u?d?s", Count = 95 },
        new() { Symbol = "?b", Name = "All bytes", Characters = "0x00-0xff", Count = 256 }
    };
}
