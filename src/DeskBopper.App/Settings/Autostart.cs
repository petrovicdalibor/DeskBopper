using System;
using System.Diagnostics;
using Microsoft.Win32;

namespace DeskBopper.App.Settings;

/// <summary>
/// Start-with-Windows support via the per-user Run key
/// (<c>HKCU\Software\Microsoft\Windows\CurrentVersion\Run</c>). Per-user means no admin
/// rights are required. All methods are best-effort and swallow registry errors.
/// </summary>
public static class Autostart
{
    private const string RunKey = @"Software\Microsoft\Windows\CurrentVersion\Run";
    private const string ValueName = "DeskBopper";

    /// <summary>Path to the running executable, quoted for the registry command line.</summary>
    private static string ExecutablePath => $"\"{Environment.ProcessPath ?? Process.GetCurrentProcess().MainModule?.FileName}\"";

    public static bool IsEnabled()
    {
        try
        {
            using RegistryKey? key = Registry.CurrentUser.OpenSubKey(RunKey, writable: false);
            return key?.GetValue(ValueName) is string;
        }
        catch
        {
            return false;
        }
    }

    public static void Set(bool enabled)
    {
        try
        {
            using RegistryKey key = Registry.CurrentUser.CreateSubKey(RunKey);
            if (enabled)
                key.SetValue(ValueName, ExecutablePath);
            else if (key.GetValue(ValueName) is not null)
                key.DeleteValue(ValueName, throwOnMissingValue: false);
        }
        catch
        {
            // Ignore registry failures; the checkbox simply won't take effect.
        }
    }
}
