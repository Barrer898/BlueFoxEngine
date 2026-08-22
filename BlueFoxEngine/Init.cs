using BlueFoxEngine.Logging;
using BlueFoxEngine.Configuration;
using BlueFoxEngine.Helper;
using BlueFoxEngine.Scenes;

namespace BlueFoxEngine;

static class Init
{
    internal static EngineCore _EngineCore;
    private static Logger _logger = new Logger("EngineInit");
    static void Main(string[] args) // Bootstrap/Entery point YAY
    {
        _logger.Output(Logger.OutputType.Notice, Logger.OutputLevel.Info, $"\n=====================\n" +
                                                                          $"BlueFoxEngine {EngineInfo.EngineVersionString}\n" +
                                                                          $"Built: {EngineInfo.EngineBuildDate}\n" +
                                                                          $"=====================");
        
        _logger.Output(Logger.OutputType.Info, Logger.OutputLevel.Trace, "Reading arguments");
        Args.ParseArgumentsAndInitialize(args);
        
        _logger.Output(Logger.OutputType.Info, Logger.OutputLevel.Info, "Preparing Engine...");
        _EngineCore = BlueFoxEngine.EngineCore.CreateInstance();
        
        if (_EngineCore == null)
        {
            EngineInitializationException e = new EngineInitializationException("EngineCore.CreateInstance() Returned null.");
            _logger.Output(Logger.OutputType.CriticalError, Logger.OutputLevel.Critical, "Failed to Initialize the Core engine.", e);
            throw e;
        }
        
        _logger.Output(Logger.OutputType.Info, Logger.OutputLevel.Debug, "Reading EngineConfig...");
        _EngineCore.LoadEngineConfigurationFromFile();
        
        _logger.Output(Logger.OutputType.Info, Logger.OutputLevel.Debug, "Loaded EngineConfig, Updating Logger...");
        _logger.UpdateOutputLevel();
        
        _logger.Output(Logger.OutputType.Info, Logger.OutputLevel.Info, "Initializing Raylib");
        _EngineCore.InitializeRaylib();
        
        _logger.Output(Logger.OutputType.Info, Logger.OutputLevel.Info, "Running Scene : {TBA}");
        SceneManager.SetCurrentScene(new BlueFoxEngine.Scenes.BuiltIn.DebugScene());
        SceneManager.Run();
        
    }
}

public class Args
{
    public static Arguments _arguments;

    public static void ParseArgumentsAndInitialize(string[] args)
    {
        _arguments = new Arguments(args);
    }

}