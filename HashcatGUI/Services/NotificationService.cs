using System;
using System.IO;
using System.Media;
using System.Windows;
using System.Windows.Threading;

namespace HashcatGUI.Services;

/// <summary>
/// Handles notifications when passwords are cracked.
/// </summary>
public static class NotificationService
{
    private static bool _soundEnabled = true;
    private static bool _desktopNotificationEnabled = true;
    private static string _customSoundPath = string.Empty;

    public static bool SoundEnabled
    {
        get => _soundEnabled;
        set => _soundEnabled = value;
    }

    public static bool DesktopNotificationEnabled
    {
        get => _desktopNotificationEnabled;
        set => _desktopNotificationEnabled = value;
    }

    public static string CustomSoundPath
    {
        get => _customSoundPath;
        set => _customSoundPath = value;
    }

    /// <summary>
    /// Plays a success sound and shows notification when a password is cracked.
    /// </summary>
    public static void NotifyPasswordCracked(string password, string hash)
    {
        if (_soundEnabled)
        {
            PlaySuccessSound();
        }

        if (_desktopNotificationEnabled)
        {
            ShowDesktopNotification("Password Cracked!", $"Found: {MaskPassword(password)}");
        }
    }

    /// <summary>
    /// Plays a notification sound when attack completes.
    /// </summary>
    public static void NotifyAttackComplete(bool success, int crackedCount)
    {
        if (_soundEnabled)
        {
            if (success && crackedCount > 0)
            {
                PlaySuccessSound();
            }
            else
            {
                PlayCompletionSound();
            }
        }

        if (_desktopNotificationEnabled)
        {
            var message = success && crackedCount > 0
                ? $"Attack finished! {crackedCount} password(s) cracked!"
                : "Attack completed. No passwords found.";
            ShowDesktopNotification("Nasser-Hash", message);
        }
    }

    /// <summary>
    /// Plays a success/victory sound.
    /// </summary>
    public static void PlaySuccessSound()
    {
        try
        {
            if (!string.IsNullOrEmpty(_customSoundPath) && File.Exists(_customSoundPath))
            {
                using var player = new SoundPlayer(_customSoundPath);
                player.Play();
            }
            else
            {
                // Play Windows success sound
                SystemSounds.Exclamation.Play();

                // Play a short celebratory sequence
                Application.Current?.Dispatcher?.BeginInvoke(async () =>
                {
                    await System.Threading.Tasks.Task.Delay(300);
                    SystemSounds.Asterisk.Play();
                });
            }
        }
        catch
        {
            // Ignore sound errors
        }
    }

    /// <summary>
    /// Plays a completion sound (not necessarily success).
    /// </summary>
    public static void PlayCompletionSound()
    {
        try
        {
            SystemSounds.Beep.Play();
        }
        catch
        {
            // Ignore sound errors
        }
    }

    /// <summary>
    /// Shows a Windows toast notification.
    /// </summary>
    public static void ShowDesktopNotification(string title, string message)
    {
        try
        {
            Application.Current?.Dispatcher?.Invoke(() =>
            {
                // Bring window to front if minimized
                var mainWindow = Application.Current.MainWindow;
                if (mainWindow != null)
                {
                    if (mainWindow.WindowState == WindowState.Minimized)
                    {
                        mainWindow.WindowState = WindowState.Normal;
                    }
                    mainWindow.Activate();
                    mainWindow.Topmost = true;
                    mainWindow.Topmost = false;
                    mainWindow.Focus();
                }

                // Show a simple message box notification
                // In a production app, you'd use Windows Toast Notifications
                MessageBox.Show(
                    message,
                    title,
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
            });
        }
        catch
        {
            // Ignore notification errors
        }
    }

    /// <summary>
    /// Masks a password for display (shows first 2 and last 2 characters).
    /// </summary>
    private static string MaskPassword(string password)
    {
        if (string.IsNullOrEmpty(password))
            return "***";
        if (password.Length <= 4)
            return new string('*', password.Length);
        return $"{password[..2]}{"***"}{password[^2..]}";
    }
}
