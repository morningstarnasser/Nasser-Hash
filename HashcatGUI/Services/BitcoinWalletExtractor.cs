using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace HashcatGUI.Services;

/// <summary>
/// Extracts password hashes from Bitcoin wallet.dat files for hashcat mode 11300.
/// Based on bitcoin2john.py from JohnTheRipper.
///
/// Hashcat format for mode 11300:
/// $bitcoin$<cry_master_len>$<cry_master>$<cry_salt_len>$<cry_salt>$<cry_rounds>$<ckey_len>$<ckey>$<public_key_len>$<public_key>
///
/// Example from hashcat:
/// $bitcoin$96$c265931309b4a59307921cf054b4ec6b6e4554369be79802e94e16477645777d948ae1d375191831efc78e5acd1f0443$16$8017214013543185$200460$96$480008005625057442352316337722323437108374245623701184230273883222762730232857701607167815448714$66$014754433300175043011633205413774877455616682000536368706315333388
/// </summary>
public static class BitcoinWalletExtractor
{
    /// <summary>
    /// Extracts the hash from a Bitcoin wallet.dat file.
    /// </summary>
    public static async Task<string?> ExtractHashAsync(string walletPath)
    {
        if (!File.Exists(walletPath))
            return null;

        try
        {
            var walletData = await File.ReadAllBytesAsync(walletPath);
            return ParseWallet(walletData);
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Parse the wallet.dat Berkeley DB file and extract the hash.
    /// </summary>
    private static string? ParseWallet(byte[] data)
    {
        // Find the CMasterKey structure by searching for the pattern:
        // 0x30 (48 bytes) + <48 bytes encrypted key> + 0x08 (8 bytes) + <8 bytes salt> + method(4) + iterations(4)
        var mkey = FindMKeyByPattern(data);
        if (mkey == null)
            return null;

        // Find a ckey entry - contains encrypted private key and public key
        var ckey = FindCKeyEntry(data);

        // Build the hash string
        return BuildHash(mkey.Value, ckey);
    }

    /// <summary>
    /// Searches for CMasterKey by looking for the byte pattern:
    /// 0x30 + 48 bytes + 0x08 + 8 bytes + 4 bytes (method) + 4 bytes (iterations with reasonable value)
    /// </summary>
    private static MKeyData? FindMKeyByPattern(byte[] data)
    {
        // Search for pattern: 0x30 <48 bytes> 0x08 <8 bytes> <method 4 bytes> <iterations 4 bytes>
        for (int i = 0; i < data.Length - 70; i++)
        {
            if (data[i] == 0x30) // 48-byte encrypted key length prefix
            {
                // Check if position after 48 bytes has 0x08 (8-byte salt length)
                int saltLenPos = i + 49;
                if (saltLenPos >= data.Length)
                    continue;

                if (data[saltLenPos] == 0x08) // Salt length marker
                {
                    // Read iterations to validate
                    int iterationsPos = saltLenPos + 9 + 4; // salt_len(1) + salt(8) + method(4)
                    if (iterationsPos + 4 > data.Length)
                        continue;

                    int iterations = BitConverter.ToInt32(data, iterationsPos);

                    // Validate iterations - should be between 1000 and 1000000 for most wallets
                    if (iterations >= 1000 && iterations <= 1000000)
                    {
                        // Found valid structure
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
    /// Searches for and parses a ckey (encrypted private key) entry.
    /// Format: \x04ckey followed by public key and encrypted private key
    /// </summary>
    private static CKeyData? FindCKeyEntry(byte[] data)
    {
        // Search for ckey marker with length prefix: \x04ckey
        byte[] marker = { 0x04, 0x63, 0x6b, 0x65, 0x79 }; // \x04ckey

        int pos = FindBytes(data, marker);
        if (pos == -1)
        {
            // Try without length prefix
            marker = new byte[] { 0x63, 0x6b, 0x65, 0x79 }; // ckey
            pos = FindBytes(data, marker);
            if (pos == -1)
                return null;
            pos += 4; // Move past 'ckey'
        }
        else
        {
            pos += 5; // Move past \x04ckey
        }

        byte[]? publicKey = null;
        byte[]? encryptedPrivateKey = null;

        // Scan for public key and encrypted private key
        for (int offset = 0; offset < 300 && pos + offset + 100 < data.Length; offset++)
        {
            int checkPos = pos + offset;

            // Look for compressed public key (33 bytes starting with 02 or 03)
            if (publicKey == null)
            {
                // Check for length prefix 0x21 (33 decimal) followed by 02 or 03
                if (data[checkPos] == 0x21 && checkPos + 34 < data.Length)
                {
                    byte nextByte = data[checkPos + 1];
                    if (nextByte == 0x02 || nextByte == 0x03)
                    {
                        publicKey = new byte[33];
                        Array.Copy(data, checkPos + 1, publicKey, 0, 33);
                    }
                }
                // Direct public key without length prefix
                else if ((data[checkPos] == 0x02 || data[checkPos] == 0x03) && checkPos + 33 < data.Length)
                {
                    // Verify it looks like a public key (check if followed by reasonable data)
                    publicKey = new byte[33];
                    Array.Copy(data, checkPos, publicKey, 0, 33);
                }
            }

            // Look for encrypted private key (48 bytes with 0x30 length prefix)
            if (encryptedPrivateKey == null && data[checkPos] == 0x30 && checkPos + 49 < data.Length)
            {
                encryptedPrivateKey = new byte[48];
                Array.Copy(data, checkPos + 1, encryptedPrivateKey, 0, 48);
            }

            // Once we have both, we can stop
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
    /// Builds the hashcat-compatible hash string.
    /// Format: $bitcoin$<len>$<master_key>$<len>$<salt>$<iterations>$<len>$<ckey>$<len>$<pubkey>
    /// </summary>
    private static string BuildHash(MKeyData mkey, CKeyData? ckey)
    {
        var sb = new StringBuilder();

        // Start with bitcoin marker
        sb.Append("$bitcoin$");

        // Encrypted master key (length in hex chars = bytes * 2)
        sb.Append(mkey.EncryptedKey.Length * 2);
        sb.Append('$');
        sb.Append(ToHex(mkey.EncryptedKey));

        // Salt (8 bytes = 16 hex chars)
        sb.Append('$');
        sb.Append(mkey.Salt.Length * 2);
        sb.Append('$');
        sb.Append(ToHex(mkey.Salt));

        // Iterations
        sb.Append('$');
        sb.Append(mkey.Iterations);

        // Add ckey (encrypted private key) if available
        if (ckey.HasValue && ckey.Value.EncryptedPrivateKey != null)
        {
            sb.Append('$');
            sb.Append(ckey.Value.EncryptedPrivateKey.Length * 2);
            sb.Append('$');
            sb.Append(ToHex(ckey.Value.EncryptedPrivateKey));
        }

        // Add public key if available
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
            if (match)
                return i;
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

    /// <summary>
    /// Simplified extraction as fallback.
    /// </summary>
    public static async Task<string?> ExtractSimpleHashAsync(string walletPath)
    {
        return await ExtractHashAsync(walletPath);
    }

    /// <summary>
    /// Validates if a file appears to be a Bitcoin wallet.
    /// </summary>
    public static async Task<bool> IsValidWalletAsync(string walletPath)
    {
        if (!File.Exists(walletPath))
            return false;

        try
        {
            using var fs = new FileStream(walletPath, FileMode.Open, FileAccess.Read);
            var buffer = new byte[Math.Min(fs.Length, 1024 * 1024)];
            await fs.ReadAsync(buffer, 0, buffer.Length);

            // Look for mkey or ckey markers
            byte[] mkeyMarker = { 0x6d, 0x6b, 0x65, 0x79 }; // mkey
            byte[] ckeyMarker = { 0x63, 0x6b, 0x65, 0x79 }; // ckey

            return FindBytes(buffer, mkeyMarker) != -1 || FindBytes(buffer, ckeyMarker) != -1;
        }
        catch
        {
            return false;
        }
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
