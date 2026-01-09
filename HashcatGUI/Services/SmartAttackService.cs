using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using HashcatGUI.Models;

namespace HashcatGUI.Services;

/// <summary>
/// Generates optimized attack profiles based on wallet analysis.
/// </summary>
public static class SmartAttackService
{
    private static string? _bitcoinWordlistPath;
    private static string? _bitcoinRulePath;

    /// <summary>
    /// Initializes the Bitcoin-specific resources.
    /// </summary>
    public static void Initialize()
    {
        EnsureResourcesExtracted();
    }

    /// <summary>
    /// Ensures Bitcoin wordlist and rules are extracted to temp folder.
    /// </summary>
    private static void EnsureResourcesExtracted()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), "NasserHash");
        Directory.CreateDirectory(tempDir);

        _bitcoinWordlistPath = Path.Combine(tempDir, "bitcoin_wordlist.txt");
        _bitcoinRulePath = Path.Combine(tempDir, "bitcoin.rule");

        // Extract embedded resources if they don't exist or are outdated
        var assemblyDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) ?? ".";
        var sourceWordlist = Path.Combine(assemblyDir, "Resources", "bitcoin_wordlist.txt");
        var sourceRule = Path.Combine(assemblyDir, "Resources", "bitcoin.rule");

        if (File.Exists(sourceWordlist))
        {
            File.Copy(sourceWordlist, _bitcoinWordlistPath, true);
        }

        if (File.Exists(sourceRule))
        {
            File.Copy(sourceRule, _bitcoinRulePath, true);
        }
    }

    /// <summary>
    /// Gets the path to the Bitcoin-specific wordlist.
    /// </summary>
    public static string? GetBitcoinWordlistPath()
    {
        if (_bitcoinWordlistPath == null)
            EnsureResourcesExtracted();
        return File.Exists(_bitcoinWordlistPath) ? _bitcoinWordlistPath : null;
    }

    /// <summary>
    /// Gets the path to the Bitcoin-specific rules file.
    /// </summary>
    public static string? GetBitcoinRulePath()
    {
        if (_bitcoinRulePath == null)
            EnsureResourcesExtracted();
        return File.Exists(_bitcoinRulePath) ? _bitcoinRulePath : null;
    }

    /// <summary>
    /// Generates an optimized attack profile based on wallet analysis.
    /// </summary>
    public static SmartAttackProfile GenerateProfile(WalletAnalysis analysis, string hashcatPath)
    {
        EnsureResourcesExtracted();

        var profile = new SmartAttackProfile
        {
            Name = $"Smart Attack - {analysis.EstimatedEra}",
            Description = $"Optimized attack for wallets from {analysis.EstimatedYearRange}",
            TargetEra = analysis.EstimatedEra
        };

        var hashcatDir = Path.GetDirectoryName(hashcatPath) ?? ".";

        switch (analysis.EstimatedEra)
        {
            case WalletEra.VeryOld:
                profile = GenerateVeryOldProfile(hashcatDir);
                break;
            case WalletEra.Old:
                profile = GenerateOldProfile(hashcatDir);
                break;
            case WalletEra.Middle:
                profile = GenerateMiddleProfile(hashcatDir);
                break;
            case WalletEra.Recent:
                profile = GenerateRecentProfile(hashcatDir);
                break;
            case WalletEra.Modern:
            case WalletEra.Current:
            default:
                profile = GenerateModernProfile(hashcatDir);
                break;
        }

        profile.TargetEra = analysis.EstimatedEra;
        return profile;
    }

    private static SmartAttackProfile GenerateVeryOldProfile(string hashcatDir)
    {
        var knockoutWordlist = FindFile(hashcatDir, "knockout_hash.txt");
        var knockoutRule = FindFile(hashcatDir, "knockout_hash.rule");
        var best64 = FindFile(hashcatDir, "rules", "best64.rule");
        var bitcoinWordlist = GetBitcoinWordlistPath();
        var bitcoinRule = GetBitcoinRulePath();

        // Find large wordlists
        var rockyou = FindWordlist("rockyou.txt", hashcatDir);
        var weakpass = FindWordlist("weakpass_3a.txt", hashcatDir) ?? FindWordlist("weakpass_3.txt", hashcatDir) ?? FindWordlist("weakpass_2a.txt", hashcatDir);

        return new SmartAttackProfile
        {
            Name = "Very Old Wallet Attack (2009-2012)",
            Description = "High success probability - early adopters used simple passwords",
            EstimatedDurationMinutes = 120,
            SuccessProbability = 0.70,
            Phases = new List<AttackPhase>
            {
                new()
                {
                    Order = 1,
                    Name = "Bitcoin Wordlist Direct",
                    Description = "Try Bitcoin-specific passwords directly",
                    AttackMode = 0,
                    Wordlist = bitcoinWordlist ?? knockoutWordlist,
                    EstimatedDurationMinutes = 2
                },
                new()
                {
                    Order = 2,
                    Name = "Rockyou Direct",
                    Description = "Try rockyou.txt directly (14M passwords)",
                    AttackMode = 0,
                    Wordlist = rockyou ?? knockoutWordlist,
                    EstimatedDurationMinutes = 5
                },
                new()
                {
                    Order = 3,
                    Name = "Bitcoin Rules Attack",
                    Description = "Bitcoin wordlist with crypto-specific rules",
                    AttackMode = 0,
                    Wordlist = bitcoinWordlist ?? knockoutWordlist,
                    Rules = bitcoinRule != null ? new List<string> { bitcoinRule } : new(),
                    EstimatedDurationMinutes = 10
                },
                new()
                {
                    Order = 4,
                    Name = "Rockyou + Best64",
                    Description = "Rockyou with best64 rules",
                    AttackMode = 0,
                    Wordlist = rockyou ?? knockoutWordlist,
                    Rules = best64 != null ? new List<string> { best64 } : new(),
                    EstimatedDurationMinutes = 15
                },
                new()
                {
                    Order = 5,
                    Name = "Numeric Patterns",
                    Description = "Try numeric passwords 4-8 digits",
                    AttackMode = 3,
                    Mask = "?d?d?d?d?d?d?d?d",
                    IncrementMode = true,
                    IncrementMin = 4,
                    IncrementMax = 8,
                    EstimatedDurationMinutes = 5
                },
                new()
                {
                    Order = 6,
                    Name = "Short Lowercase",
                    Description = "Try all lowercase 4-6 characters",
                    AttackMode = 3,
                    Mask = "?l?l?l?l?l?l",
                    IncrementMode = true,
                    IncrementMin = 4,
                    IncrementMax = 6,
                    EstimatedDurationMinutes = 10
                },
                new()
                {
                    Order = 7,
                    Name = "Rockyou + Year Hybrid",
                    Description = "Rockyou words + years",
                    AttackMode = 6,
                    Wordlist = rockyou ?? knockoutWordlist,
                    Mask = "?d?d?d?d",
                    EstimatedDurationMinutes = 20
                },
                new()
                {
                    Order = 8,
                    Name = "Weakpass Direct",
                    Description = "Try large weakpass wordlist",
                    AttackMode = 0,
                    Wordlist = weakpass ?? rockyou ?? knockoutWordlist,
                    EstimatedDurationMinutes = 30
                },
                new()
                {
                    Order = 9,
                    Name = "Extended Rules",
                    Description = "Apply knockout rules",
                    AttackMode = 0,
                    Wordlist = knockoutWordlist,
                    Rules = knockoutRule != null ? new List<string> { knockoutRule } : new(),
                    EstimatedDurationMinutes = 25
                }
            }
        };
    }

    /// <summary>
    /// Finds a wordlist in common locations.
    /// </summary>
    private static string? FindWordlist(string filename, string? hashcatDir = null)
    {
        // First check hashcat directory (most likely location)
        if (!string.IsNullOrEmpty(hashcatDir))
        {
            var inHashcat = Path.Combine(hashcatDir, filename);
            if (File.Exists(inHashcat))
                return inHashcat;

            var inWordlists = Path.Combine(hashcatDir, "wordlists", filename);
            if (File.Exists(inWordlists))
                return inWordlists;
        }

        // Also check from App.Settings
        var hashcatPath = App.Settings?.Settings?.HashcatPath;
        if (!string.IsNullOrEmpty(hashcatPath))
        {
            var settingsHashcatDir = Path.GetDirectoryName(hashcatPath);
            if (settingsHashcatDir != null && settingsHashcatDir != hashcatDir)
            {
                var inHashcat = Path.Combine(settingsHashcatDir, filename);
                if (File.Exists(inHashcat))
                    return inHashcat;

                var inWordlists = Path.Combine(settingsHashcatDir, "wordlists", filename);
                if (File.Exists(inWordlists))
                    return inWordlists;
            }
        }

        var searchPaths = new[]
        {
            // Common wordlist locations
            @"C:\wordlists",
            @"C:\Users\alina\Downloads\btc_test\hashcat-6.2.6",
            @"C:\Users\alina\Downloads\wordlists",
            @"C:\Users\alina\Downloads",
            @"D:\wordlists",
            @"C:\hashcat\wordlists",
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "wordlists"),
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Downloads"),
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Downloads", "wordlists"),
        };

        foreach (var basePath in searchPaths)
        {
            var fullPath = Path.Combine(basePath, filename);
            if (File.Exists(fullPath))
                return fullPath;
        }

        return null;
    }

    private static SmartAttackProfile GenerateOldProfile(string hashcatDir)
    {
        var knockoutWordlist = FindFile(hashcatDir, "knockout_hash.txt");
        var knockoutRule = FindFile(hashcatDir, "knockout_hash.rule");
        var oneRule = FindFile(hashcatDir, "rules", "OneRuleToRuleThemAll.rule") ??
                      FindFile(hashcatDir, "rules", "OneRule.rule");
        var best64 = FindFile(hashcatDir, "rules", "best64.rule");
        var bitcoinWordlist = GetBitcoinWordlistPath();
        var bitcoinRule = GetBitcoinRulePath();

        // Find large wordlists
        var rockyou = FindWordlist("rockyou.txt", hashcatDir);
        var weakpass = FindWordlist("weakpass_3a.txt", hashcatDir) ?? FindWordlist("weakpass_3.txt", hashcatDir);

        return new SmartAttackProfile
        {
            Name = "Old Wallet Attack (2012-2014)",
            Description = "Good success probability - slightly more complex passwords",
            EstimatedDurationMinutes = 180,
            SuccessProbability = 0.55,
            Phases = new List<AttackPhase>
            {
                new()
                {
                    Order = 1,
                    Name = "Bitcoin Wordlist",
                    Description = "Try Bitcoin-specific passwords",
                    AttackMode = 0,
                    Wordlist = bitcoinWordlist ?? knockoutWordlist,
                    EstimatedDurationMinutes = 2
                },
                new()
                {
                    Order = 2,
                    Name = "Rockyou Direct",
                    Description = "Try rockyou.txt (14M passwords)",
                    AttackMode = 0,
                    Wordlist = rockyou ?? knockoutWordlist,
                    EstimatedDurationMinutes = 5
                },
                new()
                {
                    Order = 3,
                    Name = "Rockyou + Best64",
                    Description = "Rockyou with best64 rules",
                    AttackMode = 0,
                    Wordlist = rockyou ?? knockoutWordlist,
                    Rules = best64 != null ? new List<string> { best64 } : new(),
                    EstimatedDurationMinutes = 15
                },
                new()
                {
                    Order = 4,
                    Name = "Bitcoin + Rules",
                    Description = "Bitcoin wordlist with crypto rules",
                    AttackMode = 0,
                    Wordlist = bitcoinWordlist ?? knockoutWordlist,
                    Rules = bitcoinRule != null ? new List<string> { bitcoinRule } : new(),
                    EstimatedDurationMinutes = 10
                },
                new()
                {
                    Order = 5,
                    Name = "OneRule Attack",
                    Description = "Comprehensive rule-based mutations",
                    AttackMode = 0,
                    Wordlist = rockyou ?? knockoutWordlist,
                    Rules = oneRule != null ? new List<string> { oneRule } : new(),
                    EstimatedDurationMinutes = 45
                },
                new()
                {
                    Order = 6,
                    Name = "Rockyou + Digits",
                    Description = "Rockyou words + 2-4 digit suffix",
                    AttackMode = 6,
                    Wordlist = rockyou ?? knockoutWordlist,
                    Mask = "?d?d?d?d",
                    IncrementMode = true,
                    IncrementMin = 2,
                    IncrementMax = 4,
                    EstimatedDurationMinutes = 25
                },
                new()
                {
                    Order = 7,
                    Name = "Weakpass Direct",
                    Description = "Try large weakpass wordlist",
                    AttackMode = 0,
                    Wordlist = weakpass ?? rockyou ?? knockoutWordlist,
                    EstimatedDurationMinutes = 40
                },
                new()
                {
                    Order = 8,
                    Name = "Full Knockout",
                    Description = "Full knockout rule attack",
                    AttackMode = 0,
                    Wordlist = knockoutWordlist,
                    Rules = knockoutRule != null ? new List<string> { knockoutRule } : new(),
                    EstimatedDurationMinutes = 38
                }
            }
        };
    }

    private static SmartAttackProfile GenerateMiddleProfile(string hashcatDir)
    {
        var knockoutWordlist = FindFile(hashcatDir, "knockout_hash.txt");
        var knockoutRule = FindFile(hashcatDir, "knockout_hash.rule");
        var dive = FindFile(hashcatDir, "rules", "dive.rule");

        return new SmartAttackProfile
        {
            Name = "Middle-Age Wallet Attack (2014-2017)",
            Description = "Moderate success - passwords getting more complex",
            EstimatedDurationMinutes = 240,
            SuccessProbability = 0.35,
            Phases = new List<AttackPhase>
            {
                new()
                {
                    Order = 1,
                    Name = "Direct Wordlist",
                    Description = "Try knockout wordlist directly",
                    AttackMode = 0,
                    Wordlist = knockoutWordlist,
                    EstimatedDurationMinutes = 5
                },
                new()
                {
                    Order = 2,
                    Name = "Dive Rules",
                    Description = "Comprehensive dive rule attack",
                    AttackMode = 0,
                    Wordlist = knockoutWordlist,
                    Rules = dive != null ? new List<string> { dive } : new(),
                    EstimatedDurationMinutes = 60
                },
                new()
                {
                    Order = 3,
                    Name = "Word + Symbol + Year",
                    Description = "Hybrid with special chars and years",
                    AttackMode = 6,
                    Wordlist = knockoutWordlist,
                    Mask = "?s?d?d?d?d",
                    EstimatedDurationMinutes = 45
                },
                new()
                {
                    Order = 4,
                    Name = "Knockout Rules",
                    Description = "Full knockout rule set",
                    AttackMode = 0,
                    Wordlist = knockoutWordlist,
                    Rules = knockoutRule != null ? new List<string> { knockoutRule } : new(),
                    EstimatedDurationMinutes = 90
                },
                new()
                {
                    Order = 5,
                    Name = "Extended Brute Force",
                    Description = "Lowercase 6-8 chars",
                    AttackMode = 3,
                    Mask = "?l?l?l?l?l?l?l?l",
                    IncrementMode = true,
                    IncrementMin = 6,
                    IncrementMax = 8,
                    EstimatedDurationMinutes = 40
                }
            }
        };
    }

    private static SmartAttackProfile GenerateRecentProfile(string hashcatDir)
    {
        var knockoutWordlist = FindFile(hashcatDir, "knockout_hash.txt");
        var knockoutRule = FindFile(hashcatDir, "knockout_hash.rule");

        return new SmartAttackProfile
        {
            Name = "Recent Wallet Attack (2017-2020)",
            Description = "Lower success - crypto boom era passwords",
            EstimatedDurationMinutes = 480,
            SuccessProbability = 0.20,
            Phases = new List<AttackPhase>
            {
                new()
                {
                    Order = 1,
                    Name = "Crypto Slang",
                    Description = "HODL, moon, lambo patterns",
                    AttackMode = 0,
                    Wordlist = knockoutWordlist,
                    EstimatedDurationMinutes = 5
                },
                new()
                {
                    Order = 2,
                    Name = "Knockout Attack",
                    Description = "Full knockout rules",
                    AttackMode = 0,
                    Wordlist = knockoutWordlist,
                    Rules = knockoutRule != null ? new List<string> { knockoutRule } : new(),
                    EstimatedDurationMinutes = 120
                },
                new()
                {
                    Order = 3,
                    Name = "Complex Hybrid",
                    Description = "Word + digits + special",
                    AttackMode = 6,
                    Wordlist = knockoutWordlist,
                    Mask = "?d?d?d?d?s",
                    EstimatedDurationMinutes = 90
                },
                new()
                {
                    Order = 4,
                    Name = "Year Patterns",
                    Description = "2017-2020 year combinations",
                    AttackMode = 6,
                    Wordlist = knockoutWordlist,
                    Mask = "201?",
                    EstimatedDurationMinutes = 30
                },
                new()
                {
                    Order = 5,
                    Name = "Extended Search",
                    Description = "Longer combinations",
                    AttackMode = 3,
                    Mask = "?l?l?l?l?l?l?l?l?d?d",
                    IncrementMode = true,
                    IncrementMin = 8,
                    IncrementMax = 10,
                    EstimatedDurationMinutes = 235
                }
            }
        };
    }

    private static SmartAttackProfile GenerateModernProfile(string hashcatDir)
    {
        var knockoutWordlist = FindFile(hashcatDir, "knockout_hash.txt");
        var knockoutRule = FindFile(hashcatDir, "knockout_hash.rule");

        return new SmartAttackProfile
        {
            Name = "Modern Wallet Attack (2020+)",
            Description = "Difficult target - expect long runtime",
            EstimatedDurationMinutes = 1440,
            SuccessProbability = 0.10,
            Phases = new List<AttackPhase>
            {
                new()
                {
                    Order = 1,
                    Name = "Direct Try",
                    Description = "Try wordlist directly first",
                    AttackMode = 0,
                    Wordlist = knockoutWordlist,
                    EstimatedDurationMinutes = 5
                },
                new()
                {
                    Order = 2,
                    Name = "Full Knockout",
                    Description = "Comprehensive knockout attack",
                    AttackMode = 0,
                    Wordlist = knockoutWordlist,
                    Rules = knockoutRule != null ? new List<string> { knockoutRule } : new(),
                    EstimatedDurationMinutes = 180
                },
                new()
                {
                    Order = 3,
                    Name = "Modern Patterns",
                    Description = "DeFi/NFT terminology + numbers",
                    AttackMode = 6,
                    Wordlist = knockoutWordlist,
                    Mask = "?d?d?d?d?s?s",
                    EstimatedDurationMinutes = 240
                },
                new()
                {
                    Order = 4,
                    Name = "Extended Brute",
                    Description = "Long lowercase + digits",
                    AttackMode = 3,
                    Mask = "?l?l?l?l?l?l?l?l?l?l?d?d",
                    IncrementMode = true,
                    IncrementMin = 8,
                    IncrementMax = 12,
                    EstimatedDurationMinutes = 1015
                }
            }
        };
    }

    private static string? FindFile(string baseDir, params string[] pathParts)
    {
        var path = Path.Combine(new[] { baseDir }.Concat(pathParts).ToArray());
        return File.Exists(path) ? path : null;
    }

    /// <summary>
    /// Gets a list of predefined attack profiles.
    /// </summary>
    public static List<SmartAttackProfile> GetPresetProfiles(string hashcatDir)
    {
        return new List<SmartAttackProfile>
        {
            GenerateVeryOldProfile(hashcatDir),
            GenerateOldProfile(hashcatDir),
            GenerateMiddleProfile(hashcatDir),
            GenerateRecentProfile(hashcatDir),
            GenerateModernProfile(hashcatDir)
        };
    }
}
