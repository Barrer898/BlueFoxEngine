using BlueFoxEngine.Configuration;
using BlueFoxEngine.Logging;
using Raylib_cs;

namespace BlueFoxEngine;

public class EngineCore
{
    private static Logger _logger = new Logger("EngineCore");
    private static bool _alreadyCreated = false;
    public static bool AlreadyInitialized { get; private set; } = false;

    /// <summary>
    /// This class is only meant to be used internally, I advise you not to touch this unless you know what
    /// you are doing. -B
    /// </summary>
    /// <returns>Nothing, leave.</returns>
    public static EngineCore CreateInstance()
    {
        if (!_alreadyCreated)
        {
            _alreadyCreated = true;
            return new EngineCore();
        }
        else
        {
            InvalidOperationException e = new InvalidOperationException("Attempted to create a new instance of the EngineCore class, but an instance already exists. Only one instance of this class can be created at a time.");
            _logger.Output(Logger.OutputType.ExceptionThrownWarning, "Attempted to GetInstance whilst one already exists, big no no!", e, Logger.OutputLevel.Warning);
            return null;
        }
    }

    private EngineCore()
    {
        // haha no.
    }
    internal void LoadEngineConfigurationFromFile()
    {
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
    }
    internal void InitializeRaylib()
    {
        _logger.Output(Logger.OutputType.Info, "Setting Flags", Logger.OutputLevel.Verbose);
        //Raylib.SetConfigFlags(); <-- DONT FORGET

        // Initialize window
        _logger.Output(Logger.OutputType.Info, "Initialize window", Logger.OutputLevel.Verbose);
        int width = CurrentEngineConfig._EngineConfig.Window.Width;
        int height = CurrentEngineConfig._EngineConfig.Window.Height;
        string title = CurrentEngineConfig._EngineConfig.Window.Title;
        int targetFPS = CurrentEngineConfig._EngineConfig.Renderer.TargetFps;
        
        // Handle Arguments
        if (Args._arguments.Has("Width")) int.TryParse(Args._arguments.Get("Width"), out width);
        if (Args._arguments.Has("Height")) int.TryParse(Args._arguments.Get("Height"), out height);
        if (Args._arguments.Has("ForceTitle")) title = Args._arguments.Get("ForceTitle") ?? "BlueFox Engine";
        if (Args._arguments.Has("TargetFPS")) int.TryParse(Args._arguments.Get("TargetFPS"), out targetFPS);
        
        Raylib.InitWindow(width, height, title);
        Raylib.SetTargetFPS(targetFPS);

        // Initialize audio device
        _logger.Output(Logger.OutputType.Info, "Initialize Audio", Logger.OutputLevel.Verbose);
        Raylib.InitAudioDevice();
    }

    public static void Close()
    {
        // Unload all resources and close the window
        Raylib.CloseAudioDevice();  // Close the audio device first
        Raylib.CloseWindow();
    }
}