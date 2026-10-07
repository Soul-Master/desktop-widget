using System;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace DesktopWidget.UI;

public sealed class AppSettings
{
    private bool readOnly;
    public int RefreshIntervalSeconds { get; set; } = 30;
    public int TintOpacityPercent { get; set; } = -1;
    public int? WindowX { get; set; }
    public int? WindowY { get; set; }
    public WindowMonitorSettings? WindowMonitor { get; set; }

    public static string DefaultPath => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "DesktopWidget", "settings.json");

    public static AppSettings Load(string? path = null, bool readOnly = false)
    {
        path ??= DefaultPath;
        try
        {
            if (!File.Exists(path))
            {
                var defaults = new AppSettings { readOnly = readOnly };
                defaults.Save(path);
                return defaults;
            }
            var settings = JsonSerializer.Deserialize(File.ReadAllText(path), SettingsJsonContext.Default.AppSettings) ?? new();
            settings.readOnly = readOnly;
            if (settings.RefreshIntervalSeconds is not (15 or 30 or 60 or 120 or 300 or 600))
                settings.RefreshIntervalSeconds = 30;
            if (settings.TintOpacityPercent != -1 &&
                (settings.TintOpacityPercent < 0 || settings.TintOpacityPercent > 100 || settings.TintOpacityPercent % 10 != 0))
                settings.TintOpacityPercent = -1;
            if (settings.WindowX.HasValue != settings.WindowY.HasValue)
                settings.WindowX = settings.WindowY = null;
            if (!settings.WindowX.HasValue || settings.WindowMonitor?.IsValid != true)
                settings.WindowMonitor = null;
            return settings;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or JsonException)
        {
            return new() { readOnly = readOnly };
        }
    }

    public void Save(string? path = null)
    {
        if (readOnly) return;
        path = Path.GetFullPath(path ?? DefaultPath);
        var temporaryPath = path + "." + Guid.NewGuid().ToString("N") + ".tmp";
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            File.WriteAllText(temporaryPath, JsonSerializer.Serialize(this, SettingsJsonContext.Default.AppSettings));
            File.Move(temporaryPath, path, overwrite: true);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
        }
        finally
        {
            try { File.Delete(temporaryPath); }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException) { }
        }
    }
}

// Screen coordinates are physical pixels. Restore them only in the same monitor environment.
public sealed record WindowMonitorSettings(string DeviceId, int X, int Y, int Width, int Height,
    int WorkX, int WorkY, int WorkWidth, int WorkHeight, uint Dpi)
{
    internal bool IsValid => !string.IsNullOrWhiteSpace(DeviceId) && Width > 0 && Height > 0 &&
        WorkWidth > 0 && WorkHeight > 0 && Dpi > 0;

    internal bool MatchesGeometry(WindowMonitorSettings current) =>
        this with { Dpi = current.Dpi } == current;

    internal bool CanRestore(WindowMonitorSettings? current) => IsValid && this == current;
}

[JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase, WriteIndented = true)]
[JsonSerializable(typeof(AppSettings))]
internal partial class SettingsJsonContext : JsonSerializerContext { }
