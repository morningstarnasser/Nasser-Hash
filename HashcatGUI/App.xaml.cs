using System;
using System.Windows;
using System.Windows.Threading;
using HashcatGUI.Services;

namespace HashcatGUI;

public partial class App : Application
{
    public static SettingsService Settings { get; } = new();
    public static HashcatService Hashcat { get; } = new();

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        // Global exception handling
        DispatcherUnhandledException += OnDispatcherUnhandledException;
        AppDomain.CurrentDomain.UnhandledException += OnUnhandledException;

        // Load settings
        Settings.Load();
    }

    private static bool _isShowingError = false;

    private void OnDispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
    {
        e.Handled = true;

        // Prevent recursive error dialogs
        if (_isShowingError)
            return;

        try
        {
            _isShowingError = true;
            // Log to console instead of MessageBox to prevent stack overflow
            System.Diagnostics.Debug.WriteLine($"[ERROR] {e.Exception.Message}\n{e.Exception.StackTrace}");
            Console.Error.WriteLine($"[ERROR] {e.Exception.Message}");
        }
        finally
        {
            _isShowingError = false;
        }
    }

    private void OnUnhandledException(object sender, UnhandledExceptionEventArgs e)
    {
        if (e.ExceptionObject is Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[FATAL] {ex.Message}\n{ex.StackTrace}");
            Console.Error.WriteLine($"[FATAL] {ex.Message}");
        }
    }

    protected override void OnExit(ExitEventArgs e)
    {
        Settings.Save();
        Hashcat.Dispose();
        base.OnExit(e);
    }
}
