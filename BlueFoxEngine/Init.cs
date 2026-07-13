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
        _logger.Output(Logger.OutputType.Notice, $"\n=====================\n" +
                                                 $"BlueFoxEngine {EngineInfo.EngineVersionString}\n" +
                                                 $"Built: {EngineInfo.EngineBuildDate}\n" +
                                                 $"=====================", Logger.OutputLevel.Info);
        
        _logger.Output(Logger.OutputType.Info, "Reading arguments", Logger.OutputLevel.Trace);
        Args.ParseArgumentsAndInitialize(args);
        
        _logger.Output(Logger.OutputType.Info, "Preparing Engine...", Logger.OutputLevel.Info);
        _EngineCore = BlueFoxEngine.EngineCore.CreateInstance();
        
        _logger.Output(Logger.OutputType.Info, "Reading EngineConfig...", Logger.OutputLevel.Debug);
        _EngineCore.LoadEngineConfigurationFromFile();
        
        _logger.Output(Logger.OutputType.Info, "Loaded EngineConfig, Updating Logger...", Logger.OutputLevel.Debug);
        _logger.UpdateOutputLevel();
        
        _logger.Output(Logger.OutputType.Info, "Initializing Raylib", Logger.OutputLevel.Info);
        _EngineCore.InitializeRaylib();
        
        _logger.Output(Logger.OutputType.Info, "Running Scene : {TBA}", Logger.OutputLevel.Info);
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