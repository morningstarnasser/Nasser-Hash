using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using HashcatGUI.Models;

namespace HashcatGUI.Services;

/// <summary>
/// Service for saving and restoring Smart Attack sessions.
/// </summary>
public static class SessionService
{
    private static readonly string SessionsFolder = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "NasserHash",
        "Sessions");

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNameCaseInsensitive = true
    };

    static SessionService()
    {
        Directory.CreateDirectory(SessionsFolder);
    }

    /// <summary>
    /// Saves a session to disk.
    /// </summary>
    public static void SaveSession(SavedSession session)
    {
        var filePath = GetSessionFilePath(session.Id);
        var json = JsonSerializer.Serialize(session, JsonOptions);
        File.WriteAllText(filePath, json);
    }

    /// <summary>
    /// Loads a session from disk.
    /// </summary>
    public static SavedSession? LoadSession(string sessionId)
    {
        var filePath = GetSessionFilePath(sessionId);
        if (!File.Exists(filePath))
            return null;

        var json = File.ReadAllText(filePath);
        return JsonSerializer.Deserialize<SavedSession>(json, JsonOptions);
    }

    /// <summary>
    /// Gets all saved sessions.
    /// </summary>
    public static List<SavedSession> GetAllSessions()
    {
        var sessions = new List<SavedSession>();

        if (!Directory.Exists(SessionsFolder))
            return sessions;

        foreach (var file in Directory.GetFiles(SessionsFolder, "*.json"))
        {
            try
            {
                var json = File.ReadAllText(file);
                var session = JsonSerializer.Deserialize<SavedSession>(json, JsonOptions);
                if (session != null)
                    sessions.Add(session);
            }
            catch
            {
                // Skip invalid session files
            }
        }

        sessions.Sort((a, b) => b.CreatedAt.CompareTo(a.CreatedAt));
        return sessions;
    }

    /// <summary>
    /// Deletes a session.
    /// </summary>
    public static void DeleteSession(string sessionId)
    {
        var filePath = GetSessionFilePath(sessionId);
        if (File.Exists(filePath))
            File.Delete(filePath);
    }

    /// <summary>
    /// Creates a new session from a wallet analysis.
    /// </summary>
    public static SavedSession CreateSession(string walletPath, WalletAnalysis analysis, SmartAttackProfile profile, string hashFile)
    {
        return new SavedSession
        {
            Id = Guid.NewGuid().ToString(),
            CreatedAt = DateTime.Now,
            Name = $"{Path.GetFileName(walletPath)} - {profile.Name}",
            WalletPath = walletPath,
            HashFile = hashFile,
            Profile = profile,
            CurrentPhaseIndex = 0,
            State = SessionState.New
        };
    }

    /// <summary>
    /// Updates session state.
    /// </summary>
    public static void UpdateSessionState(SavedSession session, SessionState newState, int? currentPhase = null, string? crackedPassword = null)
    {
        session.State = newState;
        session.LastRunAt = DateTime.Now;

        if (currentPhase.HasValue)
            session.CurrentPhaseIndex = currentPhase.Value;

        if (crackedPassword != null)
            session.CrackedPassword = crackedPassword;

        SaveSession(session);
    }

    private static string GetSessionFilePath(string sessionId)
    {
        return Path.Combine(SessionsFolder, $"{sessionId}.json");
    }
}
