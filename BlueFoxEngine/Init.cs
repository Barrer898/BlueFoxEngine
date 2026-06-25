using BlueFoxEngine.Logging;
using BlueFoxEngine.Configuration;

namespace BlueFoxEngine;

static class Init
{
    private static Logger _logger = new Logger("EngineInit");
    static void Main(string[] args) // Bootstrap/Entery point YAY
    {
        _logger.Output(Logger.OutputType.Notice, $"\n=====================\n" +
                                                 $"BlueFoxEngine {EngineInfo.EngineVersionString}\n" +
                                                 $"Built: {EngineInfo.EngineBuildDate}\n" +
                                                 $"=====================", Logger.OutputLevel.Info);
        _logger.Output(Logger.OutputType.Info, "Reading EngineConfig...", Logger.OutputLevel.Debug);
        try
        {
            string engineConfigPath = Path.Combine(AppContext.BaseDirectory, "Configuration", "EngineConfig.json");
            CurrentEngineConfig._EngineConfig = ConfigReader.LoadFirst(engineConfigPath);
        }
        catch (Exception e)
        {
            _logger.Output(Logger.OutputType.CriticalError, "Failed to load EngineConfig.", e, Logger.OutputLevel.Critical);
            Environment.Exit(0);
        }
        _logger.Output(Logger.OutputType.Info, "Loaded EngineConfig, Updating Logger...", Logger.OutputLevel.Debug);
        _logger.UpdateOutputLevel();
        _logger.Output(Logger.OutputType.Info, "Done.", Logger.OutputLevel.Debug);
        _logger.Output(Logger.OutputType.Info, "Loading Raylib...", Logger.OutputLevel.Info);
    }
}