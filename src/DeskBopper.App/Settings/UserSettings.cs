using System;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace DeskBopper.App.Settings;

/// <summary>
/// Persisted user preferences, stored as JSON at
/// <c>%APPDATA%\DeskBopper\settings.json</c>. Loading never throws — a missing or
/// corrupt file yields defaults so the app always starts.
/// </summary>
public sealed class UserSettings
{
    private static readonly string Dir =
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "DeskBopper");

    private static readonly string FilePath = Path.Combine(Dir, "settings.json");

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    /// <summary>Saved window position. Null until the user first moves the character.</summary>
    public double? Left { get; set; }
    public double? Top { get; set; }

    /// <summary>Bob responsiveness multiplier (matches MotionMapper.Sensitivity range).</summary>
    public double Sensitivity { get; set; } = 1.0;

    /// <summary>Whether DeskBopper should launch on Windows sign-in.</summary>
    public bool Autostart { get; set; }

    /// <summary>Base character colour as a hex string (#RRGGBB).</summary>
    public string ColorHex { get; set; } = "#4C6EF5";

    public static UserSettings Load()
    {
        try
        {
            if (File.Exists(FilePath))
            {
                string json = File.ReadAllText(FilePath);
                UserSettings? loaded = JsonSerializer.Deserialize<UserSettings>(json, JsonOptions);
                if (loaded is not null) return loaded;
            }
        }
        catch
        {
            // Corrupt/unreadable settings -> fall back to defaults.
        }
        return new UserSettings();
    }

    public void Save()
    {
        try
        {
            Directory.CreateDirectory(Dir);
            File.WriteAllText(FilePath, JsonSerializer.Serialize(this, JsonOptions));
        }
        catch
        {
            // Persistence is best-effort; never crash the app over a failed save.
        }
    }
}
