using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HashcatGUI.Models;

namespace HashcatGUI.Services;

/// <summary>
/// Analyzes Bitcoin wallet.dat files to extract encryption metadata and provide
/// password pattern recommendations based on wallet age and encryption parameters.
/// </summary>
public static class WalletAnalyzerService
{
    /// <summary>
    /// Performs a complete analysis of a wallet.dat file.
    /// </summary>
    public static async Task<WalletAnalysis> AnalyzeWalletAsync(string walletPath)
    {
        var analysis = new WalletAnalysis
        {
            FilePath = walletPath,
            FileName = Path.GetFileName(walletPath)
        };

        if (!File.Exists(walletPath))
        {
            analysis.IsValid = false;
            analysis.ErrorMessage = "File not found";
            return analysis;
        }

        try
        {
            // Get file info
            var fileInfo = new FileInfo(walletPath);
            analysis.FileSize = fileInfo.Length;
            analysis.FileCreated = fileInfo.CreationTime;
            analysis.FileModified = fileInfo.LastWriteTime;

            // Read wallet data
            var walletData = await File.ReadAllBytesAsync(walletPath);

            // Extract mkey data
            var mkeyData = ExtractMKeyData(walletData);
            if (mkeyData == null)
            {
                analysis.IsValid = false;
                analysis.ErrorMessage = "No encrypted master key found - wallet may not be encrypted or is corrupted";
                return analysis;
            }

            // Populate encryption details
            analysis.IsEncrypted = true;
            analysis.IterationCount = mkeyData.Value.Iterations;
            analysis.Salt = ToHex(mkeyData.Value.Salt);
            analysis.EncryptionMethod = mkeyData.Value.Method;
            analysis.EncryptedMasterKey = ToHex(mkeyData.Value.EncryptedKey);

            // Count keys and addresses
            analysis.KeyCount = CountPattern(walletData, new byte[] { 0x04, 0x63, 0x6b, 0x65, 0x79 }); // \x04ckey
            analysis.Addresses = ExtractBitcoinAddresses(walletData);
            analysis.AddressCount = analysis.Addresses.Count;
            analysis.HasPublicKeys = FindBytes(walletData, new byte[] { 0x02 }) != -1 ||
                                     FindBytes(walletData, new byte[] { 0x03 }) != -1;

            // Determine wallet era based on iteration count
            analysis.EstimatedEra = DetermineWalletEra(mkeyData.Value.Iterations);
            analysis.EstimatedYearRange = GetYearRangeForEra(analysis.EstimatedEra);

            // Generate password pattern recommendations
            analysis.Recommendations = GenerateRecommendations(analysis);

            // Extract hashcat-compatible hash
            var ckeyData = ExtractCKeyData(walletData);
            analysis.HashcatHash = BuildHashcatHash(mkeyData.Value, ckeyData);

            analysis.IsValid = true;
        }
        catch (Exception ex)
        {
            analysis.IsValid = false;
            analysis.ErrorMessage = $"Analysis failed: {ex.Message}";
        }

        return analysis;
    }

    /// <summary>
    /// Extracts master key data from wallet bytes.
    /// </summary>
    private static MKeyData? ExtractMKeyData(byte[] data)
    {
        // Search for CMasterKey pattern:
        // 0x30 (48 bytes encrypted key length) + <48 bytes> + 0x08 (8 bytes salt) + <8 bytes> + method(4) + iterations(4)
        for (int i = 0; i < data.Length - 70; i++)
        {
            if (data[i] == 0x30) // 48-byte encrypted key length prefix
            {
                int saltLenPos = i + 49;
                if (saltLenPos >= data.Length)
                    continue;

                if (data[saltLenPos] == 0x08) // Salt length marker (8 bytes)
                {
                    int iterationsPos = saltLenPos + 9 + 4; // salt_len(1) + salt(8) + method(4)
                    if (iterationsPos + 4 > data.Length)
                        continue;

                    int iterations = BitConverter.ToInt32(data, iterationsPos);

                    // Validate iterations - should be between 1000 and 1000000 for most wallets
                    if (iterations >= 1000 && iterations <= 1000000)
                    {
                        byte[] encryptedKey = new byte[48];
                        Array.Copy(data, i + 1, encryptedKey, 0, 48);

                        byte[] salt = new byte[8];
                        Array.Copy(data, saltLenPos + 1, salt, 0, 8);

                        int method = BitConverter.ToInt32(data, saltLenPos + 9);

                        return new MKeyData
                        {
                            EncryptedKey = encryptedKey,
                            Salt = salt,
                            Method = method,
                            Iterations = iterations
                        };
                    }
                }
            }
        }

        return null;
    }

    /// <summary>
    /// Extracts encrypted private key data.
    /// </summary>
    private static CKeyData? ExtractCKeyData(byte[] data)
    {
        // Search for ckey marker
        byte[] marker = { 0x04, 0x63, 0x6b, 0x65, 0x79 }; // \x04ckey
        int pos = FindBytes(data, marker);

        if (pos == -1)
        {
            marker = new byte[] { 0x63, 0x6b, 0x65, 0x79 }; // ckey
            pos = FindBytes(data, marker);
            if (pos == -1) return null;
            pos += 4;
        }
        else
        {
            pos += 5;
        }

        byte[]? publicKey = null;
        byte[]? encryptedPrivateKey = null;

        for (int offset = 0; offset < 300 && pos + offset + 100 < data.Length; offset++)
        {
            int checkPos = pos + offset;

            // Look for compressed public key (33 bytes starting with 02 or 03)
            if (publicKey == null)
            {
                if (data[checkPos] == 0x21 && checkPos + 34 < data.Length)
                {
                    byte nextByte = data[checkPos + 1];
                    if (nextByte == 0x02 || nextByte == 0x03)
                    {
                        publicKey = new byte[33];
                        Array.Copy(data, checkPos + 1, publicKey, 0, 33);
                    }
                }
                else if ((data[checkPos] == 0x02 || data[checkPos] == 0x03) && checkPos + 33 < data.Length)
                {
                    publicKey = new byte[33];
                    Array.Copy(data, checkPos, publicKey, 0, 33);
                }
            }

            // Look for encrypted private key
            if (encryptedPrivateKey == null && data[checkPos] == 0x30 && checkPos + 49 < data.Length)
            {
                encryptedPrivateKey = new byte[48];
                Array.Copy(data, checkPos + 1, encryptedPrivateKey, 0, 48);
            }

            if (publicKey != null && encryptedPrivateKey != null)
                break;
        }

        if (publicKey != null || encryptedPrivateKey != null)
        {
            return new CKeyData
            {
                PublicKey = publicKey,
                EncryptedPrivateKey = encryptedPrivateKey
            };
        }

        return null;
    }

    /// <summary>
    /// Determines the wallet era based on iteration count.
    /// </summary>
    private static WalletEra DetermineWalletEra(int iterations)
    {
        return iterations switch
        {
            < 25000 => WalletEra.VeryOld,      // 2009-2012
            < 35000 => WalletEra.Old,          // 2012-2014
            < 60000 => WalletEra.Middle,       // 2014-2017
            < 100000 => WalletEra.Recent,      // 2017-2020
            < 200000 => WalletEra.Modern,      // 2020-2023
            _ => WalletEra.Current             // 2023+
        };
    }

    /// <summary>
    /// Gets the estimated year range for a wallet era.
    /// </summary>
    private static string GetYearRangeForEra(WalletEra era)
    {
        return era switch
        {
            WalletEra.VeryOld => "2009-2012 (Bitcoin Early Adopter Era)",
            WalletEra.Old => "2012-2014 (Bitcoin Growth Era)",
            WalletEra.Middle => "2014-2017 (Bitcoin Mainstream Era)",
            WalletEra.Recent => "2017-2020 (Bitcoin Boom Era)",
            WalletEra.Modern => "2020-2023 (Bitcoin Institutional Era)",
            WalletEra.Current => "2023+ (Current Era)",
            _ => "Unknown"
        };
    }

    /// <summary>
    /// Generates password pattern recommendations based on wallet analysis.
    /// </summary>
    private static PasswordPatternRecommendation GenerateRecommendations(WalletAnalysis analysis)
    {
        var rec = new PasswordPatternRecommendation();

        switch (analysis.EstimatedEra)
        {
            case WalletEra.VeryOld:
                rec.EraDescription = "VERY OLD WALLET (2009-2012)";
                rec.PasswordStyleDescription = "Early adopters typically used simple passwords. Security awareness was low.";
                rec.EstimatedMinLength = 4;
                rec.EstimatedMaxLength = 12;
                rec.ComplexityScore = 2.5;
                rec.LikelyPatterns = new List<string>
                {
                    "Simple dictionary words (password, bitcoin, satoshi)",
                    "Short numeric passwords (123456, 1234)",
                    "Name + numbers (john123, mike1)",
                    "Simple keyboard patterns (qwerty, asdf)",
                    "All lowercase, rarely mixed case",
                    "Rarely special characters"
                };
                rec.SuggestedMasks = new List<string>
                {
                    "?l?l?l?l?l?l (6 lowercase)",
                    "?l?l?l?l?l?l?l?l (8 lowercase)",
                    "?l?l?l?l?d?d?d (word + 3 digits)",
                    "?d?d?d?d?d?d (6 digits)"
                };
                rec.AttackStrategyRecommendation = "Start with small wordlists + basic rules. Try common passwords first. High success rate expected.";
                break;

            case WalletEra.Old:
                rec.EraDescription = "OLD WALLET (2012-2014)";
                rec.PasswordStyleDescription = "Growing Bitcoin awareness. Slightly better passwords but still mostly simple.";
                rec.EstimatedMinLength = 6;
                rec.EstimatedMaxLength = 14;
                rec.ComplexityScore = 4.0;
                rec.LikelyPatterns = new List<string>
                {
                    "Word + year (bitcoin2013, wallet2014)",
                    "Capitalized first letter (Bitcoin, Password)",
                    "Simple leet speak (b1tc0in, p4ssw0rd)",
                    "Name combinations (firstname + birthday)",
                    "Tech/crypto terms (blockchain, mining, satoshi)",
                    "Occasional ! or 1 at end"
                };
                rec.SuggestedMasks = new List<string>
                {
                    "?u?l?l?l?l?l?l?d?d (Cap word + 2 digits)",
                    "?l?l?l?l?l?l?l?l?d?d?d?d (word + year)",
                    "?u?l?l?l?l?l?l?d?d?d?d (Cap + year)"
                };
                rec.AttackStrategyRecommendation = "Use medium wordlists with Best64/OneRule. Focus on crypto-related terms. Include years 2012-2014.";
                break;

            case WalletEra.Middle:
                rec.EraDescription = "MIDDLE-AGE WALLET (2014-2017)";
                rec.PasswordStyleDescription = "Increased security awareness. More complex passwords common.";
                rec.EstimatedMinLength = 8;
                rec.EstimatedMaxLength = 16;
                rec.ComplexityScore = 5.5;
                rec.LikelyPatterns = new List<string>
                {
                    "Word + Symbol + Year (Bitcoin!2016)",
                    "Multiple words (MyBitcoinWallet)",
                    "CamelCase patterns (BitCoin, PassWord)",
                    "More leet speak variations (B1tC01n, P@ssw0rd)",
                    "Special chars at end (!@#$)",
                    "German words for German users (Passwort, Geheim)"
                };
                rec.SuggestedMasks = new List<string>
                {
                    "?u?l?l?l?l?l?l?s?d?d?d?d (Cap + symbol + year)",
                    "?u?l?l?l?l?l?l?l?d?d?d?d?s (word + year + symbol)"
                };
                rec.AttackStrategyRecommendation = "Use comprehensive wordlists with d3ad0ne/dive rules. Include years 2014-2017. Try symbol variations.";
                break;

            case WalletEra.Recent:
                rec.EraDescription = "RECENT WALLET (2017-2020)";
                rec.PasswordStyleDescription = "Bitcoin boom era. Mixed security levels - some very secure, some still simple.";
                rec.EstimatedMinLength = 8;
                rec.EstimatedMaxLength = 20;
                rec.ComplexityScore = 6.5;
                rec.LikelyPatterns = new List<string>
                {
                    "Crypto slang (hodl, tothemoon, lambo)",
                    "Complex patterns (MyBitcoin2019!)",
                    "Passphrase style (multiple words)",
                    "Exchange-influenced (binance, coinbase prefixes)",
                    "FOMO/hype words (moon, rocket, diamond)",
                    "Memorable sentences shortened"
                };
                rec.SuggestedMasks = new List<string>
                {
                    "?u?l?l?l?l?l?l?l?d?d?d?d?s?s (complex)",
                    "?u?l?l?l?d?d?d?d?s (short complex)"
                };
                rec.AttackStrategyRecommendation = "Use knockout_hash wordlist with knockout_hash.rule. Include crypto slang and years 2017-2020.";
                break;

            case WalletEra.Modern:
                rec.EraDescription = "MODERN WALLET (2020-2023)";
                rec.PasswordStyleDescription = "Generally better security. Often password managers or complex passphrases.";
                rec.EstimatedMinLength = 10;
                rec.EstimatedMaxLength = 25;
                rec.ComplexityScore = 7.5;
                rec.LikelyPatterns = new List<string>
                {
                    "Long passphrases (4+ words)",
                    "Password manager generated",
                    "Very complex mixed character sets",
                    "Pandemic-era words (covid, lockdown, remote)",
                    "DeFi terms (yield, farming, liquidity)",
                    "NFT-related (nft, opensea, mint)"
                };
                rec.SuggestedMasks = new List<string>
                {
                    "Long masks with all character types",
                    "Hybrid attacks recommended"
                };
                rec.AttackStrategyRecommendation = "Difficult target. Use hybrid attacks. Focus on personal info if known. Consider DeFi/NFT terminology.";
                break;

            case WalletEra.Current:
                rec.EraDescription = "CURRENT WALLET (2023+)";
                rec.PasswordStyleDescription = "Modern security practices. Often very strong passwords.";
                rec.EstimatedMinLength = 12;
                rec.EstimatedMaxLength = 30;
                rec.ComplexityScore = 8.5;
                rec.LikelyPatterns = new List<string>
                {
                    "Very long passphrases",
                    "Password manager generated (random)",
                    "Hardware wallet backup phrases",
                    "AI-related terms (gpt, ai, chatbot)",
                    "Maximum complexity expected"
                };
                rec.SuggestedMasks = new List<string>
                {
                    "Hybrid dictionary + mask attacks",
                    "Consider very long runtimes"
                };
                rec.AttackStrategyRecommendation = "Very difficult target. Success unlikely without partial password knowledge. Focus on targeted attacks.";
                break;

            default:
                rec.EraDescription = "UNKNOWN ERA";
                rec.PasswordStyleDescription = "Could not determine wallet age.";
                rec.ComplexityScore = 5.0;
                rec.AttackStrategyRecommendation = "Try general attack strategies.";
                break;
        }

        // Add recommended wordlists based on era
        rec.RecommendedWordlists = analysis.EstimatedEra switch
        {
            WalletEra.VeryOld or WalletEra.Old => new List<string>
            {
                "rockyou.txt",
                "knockout_hash.txt",
                "common_passwords.txt"
            },
            WalletEra.Middle or WalletEra.Recent => new List<string>
            {
                "knockout_hash.txt",
                "rockyou.txt",
                "bitcoin_related.txt",
                "crypto_terms.txt"
            },
            _ => new List<string>
            {
                "knockout_hash.txt",
                "Large combined wordlists"
            }
        };

        // Add recommended rules
        rec.RecommendedRules = analysis.EstimatedEra switch
        {
            WalletEra.VeryOld => new List<string> { "best64.rule" },
            WalletEra.Old => new List<string> { "best64.rule", "OneRule.rule" },
            WalletEra.Middle => new List<string> { "OneRule.rule", "d3ad0ne.rule" },
            WalletEra.Recent => new List<string> { "knockout_hash.rule", "dive.rule" },
            _ => new List<string> { "knockout_hash.rule" }
        };

        return rec;
    }

    /// <summary>
    /// Builds the hashcat-compatible hash string.
    /// </summary>
    private static string BuildHashcatHash(MKeyData mkey, CKeyData? ckey)
    {
        var sb = new StringBuilder();
        sb.Append("$bitcoin$");
        sb.Append(mkey.EncryptedKey.Length * 2);
        sb.Append('$');
        sb.Append(ToHex(mkey.EncryptedKey));
        sb.Append('$');
        sb.Append(mkey.Salt.Length * 2);
        sb.Append('$');
        sb.Append(ToHex(mkey.Salt));
        sb.Append('$');
        sb.Append(mkey.Iterations);

        if (ckey.HasValue && ckey.Value.EncryptedPrivateKey != null)
        {
            sb.Append('$');
            sb.Append(ckey.Value.EncryptedPrivateKey.Length * 2);
            sb.Append('$');
            sb.Append(ToHex(ckey.Value.EncryptedPrivateKey));
        }

        if (ckey.HasValue && ckey.Value.PublicKey != null)
        {
            sb.Append('$');
            sb.Append(ckey.Value.PublicKey.Length * 2);
            sb.Append('$');
            sb.Append(ToHex(ckey.Value.PublicKey));
        }

        return sb.ToString();
    }

    private static int FindBytes(byte[] data, byte[] pattern)
    {
        for (int i = 0; i <= data.Length - pattern.Length; i++)
        {
            bool match = true;
            for (int j = 0; j < pattern.Length; j++)
            {
                if (data[i + j] != pattern[j])
                {
                    match = false;
                    break;
                }
            }
            if (match) return i;
        }
        return -1;
    }

    private static int CountPattern(byte[] data, byte[] pattern)
    {
        int count = 0;
        int pos = 0;
        while ((pos = FindBytesFrom(data, pattern, pos)) != -1)
        {
            count++;
            pos++;
        }
        return count;
    }

    /// <summary>
    /// Extracts Bitcoin addresses from wallet data.
    /// </summary>
    private static List<string> ExtractBitcoinAddresses(byte[] data)
    {
        var addresses = new HashSet<string>();
        var dataString = Encoding.ASCII.GetString(data);

        // Search for Legacy addresses (1...)
        for (int i = 0; i < dataString.Length - 26; i++)
        {
            if (dataString[i] == '1')
            {
                var potential = ExtractAddressAt(dataString, i, 26, 34);
                if (IsValidBitcoinAddress(potential))
                    addresses.Add(potential);
            }
        }

        // Search for SegWit addresses (3...)
        for (int i = 0; i < dataString.Length - 26; i++)
        {
            if (dataString[i] == '3')
            {
                var potential = ExtractAddressAt(dataString, i, 26, 34);
                if (IsValidBitcoinAddress(potential))
                    addresses.Add(potential);
            }
        }

        // Search for Native SegWit addresses (bc1...)
        for (int i = 0; i < dataString.Length - 42; i++)
        {
            if (i + 3 < dataString.Length && dataString.Substring(i, 3) == "bc1")
            {
                var potential = ExtractAddressAt(dataString, i, 42, 62);
                if (IsValidBech32Address(potential))
                    addresses.Add(potential);
            }
        }

        return addresses.OrderBy(a => a).ToList();
    }

    private static string ExtractAddressAt(string data, int start, int minLen, int maxLen)
    {
        var sb = new StringBuilder();
        for (int i = start; i < data.Length && sb.Length < maxLen; i++)
        {
            char c = data[i];
            if (IsBase58Char(c) || (sb.Length > 0 && sb[0] == 'b' && IsBech32Char(c)))
            {
                sb.Append(c);
            }
            else
            {
                break;
            }
        }
        return sb.Length >= minLen ? sb.ToString() : string.Empty;
    }

    private static bool IsBase58Char(char c)
    {
        return (c >= '1' && c <= '9') ||
               (c >= 'A' && c <= 'H') ||
               (c >= 'J' && c <= 'N') ||
               (c >= 'P' && c <= 'Z') ||
               (c >= 'a' && c <= 'k') ||
               (c >= 'm' && c <= 'z');
    }

    private static bool IsBech32Char(char c)
    {
        const string bech32Chars = "qpzry9x8gf2tvdw0s3jn54khce6mua7l";
        return bech32Chars.Contains(char.ToLower(c));
    }

    private static bool IsValidBitcoinAddress(string address)
    {
        if (string.IsNullOrEmpty(address))
            return false;

        // Legacy address (1...)
        if (address.StartsWith("1") && address.Length >= 26 && address.Length <= 34)
            return address.All(IsBase58Char);

        // SegWit address (3...)
        if (address.StartsWith("3") && address.Length >= 26 && address.Length <= 34)
            return address.All(IsBase58Char);

        return false;
    }

    private static bool IsValidBech32Address(string address)
    {
        if (string.IsNullOrEmpty(address))
            return false;

        // Native SegWit address (bc1...)
        if (address.StartsWith("bc1") && address.Length >= 42 && address.Length <= 62)
            return address.Skip(3).All(c => IsBech32Char(c));

        return false;
    }

    private static int FindBytesFrom(byte[] data, byte[] pattern, int startIndex)
    {
        for (int i = startIndex; i <= data.Length - pattern.Length; i++)
        {
            bool match = true;
            for (int j = 0; j < pattern.Length; j++)
            {
                if (data[i + j] != pattern[j])
                {
                    match = false;
                    break;
                }
            }
            if (match) return i;
        }
        return -1;
    }

    private static string ToHex(byte[] bytes)
    {
        var sb = new StringBuilder(bytes.Length * 2);
        foreach (byte b in bytes)
            sb.Append(b.ToString("x2"));
        return sb.ToString();
    }

    private struct MKeyData
    {
        public byte[] EncryptedKey;
        public byte[] Salt;
        public int Method;
        public int Iterations;
    }

    private struct CKeyData
    {
        public byte[]? PublicKey;
        public byte[]? EncryptedPrivateKey;
    }
}
