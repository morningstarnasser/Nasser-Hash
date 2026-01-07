using System;
using System.Collections.Generic;

namespace HashcatGUI.Models;

public class WalletAnalysis
{
    public string FilePath { get; set; } = string.Empty;
    public string FileName { get; set; } = string.Empty;
    public long FileSize { get; set; }
    public DateTime? FileCreated { get; set; }
    public DateTime? FileModified { get; set; }

    // Encryption Details
    public int IterationCount { get; set; }
    public string Salt { get; set; } = string.Empty;
    public int EncryptionMethod { get; set; }
    public string EncryptedMasterKey { get; set; } = string.Empty;

    // Wallet Contents
    public int KeyCount { get; set; }
    public int AddressCount { get; set; }
    public List<string> Addresses { get; set; } = new();
    public bool HasPublicKeys { get; set; }
    public bool IsEncrypted { get; set; }

    // Estimated Age & Era
    public WalletEra EstimatedEra { get; set; }
    public string EstimatedYearRange { get; set; } = string.Empty;

    // Password Pattern Recommendations
    public PasswordPatternRecommendation Recommendations { get; set; } = new();

    // Extracted Hash for Hashcat
    public string HashcatHash { get; set; } = string.Empty;

    // Analysis Status
    public bool IsValid { get; set; }
    public string ErrorMessage { get; set; } = string.Empty;
}

public enum WalletEra
{
    Unknown,
    VeryOld,      // 2009-2012: 19,000-25,000 iterations
    Old,          // 2012-2014: 25,000-35,000 iterations
    Middle,       // 2014-2017: 35,000-60,000 iterations
    Recent,       // 2017-2020: 60,000-100,000 iterations
    Modern,       // 2020-2023: 100,000-200,000 iterations
    Current       // 2023+: 200,000+ iterations
}

public class PasswordPatternRecommendation
{
    public string EraDescription { get; set; } = string.Empty;
    public string PasswordStyleDescription { get; set; } = string.Empty;
    public List<string> LikelyPatterns { get; set; } = new();
    public List<string> RecommendedWordlists { get; set; } = new();
    public List<string> RecommendedRules { get; set; } = new();
    public List<string> SuggestedMasks { get; set; } = new();
    public int EstimatedMinLength { get; set; }
    public int EstimatedMaxLength { get; set; }
    public double ComplexityScore { get; set; } // 1-10
    public string AttackStrategyRecommendation { get; set; } = string.Empty;
}

public class WalletKeyInfo
{
    public string PublicKey { get; set; } = string.Empty;
    public DateTime? CreatedAt { get; set; }
    public bool IsCompressed { get; set; }
}
