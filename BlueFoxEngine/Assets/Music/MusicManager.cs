
using BlueFoxEngine.Assets;
using BlueFoxEngine.Configuration;
using BlueFoxEngine.Logging;
using Raylib_cs;
namespace BlueFoxEngine.Assets;


public class MusicPlayer
{
    public MusicLayer[] Layers { get; protected set; } =
        new MusicLayer[CurrentEngineConfig._EngineConfig.Audio.MusicLayerCount];
    
    

    public static void PlayMusic(Music muisc, float volume, uint loopCount)
    {
        // TODO
    }
    public static void StopMusic()
    {
        // TODO
    }
    public static void StopAllMusic()
    {
        // TODO
    }
}

