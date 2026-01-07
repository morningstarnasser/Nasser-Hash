using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace HashcatGUI.Models;

public class HashcatSession
{
    public string Name { get; set; } = string.Empty;
    public DateTime StartTime { get; set; }
    public DateTime? EndTime { get; set; }
    public SessionStatus Status { get; set; }
    public HashcatConfig Config { get; set; } = new();
    public SessionProgress Progress { get; set; } = new();
    public List<CrackedHash> CrackedHashes { get; set; } = new();
}

public enum SessionStatus
{
    Pending,
    Running,
    Paused,
    Completed,
    Exhausted,
    Aborted,
    Error
}

public class HashcatConfig
{
    public int AttackMode { get; set; }
    public int HashMode { get; set; }
    public string HashFile { get; set; } = string.Empty;
    public string? Wordlist { get; set; }
    public string? SecondWordlist { get; set; }
    public string? Mask { get; set; }
    public List<string> RuleFiles { get; set; } = new();
    public string? CustomCharset1 { get; set; }
    public string? CustomCharset2 { get; set; }
    public string? CustomCharset3 { get; set; }
    public string? CustomCharset4 { get; set; }
    public bool IncrementMode { get; set; }
    public int IncrementMin { get; set; } = 1;
    public int IncrementMax { get; set; } = 8;
    public int WorkloadProfile { get; set; } = 2;
    public string? Devices { get; set; }
    public string? DeviceTypes { get; set; }
    public bool OptimizedKernels { get; set; }
    public string? OutputFile { get; set; }
    public int OutputFormat { get; set; } = 2;
    public bool DisablePotfile { get; set; }
    public bool ForceMode { get; set; }
    public bool StatusJson { get; set; } = true;
    public int StatusTimer { get; set; } = 10;
    public int? RuntimeLimit { get; set; }
    public int? TempAbort { get; set; }
    public bool MarkovDisable { get; set; }
    public int? MarkovThreshold { get; set; }
    public bool LoopbackMode { get; set; }
    public bool KeepGuessing { get; set; }
    public bool SlowCandidates { get; set; }
}

public class SessionProgress
{
    public long TotalHashes { get; set; }
    public long CrackedHashes { get; set; }
    public long Progress { get; set; }
    public long TotalCandidates { get; set; }
    public double ProgressPercent => TotalCandidates > 0 ? (double)Progress / TotalCandidates * 100 : 0;
    public double Speed { get; set; }
    public string SpeedUnit { get; set; } = "H/s";
    public TimeSpan ElapsedTime { get; set; }
    public TimeSpan? EstimatedTime { get; set; }
    public double Temperature { get; set; }
    public int Utilization { get; set; }
}

public class CrackedHash
{
    public string Hash { get; set; } = string.Empty;
    public string? Salt { get; set; }
    public string Password { get; set; } = string.Empty;
    public DateTime CrackedAt { get; set; }
}

public class HashcatStatusJson
{
    [JsonPropertyName("session")]
    public string Session { get; set; } = string.Empty;

    [JsonPropertyName("guess")]
    public GuessInfo? Guess { get; set; }

    [JsonPropertyName("status")]
    public int Status { get; set; }

    [JsonPropertyName("target")]
    public string Target { get; set; } = string.Empty;

    [JsonPropertyName("progress")]
    public long[]? Progress { get; set; }

    [JsonPropertyName("restore_point")]
    public long RestorePoint { get; set; }

    [JsonPropertyName("recovered_hashes")]
    public int[]? RecoveredHashes { get; set; }

    [JsonPropertyName("recovered_salts")]
    public int[]? RecoveredSalts { get; set; }

    [JsonPropertyName("rejected")]
    public long Rejected { get; set; }

    [JsonPropertyName("devices")]
    public DeviceInfo[]? Devices { get; set; }

    [JsonPropertyName("time_start")]
    public long TimeStart { get; set; }

    [JsonPropertyName("estimated_stop")]
    public long EstimatedStop { get; set; }
}

public class GuessInfo
{
    [JsonPropertyName("guess_base")]
    public string? GuessBase { get; set; }

    [JsonPropertyName("guess_base_count")]
    public long GuessBaseCount { get; set; }

    [JsonPropertyName("guess_base_offset")]
    public long GuessBaseOffset { get; set; }

    [JsonPropertyName("guess_base_percent")]
    public double GuessBasePercent { get; set; }

    [JsonPropertyName("guess_mask_length")]
    public int? GuessMaskLength { get; set; }

    [JsonPropertyName("guess_mod")]
    public string? GuessMod { get; set; }

    [JsonPropertyName("guess_mod_count")]
    public long GuessModCount { get; set; }

    [JsonPropertyName("guess_mod_offset")]
    public long GuessModOffset { get; set; }

    [JsonPropertyName("guess_mod_percent")]
    public double GuessModPercent { get; set; }

    [JsonPropertyName("guess_mode")]
    public int GuessMode { get; set; }
}

public class DeviceInfo
{
    [JsonPropertyName("device_id")]
    public int DeviceId { get; set; }

    [JsonPropertyName("device_name")]
    public string DeviceName { get; set; } = string.Empty;

    [JsonPropertyName("device_type")]
    public string DeviceType { get; set; } = string.Empty;

    [JsonPropertyName("speed")]
    public long Speed { get; set; }

    [JsonPropertyName("temp")]
    public int? Temp { get; set; }

    [JsonPropertyName("util")]
    public int? Util { get; set; }

    [JsonPropertyName("fanspeed")]
    public int? FanSpeed { get; set; }

    [JsonPropertyName("corespeed")]
    public int? CoreSpeed { get; set; }

    [JsonPropertyName("memoryspeed")]
    public int? MemorySpeed { get; set; }

    [JsonPropertyName("buslanes")]
    public int? BusLanes { get; set; }
}

public class BenchmarkResult
{
    public int HashMode { get; set; }
    public string HashName { get; set; } = string.Empty;
    public double Speed { get; set; }
    public string SpeedUnit { get; set; } = "H/s";
    public string SpeedFormatted => FormatSpeed(Speed);

    private static string FormatSpeed(double speed)
    {
        if (speed >= 1_000_000_000_000)
            return $"{speed / 1_000_000_000_000:F2} TH/s";
        if (speed >= 1_000_000_000)
            return $"{speed / 1_000_000_000:F2} GH/s";
        if (speed >= 1_000_000)
            return $"{speed / 1_000_000:F2} MH/s";
        if (speed >= 1_000)
            return $"{speed / 1_000:F2} kH/s";
        return $"{speed:F2} H/s";
    }
}
