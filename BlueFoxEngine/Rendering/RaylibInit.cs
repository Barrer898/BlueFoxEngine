using BlueFoxEngine.Logging;
using BlueFoxEngine.Configuration;
using Raylib_cs;
namespace BlueFoxEngine.Rendering;

public static class RaylibInit
{
    public static void Initialize()
    {
        Logger _logger = new("InitializeRaylib");
        _logger.Output(Logger.OutputType.Info, "Setting Flags", Logger.OutputLevel.Verbose);
        //Raylib.SetConfigFlags(); <-- DONT FORGET

        // Initialize window
        _logger.Output(Logger.OutputType.Info, "Initialize window", Logger.OutputLevel.Verbose);
        int width = CurrentEngineConfig._EngineConfig.Window.Width;
        int height = CurrentEngineConfig._EngineConfig.Window.Height;
        string title = CurrentEngineConfig._EngineConfig.Window.Title;
        int targetFPS = CurrentEngineConfig._EngineConfig.Renderer.TargetFps;
        
        // Handle Arguments
        if (Args._arguments.Has("Width") && int.TryParse(Args._arguments.Get("Width"), out width));
        if (Args._arguments.Has("Height") && int.TryParse(Args._arguments.Get("Height"), out height));
        if (Args._arguments.Has("ForceTitle")) title = Args._arguments.Get("ForceTitle") ?? "BlueFox Engine";
        if (Args._arguments.Has("TargetFPS") && int.TryParse(Args._arguments.Get("TargetFPS"), out targetFPS));
        
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