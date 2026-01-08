using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using HashcatGUI.Models;

namespace HashcatGUI.Services;

public class HashcatService : IDisposable
{
    private Process? _hashcatProcess;
    private readonly object _lock = new();
    private bool _isDisposed;

    private static readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        AllowTrailingCommas = true,
        ReadCommentHandling = JsonCommentHandling.Skip
    };

    public event EventHandler<string>? OutputReceived;
    public event EventHandler<string>? ErrorReceived;
    public event EventHandler<HashcatStatusJson>? StatusUpdated;
    public event EventHandler<CrackedHash>? HashCracked;
    public event EventHandler<int>? ProcessExited;

    public bool IsRunning => _hashcatProcess is { HasExited: false };
    public string HashcatPath { get; set; } = string.Empty;

    public string BuildCommandLine(HashcatConfig config)
    {
        var args = new StringBuilder();

        // Attack mode
        args.Append($"-a {config.AttackMode} ");

        // Hash mode
        args.Append($"-m {config.HashMode} ");

        // Workload profile
        args.Append($"-w {config.WorkloadProfile} ");

        // Status output
        if (config.StatusJson)
        {
            args.Append("--status --status-json ");
            args.Append($"--status-timer={config.StatusTimer} ");
        }

        // Devices
        if (!string.IsNullOrEmpty(config.Devices))
            args.Append($"-d {config.Devices} ");

        if (!string.IsNullOrEmpty(config.DeviceTypes))
            args.Append($"-D {config.DeviceTypes} ");

        // Optimized kernels
        if (config.OptimizedKernels)
            args.Append("-O ");

        // Output file
        if (!string.IsNullOrEmpty(config.OutputFile))
        {
            args.Append($"-o \"{config.OutputFile}\" ");
            args.Append($"--outfile-format={config.OutputFormat} ");
        }

        // Potfile
        if (config.DisablePotfile)
            args.Append("--potfile-disable ");

        // Force mode
        if (config.ForceMode)
            args.Append("--force ");

        // Rules
        foreach (var ruleFile in config.RuleFiles)
        {
            args.Append($"-r \"{ruleFile}\" ");
        }

        // Custom charsets
        if (!string.IsNullOrEmpty(config.CustomCharset1))
            args.Append($"-1 \"{config.CustomCharset1}\" ");
        if (!string.IsNullOrEmpty(config.CustomCharset2))
            args.Append($"-2 \"{config.CustomCharset2}\" ");
        if (!string.IsNullOrEmpty(config.CustomCharset3))
            args.Append($"-3 \"{config.CustomCharset3}\" ");
        if (!string.IsNullOrEmpty(config.CustomCharset4))
            args.Append($"-4 \"{config.CustomCharset4}\" ");

        // Increment mode
        if (config.IncrementMode)
        {
            args.Append("-i ");
            args.Append($"--increment-min={config.IncrementMin} ");
            args.Append($"--increment-max={config.IncrementMax} ");
        }

        // Runtime limit
        if (config.RuntimeLimit.HasValue)
            args.Append($"--runtime={config.RuntimeLimit.Value} ");

        // Temperature abort
        if (config.TempAbort.HasValue)
            args.Append($"--hwmon-temp-abort={config.TempAbort.Value} ");

        // Markov options
        if (config.MarkovDisable)
            args.Append("--markov-disable ");
        if (config.MarkovThreshold.HasValue)
            args.Append($"-t {config.MarkovThreshold.Value} ");

        // Loopback
        if (config.LoopbackMode)
            args.Append("--loopback ");

        // Keep guessing
        if (config.KeepGuessing)
            args.Append("--keep-guessing ");

        // Slow candidates
        if (config.SlowCandidates)
            args.Append("-S ");

        // Hash file
        args.Append($"\"{config.HashFile}\" ");

        // Attack-specific inputs
        switch (config.AttackMode)
        {
            case 0: // Straight
                if (!string.IsNullOrEmpty(config.Wordlist))
                    args.Append($"\"{config.Wordlist}\"");
                break;

            case 1: // Combination
                if (!string.IsNullOrEmpty(config.Wordlist))
                    args.Append($"\"{config.Wordlist}\" ");
                if (!string.IsNullOrEmpty(config.SecondWordlist))
                    args.Append($"\"{config.SecondWordlist}\"");
                break;

            case 3: // Brute-force
                if (!string.IsNullOrEmpty(config.Mask))
                    args.Append(config.Mask); // No quotes for masks
                break;

            case 6: // Hybrid Wordlist + Mask
                if (!string.IsNullOrEmpty(config.Wordlist))
                    args.Append($"\"{config.Wordlist}\" ");
                if (!string.IsNullOrEmpty(config.Mask))
                    args.Append(config.Mask); // No quotes for masks
                break;

            case 7: // Hybrid Mask + Wordlist
                if (!string.IsNullOrEmpty(config.Mask))
                    args.Append($"{config.Mask} "); // No quotes for masks
                if (!string.IsNullOrEmpty(config.Wordlist))
                    args.Append($"\"{config.Wordlist}\"");
                break;

            case 9: // Association
                if (!string.IsNullOrEmpty(config.Wordlist))
                    args.Append($"\"{config.Wordlist}\"");
                break;
        }

        return args.ToString().Trim();
    }

    public async Task<bool> StartAsync(HashcatConfig config)
    {
        if (IsRunning)
            return false;

        if (string.IsNullOrEmpty(HashcatPath) || !File.Exists(HashcatPath))
            throw new FileNotFoundException("Hashcat executable not found", HashcatPath);

        var arguments = BuildCommandLine(config);
        var workingDir = Path.GetDirectoryName(HashcatPath) ?? Environment.CurrentDirectory;

        var startInfo = new ProcessStartInfo
        {
            FileName = HashcatPath,
            Arguments = arguments,
            WorkingDirectory = workingDir,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            RedirectStandardInput = true,
            CreateNoWindow = true,
            StandardOutputEncoding = Encoding.UTF8,
            StandardErrorEncoding = Encoding.UTF8
        };

        _hashcatProcess = new Process { StartInfo = startInfo };
        _hashcatProcess.OutputDataReceived += OnOutputDataReceived;
        _hashcatProcess.ErrorDataReceived += OnErrorDataReceived;
        _hashcatProcess.Exited += OnProcessExited;
        _hashcatProcess.EnableRaisingEvents = true;

        try
        {
            _hashcatProcess.Start();
            _hashcatProcess.BeginOutputReadLine();
            _hashcatProcess.BeginErrorReadLine();
            return true;
        }
        catch
        {
            _hashcatProcess.Dispose();
            _hashcatProcess = null;
            throw;
        }
    }

    public async Task<List<BenchmarkResult>> RunBenchmarkAsync(int? hashMode = null, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(HashcatPath) || !File.Exists(HashcatPath))
            throw new FileNotFoundException("Hashcat executable not found", HashcatPath);

        var results = new List<BenchmarkResult>();
        var arguments = hashMode.HasValue ? $"-b -m {hashMode.Value}" : "-b";
        var workingDir = Path.GetDirectoryName(HashcatPath) ?? Environment.CurrentDirectory;

        var startInfo = new ProcessStartInfo
        {
            FileName = HashcatPath,
            Arguments = arguments,
            WorkingDirectory = workingDir,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true,
            StandardOutputEncoding = Encoding.UTF8
        };

        using var process = new Process { StartInfo = startInfo };
        var output = new StringBuilder();

        process.OutputDataReceived += (s, e) =>
        {
            if (!string.IsNullOrEmpty(e.Data))
            {
                output.AppendLine(e.Data);
                OutputReceived?.Invoke(this, e.Data);
            }
        };

        process.ErrorDataReceived += (s, e) =>
        {
            if (!string.IsNullOrEmpty(e.Data))
                ErrorReceived?.Invoke(this, e.Data);
        };

        process.Start();
        process.BeginOutputReadLine();
        process.BeginErrorReadLine();

        await process.WaitForExitAsync(cancellationToken);

        // Parse benchmark results
        var lines = output.ToString().Split('\n');
        var speedRegex = new Regex(@"Speed\.#\d+.*?:\s+([\d.]+)\s*(\w+H/s)", RegexOptions.Compiled);
        var modeRegex = new Regex(@"Hashmode:\s+(\d+)\s+-\s+(.+)", RegexOptions.Compiled);

        int currentMode = 0;
        string currentName = "";

        foreach (var line in lines)
        {
            var modeMatch = modeRegex.Match(line);
            if (modeMatch.Success)
            {
                currentMode = int.Parse(modeMatch.Groups[1].Value);
                currentName = modeMatch.Groups[2].Value.Trim();
            }

            var speedMatch = speedRegex.Match(line);
            if (speedMatch.Success && currentMode > 0)
            {
                var speed = double.Parse(speedMatch.Groups[1].Value);
                var unit = speedMatch.Groups[2].Value;

                // Convert to H/s
                speed = unit switch
                {
                    "TH/s" => speed * 1_000_000_000_000,
                    "GH/s" => speed * 1_000_000_000,
                    "MH/s" => speed * 1_000_000,
                    "kH/s" => speed * 1_000,
                    _ => speed
                };

                results.Add(new BenchmarkResult
                {
                    HashMode = currentMode,
                    HashName = currentName,
                    Speed = speed,
                    SpeedUnit = "H/s"
                });
            }
        }

        return results;
    }

    public async Task<List<DeviceInfo>> GetDevicesAsync()
    {
        if (string.IsNullOrEmpty(HashcatPath) || !File.Exists(HashcatPath))
            throw new FileNotFoundException("Hashcat executable not found", HashcatPath);

        var devices = new List<DeviceInfo>();
        var workingDir = Path.GetDirectoryName(HashcatPath) ?? Environment.CurrentDirectory;

        var startInfo = new ProcessStartInfo
        {
            FileName = HashcatPath,
            Arguments = "-I",
            WorkingDirectory = workingDir,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true,
            StandardOutputEncoding = Encoding.UTF8
        };

        using var process = new Process { StartInfo = startInfo };
        var output = new StringBuilder();

        process.OutputDataReceived += (s, e) =>
        {
            if (!string.IsNullOrEmpty(e.Data))
                output.AppendLine(e.Data);
        };

        process.Start();
        process.BeginOutputReadLine();
        await process.WaitForExitAsync();

        // Parse device info
        var lines = output.ToString().Split('\n');
        var deviceRegex = new Regex(@"Backend Device ID #(\d+).*", RegexOptions.Compiled);
        var nameRegex = new Regex(@"Name\.+:\s+(.+)", RegexOptions.Compiled);
        var typeRegex = new Regex(@"Type\.+:\s+(.+)", RegexOptions.Compiled);

        int currentId = 0;
        string currentName = "";
        string currentType = "";

        foreach (var line in lines)
        {
            var deviceMatch = deviceRegex.Match(line);
            if (deviceMatch.Success)
            {
                if (currentId > 0 && !string.IsNullOrEmpty(currentName))
                {
                    devices.Add(new DeviceInfo
                    {
                        DeviceId = currentId,
                        DeviceName = currentName,
                        DeviceType = currentType
                    });
                }
                currentId = int.Parse(deviceMatch.Groups[1].Value);
                currentName = "";
                currentType = "";
            }

            var nameMatch = nameRegex.Match(line);
            if (nameMatch.Success)
                currentName = nameMatch.Groups[1].Value.Trim();

            var typeMatch = typeRegex.Match(line);
            if (typeMatch.Success)
                currentType = typeMatch.Groups[1].Value.Trim();
        }

        // Add last device
        if (currentId > 0 && !string.IsNullOrEmpty(currentName))
        {
            devices.Add(new DeviceInfo
            {
                DeviceId = currentId,
                DeviceName = currentName,
                DeviceType = currentType
            });
        }

        return devices;
    }

    public void SendCommand(char command)
    {
        if (_hashcatProcess is { HasExited: false })
        {
            try
            {
                _hashcatProcess.StandardInput.Write(command);
                _hashcatProcess.StandardInput.Flush();
            }
            catch { }
        }
    }

    public void Pause() => SendCommand('p');
    public void Resume() => SendCommand('r');
    public void Checkpoint() => SendCommand('c');
    public void Status() => SendCommand('s');
    public void Quit() => SendCommand('q');
    public void Bypass() => SendCommand('b');

    public void Stop()
    {
        lock (_lock)
        {
            if (_hashcatProcess is { HasExited: false })
            {
                try
                {
                    Quit();
                    if (!_hashcatProcess.WaitForExit(5000))
                    {
                        _hashcatProcess.Kill();
                    }
                }
                catch { }
            }
        }
    }

    // Fix invalid JSON escape sequences from hashcat (Windows paths like \Users become invalid)
    private static string FixJsonEscapeSequences(string json)
    {
        // Replace backslashes that are followed by characters that aren't valid JSON escapes
        // Valid JSON escapes: \", \\, \/, \b, \f, \n, \r, \t, \uXXXX (where XXXX are hex digits)
        var result = new StringBuilder(json.Length * 2);
        for (int i = 0; i < json.Length; i++)
        {
            if (json[i] == '\\' && i + 1 < json.Length)
            {
                char next = json[i + 1];
                // Check if it's a valid JSON escape
                bool isValidEscape = next == '"' || next == '\\' || next == '/' ||
                                     next == 'b' || next == 'f' || next == 'n' || next == 'r' || next == 't';

                // Check for \uXXXX (must be followed by exactly 4 hex digits)
                if (!isValidEscape && next == 'u' && i + 5 < json.Length)
                {
                    isValidEscape = IsHexDigit(json[i + 2]) && IsHexDigit(json[i + 3]) &&
                                    IsHexDigit(json[i + 4]) && IsHexDigit(json[i + 5]);
                }

                if (isValidEscape)
                {
                    result.Append(json[i]);
                }
                else
                {
                    // Invalid escape - double the backslash to make it valid JSON
                    result.Append("\\\\");
                }
            }
            else
            {
                result.Append(json[i]);
            }
        }
        return result.ToString();
    }

    private static bool IsHexDigit(char c)
    {
        return (c >= '0' && c <= '9') || (c >= 'a' && c <= 'f') || (c >= 'A' && c <= 'F');
    }

    // Regex patterns for parsing hashcat text output - made more flexible
    private static readonly Regex SpeedRegex = new(@"Speed[.#\d\*]*\s*[:\.]+\s*([\d.,]+)\s*([kMGT]?H/s)", RegexOptions.Compiled | RegexOptions.IgnoreCase);
    private static readonly Regex ProgressRegex = new(@"Progress\s*[:\.]+\s*([\d]+)\s*/\s*([\d]+)", RegexOptions.Compiled | RegexOptions.IgnoreCase);
    private static readonly Regex EtaRegex = new(@"Time\.Estimated|ETA\s*[:\.]+\s*(.+?)(?:\s*\(|$)", RegexOptions.Compiled | RegexOptions.IgnoreCase);
    private static readonly Regex RecoveredRegex = new(@"Recovered\s*[:\.]+\s*(\d+)\s*/\s*(\d+)", RegexOptions.Compiled | RegexOptions.IgnoreCase);

    private void OnOutputDataReceived(object sender, DataReceivedEventArgs e)
    {
        if (string.IsNullOrEmpty(e.Data))
            return;

        OutputReceived?.Invoke(this, e.Data);

        // Try to parse JSON status first
        if (e.Data.StartsWith("{"))
        {
            try
            {
                // Fix invalid escape sequences in Windows paths (e.g., \U, \D, \P become \\U, \\D, \\P)
                var fixedJson = FixJsonEscapeSequences(e.Data);
                var status = JsonSerializer.Deserialize<HashcatStatusJson>(fixedJson, _jsonOptions);
                if (status != null)
                {
                    StatusUpdated?.Invoke(this, status);
                    return;
                }
            }
            catch (Exception ex)
            {
                // JSON parsing failed - log the error for debugging
                System.Diagnostics.Debug.WriteLine($"JSON parse error: {ex.Message}");
            }
        }

        // Fallback: Parse text-based status output
        TryParseTextStatus(e.Data);

        // Check for cracked hashes - must be very specific to avoid false positives
        // Cracked hashes are output as: hash:password (on a line by itself, no extra info)
        // Exclude: status messages, errors, info lines, progress indicators
        if (e.Data.Contains(":") && !e.Data.StartsWith("{") && !e.Data.StartsWith("["))
        {
            // Skip common non-hash lines
            var lowerData = e.Data.ToLowerInvariant();
            bool isStatusLine = lowerData.Contains("speed") ||
                               lowerData.Contains("recovered") ||
                               lowerData.Contains("progress") ||
                               lowerData.Contains("rejected") ||
                               lowerData.Contains("restore") ||
                               lowerData.Contains("candidates") ||
                               lowerData.Contains("hardware") ||
                               lowerData.Contains("hashfile") ||
                               lowerData.Contains("session") ||
                               lowerData.Contains("status") ||
                               lowerData.Contains("started") ||
                               lowerData.Contains("stopped") ||
                               lowerData.Contains("running") ||
                               lowerData.Contains("time.") ||
                               lowerData.Contains("gpu") ||
                               lowerData.Contains("device") ||
                               lowerData.Contains("kernel") ||
                               lowerData.Contains("token") ||
                               lowerData.Contains("separator") ||
                               lowerData.Contains("hash.") ||
                               lowerData.Contains("exception") ||
                               lowerData.Contains("error") ||
                               lowerData.Contains("warning") ||
                               e.Data.Contains("...") ||
                               e.Data.Contains("##") ||
                               e.Data.Contains("==") ||
                               e.Data.StartsWith("*") ||
                               e.Data.StartsWith("-") ||
                               e.Data.Length > 500; // Real cracked output shouldn't be this long

            if (!isStatusLine)
            {
                var parts = e.Data.Split(':');
                // Valid cracked hash: at least hash:password format
                // Hash part should look like a hash (hex chars, or $format$ style)
                if (parts.Length >= 2 && parts[0].Length >= 8)
                {
                    var hashPart = parts[0].Trim();
                    var passwordPart = parts[^1].Trim();

                    // Check if hash part looks valid (hex or starts with $)
                    bool looksLikeHash = hashPart.StartsWith("$") ||
                                         System.Text.RegularExpressions.Regex.IsMatch(hashPart, "^[a-fA-F0-9]+$");

                    if (looksLikeHash && passwordPart.Length > 0)
                    {
                        var crackedHash = new CrackedHash
                        {
                            Hash = hashPart,
                            Password = passwordPart,
                            CrackedAt = DateTime.Now
                        };
                        HashCracked?.Invoke(this, crackedHash);
                    }
                }
            }
        }
    }

    private void OnErrorDataReceived(object sender, DataReceivedEventArgs e)
    {
        if (!string.IsNullOrEmpty(e.Data))
        {
            ErrorReceived?.Invoke(this, e.Data);
        }
    }

    private void OnProcessExited(object? sender, EventArgs e)
    {
        var exitCode = _hashcatProcess?.ExitCode ?? -1;
        ProcessExited?.Invoke(this, exitCode);
    }

    public List<RuleFile> GetAvailableRules(string rulesPath)
    {
        var rules = new List<RuleFile>();

        if (!Directory.Exists(rulesPath))
            return rules;

        foreach (var file in Directory.GetFiles(rulesPath, "*.rule"))
        {
            var info = new FileInfo(file);
            var fileName = Path.GetFileName(file);

            rules.Add(new RuleFile
            {
                Name = fileName,
                FullPath = file,
                Size = info.Length,
                Description = RuleFile.KnownDescriptions.TryGetValue(fileName, out var desc) ? desc : ""
            });
        }

        return rules.OrderBy(r => r.Name).ToList();
    }

    public List<WordlistFile> GetAvailableWordlists(string path)
    {
        var wordlists = new List<WordlistFile>();

        if (!Directory.Exists(path))
            return wordlists;

        var extensions = new[] { "*.txt", "*.dict", "*.lst", "*.wordlist" };

        foreach (var ext in extensions)
        {
            foreach (var file in Directory.GetFiles(path, ext))
            {
                var info = new FileInfo(file);
                wordlists.Add(new WordlistFile
                {
                    Name = Path.GetFileName(file),
                    FullPath = file,
                    Size = info.Length
                });
            }
        }

        return wordlists.OrderByDescending(w => w.Size).ToList();
    }

    public List<MaskFile> GetAvailableMasks(string masksPath)
    {
        var masks = new List<MaskFile>();

        if (!Directory.Exists(masksPath))
            return masks;

        foreach (var file in Directory.GetFiles(masksPath, "*.hcmask"))
        {
            var fileName = Path.GetFileName(file);
            masks.Add(new MaskFile
            {
                Name = fileName,
                FullPath = file,
                Description = MaskFile.KnownDescriptions.TryGetValue(fileName, out var desc) ? desc : ""
            });
        }

        return masks.OrderBy(m => m.Name).ToList();
    }

    public async Task<List<CrackedHash>> ReadPotfileAsync(string potfilePath)
    {
        var hashes = new List<CrackedHash>();

        if (!File.Exists(potfilePath))
            return hashes;

        var lines = await File.ReadAllLinesAsync(potfilePath);
        foreach (var line in lines)
        {
            if (string.IsNullOrWhiteSpace(line))
                continue;

            var lastColon = line.LastIndexOf(':');
            if (lastColon > 0)
            {
                hashes.Add(new CrackedHash
                {
                    Hash = line[..lastColon],
                    Password = line[(lastColon + 1)..]
                });
            }
        }

        return hashes;
    }

    // Accumulated text status for building a complete status object
    private HashcatStatusJson _textStatus = new();
    private DateTime _lastTextStatusUpdate = DateTime.MinValue;

    private void TryParseTextStatus(string line)
    {
        bool updated = false;

        // Parse Speed line - multiple formats supported
        var speedMatch = SpeedRegex.Match(line);
        if (speedMatch.Success)
        {
            // Handle both . and , as decimal separator
            var speedStr = speedMatch.Groups[1].Value.Replace(",", ".");
            if (double.TryParse(speedStr, System.Globalization.NumberStyles.Float,
                System.Globalization.CultureInfo.InvariantCulture, out var speed))
            {
                var unit = speedMatch.Groups[2].Value.ToUpperInvariant();
                // Convert to H/s
                long speedHs = unit switch
                {
                    "TH/S" => (long)(speed * 1_000_000_000_000),
                    "GH/S" => (long)(speed * 1_000_000_000),
                    "MH/S" => (long)(speed * 1_000_000),
                    "KH/S" => (long)(speed * 1_000),
                    _ => (long)speed
                };
                _textStatus.Devices = new[] { new DeviceInfo { Speed = speedHs, DeviceId = 1, DeviceName = "Device" } };
                updated = true;
            }
        }

        // Parse Progress line
        var progressMatch = ProgressRegex.Match(line);
        if (progressMatch.Success)
        {
            if (long.TryParse(progressMatch.Groups[1].Value, out var current) &&
                long.TryParse(progressMatch.Groups[2].Value, out var total))
            {
                _textStatus.Progress = new[] { current, total };
                updated = true;
            }
        }

        // Parse ETA line
        var etaMatch = EtaRegex.Match(line);
        if (etaMatch.Success && etaMatch.Groups.Count > 1)
        {
            var etaStr = etaMatch.Groups[1].Value.Trim();
            if (DateTime.TryParse(etaStr, out var etaTime))
            {
                _textStatus.EstimatedStop = new DateTimeOffset(etaTime).ToUnixTimeSeconds();
                updated = true;
            }
        }

        // Parse Recovered line
        var recoveredMatch = RecoveredRegex.Match(line);
        if (recoveredMatch.Success)
        {
            if (int.TryParse(recoveredMatch.Groups[1].Value, out var recovered) &&
                int.TryParse(recoveredMatch.Groups[2].Value, out var total))
            {
                _textStatus.RecoveredHashes = new[] { recovered, total };
                updated = true;
            }
        }

        // Send status update immediately if we parsed something
        if (updated)
        {
            _lastTextStatusUpdate = DateTime.Now;
            StatusUpdated?.Invoke(this, _textStatus);
        }
    }

    public void Dispose()
    {
        if (_isDisposed)
            return;

        _isDisposed = true;
        Stop();
        _hashcatProcess?.Dispose();
    }
}
