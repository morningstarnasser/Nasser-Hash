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

        return new SmartAttackProfile
        {
            Name = "Very Old Wallet Attack (2009-2012)",
            Description = "High success probability - early adopters used simple passwords",
            EstimatedDurationMinutes = 90,
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
                    Name = "Common Passwords",
                    Description = "Try most common passwords directly",
                    AttackMode = 0,
                    Wordlist = knockoutWordlist,
                    EstimatedDurationMinutes = 3
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
                    Name = "Simple Rules",
                    Description = "Apply basic transformations",
                    AttackMode = 0,
                    Wordlist = knockoutWordlist,
                    Rules = best64 != null ? new List<string> { best64 } : new(),
                    EstimatedDurationMinutes = 10
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
                    Name = "Bitcoin + Year Hybrid",
                    Description = "Bitcoin words + years 2009-2012",
                    AttackMode = 6,
                    Wordlist = bitcoinWordlist ?? knockoutWordlist,
                    Mask = "?d?d?d?d",
                    EstimatedDurationMinutes = 15
                },
                new()
                {
                    Order = 8,
                    Name = "Extended Rules",
                    Description = "Apply knockout rules",
                    AttackMode = 0,
                    Wordlist = knockoutWordlist,
                    Rules = knockoutRule != null ? new List<string> { knockoutRule } : new(),
                    EstimatedDurationMinutes = 25
                },
                new()
                {
                    Order = 9,
                    Name = "All Printable Short",
                    Description = "Try all 4-5 char combinations",
                    AttackMode = 3,
                    Mask = "?a?a?a?a?a",
                    IncrementMode = true,
                    IncrementMin = 4,
                    IncrementMax = 5,
                    EstimatedDurationMinutes = 10
                }
            }
        };
    }

    private static SmartAttackProfile GenerateOldProfile(string hashcatDir)
    {
        var knockoutWordlist = FindFile(hashcatDir, "knockout_hash.txt");
        var knockoutRule = FindFile(hashcatDir, "knockout_hash.rule");
        var oneRule = FindFile(hashcatDir, "rules", "OneRuleToRuleThemAll.rule") ??
                      FindFile(hashcatDir, "rules", "OneRule.rule");
        var bitcoinWordlist = GetBitcoinWordlistPath();
        var bitcoinRule = GetBitcoinRulePath();

        return new SmartAttackProfile
        {
            Name = "Old Wallet Attack (2012-2014)",
            Description = "Good success probability - slightly more complex passwords",
            EstimatedDurationMinutes = 150,
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
                    Name = "Bitcoin + Rules",
                    Description = "Bitcoin wordlist with crypto rules",
                    AttackMode = 0,
                    Wordlist = bitcoinWordlist ?? knockoutWordlist,
                    Rules = bitcoinRule != null ? new List<string> { bitcoinRule } : new(),
                    EstimatedDurationMinutes = 10
                },
                new()
                {
                    Order = 3,
                    Name = "Common + Year Patterns",
                    Description = "Bitcoin-related words with years",
                    AttackMode = 0,
                    Wordlist = knockoutWordlist,
                    EstimatedDurationMinutes = 5
                },
                new()
                {
                    Order = 4,
                    Name = "OneRule Attack",
                    Description = "Comprehensive rule-based mutations",
                    AttackMode = 0,
                    Wordlist = knockoutWordlist,
                    Rules = oneRule != null ? new List<string> { oneRule } : new(),
                    EstimatedDurationMinutes = 30
                },
                new()
                {
                    Order = 5,
                    Name = "Bitcoin + Digits",
                    Description = "Bitcoin words + 2-4 digit suffix",
                    AttackMode = 6,
                    Wordlist = bitcoinWordlist ?? knockoutWordlist,
                    Mask = "?d?d?d?d",
                    IncrementMode = true,
                    IncrementMin = 2,
                    IncrementMax = 4,
                    EstimatedDurationMinutes = 15
                },
                new()
                {
                    Order = 6,
                    Name = "Hybrid Word+Digits",
                    Description = "Wordlist + 2-4 digit suffix",
                    AttackMode = 6,
                    Wordlist = knockoutWordlist,
                    Mask = "?d?d?d?d",
                    IncrementMode = true,
                    IncrementMin = 2,
                    IncrementMax = 4,
                    EstimatedDurationMinutes = 20
                },
                new()
                {
                    Order = 7,
                    Name = "Capitalized + Year",
                    Description = "Try Cap word + year patterns",
                    AttackMode = 3,
                    Mask = "?u?l?l?l?l?l?l?d?d?d?d",
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
                    EstimatedDurationMinutes = 28
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
