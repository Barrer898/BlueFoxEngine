using System.Text.Json;
using System.Text.Json.Serialization;

namespace BlueFoxEngine.Configuration;

public static class ConfigReader
{
    private static readonly JsonSerializerOptions Options = new()
    {
        PropertyNameCaseInsensitive = true,
        WriteIndented = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.Never
    };

    private static List<EngineConfig> LoadFromFile(string filePath)
    {
        if (!File.Exists(filePath))
            throw new FileNotFoundException($"Config file not found: {filePath}");
        var json = File.ReadAllText(filePath);
        return LoadFromJson(json);
    }

    private static List<EngineConfig> LoadFromJson(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
            throw new ArgumentException("JSON string is empty or null.", nameof(json));
        var configs = JsonSerializer.Deserialize<List<EngineConfig>>(json, Options);
        return configs ?? throw new InvalidDataException("Failed to deserialize config: result was null.");
    }

    public static EngineConfig LoadFirst(string filePath)
    {
        var configs = LoadFromFile(filePath);
        if (configs.Count == 0)
            throw new InvalidDataException("Config file contains no entries.");
        return configs[0];
    }

    private static void SaveToFile(string filePath, List<EngineConfig> configs)
    {
        var json = JsonSerializer.Serialize(configs, Options);
        File.WriteAllText(filePath, json);
    }

    public static void SaveToFile(string filePath, EngineConfig config)
    {
        SaveToFile(filePath, new List<EngineConfig> { config });
    }
}