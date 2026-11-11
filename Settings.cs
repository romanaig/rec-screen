using System.IO;
using System.Text.Json;

namespace RecScreen;

/// <summary>User choices, saved as JSON so every recording reuses the last ones.</summary>
public sealed class Settings
{
    static readonly string FilePath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "RecScreen", "settings.json");

    static readonly string DefaultOutputFolder = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.MyVideos), "Rec Screen");

    public string OutputFolder { get; set; } = DefaultOutputFolder;

    /// <summary>Display device name (e.g. \\.\DISPLAY2); null records the main monitor.</summary>
    public string? Monitor { get; set; }

    public bool RecordSystemAudio { get; set; } = true;

    public bool RecordMicrophone { get; set; }

    /// <summary>Capture device id; null uses the Windows default microphone.</summary>
    public string? Microphone { get; set; }

    public static Settings Load()
    {
        try
        {
            var settings = JsonSerializer.Deserialize<Settings>(File.ReadAllText(FilePath)) ?? new();
            if (string.IsNullOrWhiteSpace(settings.OutputFolder))
                settings.OutputFolder = DefaultOutputFolder; // a hand-edited file may have blanked it
            return settings;
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException or JsonException)
        {
            return new(); // first run or unreadable file: fall back to defaults
        }
    }

    public void Save()
    {
        Directory.CreateDirectory(Path.GetDirectoryName(FilePath)!);
        File.WriteAllText(FilePath, JsonSerializer.Serialize(this, new JsonSerializerOptions { WriteIndented = true }));
    }
}
