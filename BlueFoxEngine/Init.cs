using BlueFoxEngine.Logging;
using BlueFoxEngine.Configuration;
using BlueFoxEngine.Helper;
using BlueFoxEngine.Scenes;
using BlueFoxEngine.Rendering;

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
        
        _logger.Output(Logger.OutputType.Info, "Reading arguments", Logger.OutputLevel.Trace);
        Args.ParseArgumentsAndInitialize(args);
        
        _logger.Output(Logger.OutputType.Info, "Reading EngineConfig...", Logger.OutputLevel.Debug);
        try
        {
            string engineConfigPath;
            if (!Args._arguments.Has("EngineConfigPath"))
            {
                engineConfigPath = Path.Combine(AppContext.BaseDirectory, "Configuration", "EngineConfig.json");
            }
            else
            {
                engineConfigPath = Args._arguments.Get("EngineConfigPath");
            }
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
        
        _logger.Output(Logger.OutputType.Info, "Initializing Raylib", Logger.OutputLevel.Info);
        RaylibInit.Initialize();
        
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