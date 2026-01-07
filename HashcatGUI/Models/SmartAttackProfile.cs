using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace HashcatGUI.Models;

/// <summary>
/// Represents a smart attack profile generated from wallet analysis.
/// </summary>
public class SmartAttackProfile
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public WalletEra TargetEra { get; set; }
    public int EstimatedDurationMinutes { get; set; }
    public double SuccessProbability { get; set; }
    public List<AttackPhase> Phases { get; set; } = new();
}

public class AttackPhase
{
    public int Order { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public int AttackMode { get; set; }
    public string? Wordlist { get; set; }
    public string? SecondWordlist { get; set; }
    public string? Mask { get; set; }
    public List<string> Rules { get; set; } = new();
    public bool IncrementMode { get; set; }
    public int IncrementMin { get; set; }
    public int IncrementMax { get; set; }
    public int EstimatedDurationMinutes { get; set; }
}

/// <summary>
/// Represents a wallet in the queue for batch processing.
/// </summary>
public class QueuedWallet : INotifyPropertyChanged
{
    private string _filePath = string.Empty;
    private string _fileName = string.Empty;
    private WalletAnalysis? _analysis;
    private QueueStatus _status;
    private string? _hashFile;
    private string? _crackedPassword;
    private string _errorMessage = string.Empty;

    public string FilePath
    {
        get => _filePath;
        set { _filePath = value; OnPropertyChanged(); }
    }

    public string FileName
    {
        get => _fileName;
        set { _fileName = value; OnPropertyChanged(); }
    }

    public WalletAnalysis? Analysis
    {
        get => _analysis;
        set { _analysis = value; OnPropertyChanged(); }
    }

    public QueueStatus Status
    {
        get => _status;
        set { _status = value; OnPropertyChanged(); }
    }

    public string? HashFile
    {
        get => _hashFile;
        set { _hashFile = value; OnPropertyChanged(); }
    }

    public string? CrackedPassword
    {
        get => _crackedPassword;
        set { _crackedPassword = value; OnPropertyChanged(); }
    }

    public string ErrorMessage
    {
        get => _errorMessage;
        set { _errorMessage = value; OnPropertyChanged(); }
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}

public enum QueueStatus
{
    Pending,
    Analyzing,
    Ready,
    Running,
    Completed,
    Failed,
    Skipped
}

/// <summary>
/// Represents a saved session that can be loaded later.
/// </summary>
public class SavedSession
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public DateTime? LastRunAt { get; set; }
    public string Name { get; set; } = string.Empty;
    public string WalletPath { get; set; } = string.Empty;
    public string HashFile { get; set; } = string.Empty;
    public SmartAttackProfile? Profile { get; set; }
    public int CurrentPhaseIndex { get; set; }
    public SessionState State { get; set; }
    public string? CrackedPassword { get; set; }
}

public enum SessionState
{
    New,
    Running,
    Paused,
    Completed,
    Exhausted,
    Aborted
}
