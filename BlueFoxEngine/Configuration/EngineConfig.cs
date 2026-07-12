using System.Text.Json.Serialization;

namespace BlueFoxEngine.Configuration;

public static class CurrentEngineConfig
{
    public static EngineConfig _EngineConfig { get; set; }
}

public class WindowConfig
{
    [JsonPropertyName("title")] public string Title { get; set; } = string.Empty;

    [JsonPropertyName("width")] public int Width { get; set; } = 1280;

    [JsonPropertyName("height")] public int Height { get; set; } = 720;

    [JsonPropertyName("fullscreen")] public bool Fullscreen { get; set; } = false;

    [JsonPropertyName("vsync")] public bool VSync { get; set; } = true;
}

public class RendererConfig
{
    [JsonPropertyName("targetFps")] public int TargetFps { get; set; } = 60;

    [JsonPropertyName("pixelPerfect")] public bool PixelPerfect { get; set; } = false;

    [JsonPropertyName("msaa")] public bool Msaa { get; set; } = true;
}

public class AudioConfig
{
    [JsonPropertyName("masterVolume")] public float MasterVolume { get; set; } = 100f;

    [JsonPropertyName("musicVolume")] public float MusicVolume { get; set; } = 100f;

    [JsonPropertyName("sfxVolume")] public float SfxVolume { get; set; } = 100f;
}

public class LoggingConfig
{
    [JsonPropertyName("console")] public bool Console { get; set; }

    [JsonPropertyName("file")] public bool File { get; set; }

    [JsonPropertyName("level")] public string Level { get; set; } = "Info";
}

public class AssetsConfig
{
    [JsonPropertyName("directory")] public string Directory { get; set; } = string.Empty;

    [JsonPropertyName("autoImport")] public bool AutoImport { get; set; }
}

public class UiConfig
{
    [JsonPropertyName("defaultTheme")] public string DefaultTheme { get; set; } = string.Empty;

    [JsonPropertyName("defaultFont")] public string DefaultFont { get; set; } = string.Empty;

    [JsonPropertyName("enableAnimations")] public bool EnableAnimations { get; set; }
}

public class EngineConfig
{
    [JsonPropertyName("window")] public WindowConfig Window { get; set; } = new();

    [JsonPropertyName("renderer")] public RendererConfig Renderer { get; set; } = new();

    [JsonPropertyName("audio")] public AudioConfig Audio { get; set; } = new();

    [JsonPropertyName("logging")] public LoggingConfig Logging { get; set; } = new();

    [JsonPropertyName("assets")] public AssetsConfig Assets { get; set; } = new();
}