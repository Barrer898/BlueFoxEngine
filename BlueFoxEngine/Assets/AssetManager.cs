using BlueFoxEngine.Logging;
using BlueFoxEngine.Configuration;
using Raylib_cs;
using System.IO;

namespace BlueFoxEngine.Resources;


public static class ResourceLoader
{
    private static readonly string BaseDirectory = AppContext.BaseDirectory;

    internal const uint BadAssetNukeMargin = 10;
    internal static uint CurrentBadAssetCount = 0;

    internal const uint MaxSoundCacheAmount = 250;
    private static Dictionary<string, Sound> SoundCache = new Dictionary<string, Sound>();
    private static readonly Sound InvalidSound = new Sound();

    private static Logger _logger = new Logger("AssetManager");

    public static Sound LoadSoundResource(string AudioRelativePath)
    {
        Sound RequestedSound;
        if (TryLoadSound(AudioRelativePath, out RequestedSound))
            return RequestedSound;
        else
        {
            return InvalidSound; // Invalid!
        }
    }

    public static bool UnloadSoundResource(string AudioRelativePath)
    {
        Sound SoundToUnload = FindSoundInCache(AudioRelativePath);
        if(Raylib.IsSoundValid(SoundToUnload))
        {
            Raylib.UnloadSound(SoundToUnload);
            SoundCache.Remove(AudioRelativePath); // We know that AudioRelativePath is valid as FindSoundInCache() didn't return null
            return true;
        }
        else
            return false;
        
    }

    public static bool TryLoadSound(string AudioRelativePath, out Sound sound)
    {
        try
        {
            AudioRelativePath = AudioRelativePath.TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);

            Sound RequestedSound = FindSoundInCache(AudioRelativePath);

            if(Raylib.IsSoundValid(RequestedSound))
            {
                _logger.Output(Logger.OutputType.Info, "Found Cache!", Logger.OutputLevel.Debug);
                sound = RequestedSound;
                return true;  
            }
            string FullLoadPath = System.IO.Path.Combine([BaseDirectory,
                            CurrentEngineConfig._EngineConfig.Assets.Directory,
                            "Audio",
                            AudioRelativePath]
                        );

            RequestedSound = Raylib.LoadSound(FullLoadPath);

            if(Raylib.IsSoundValid((Sound)RequestedSound))
            {
                AddSoundToCache(AudioRelativePath, (Sound)RequestedSound);
                sound = RequestedSound;
                return true;
            }
            else
            {
                sound = InvalidSound; // Invalid!
                return false;
            }

            
        } catch(Exception e)
        {
            _logger.Output(Logger.OutputType.ExceptionThrownError, "Uh oh! Failed to load sound...", e, Logger.OutputLevel.Error);
            sound = InvalidSound; // Invalid!
            return false;
        }
    }

    internal static void AddSoundToCache(string AudioRelativePath, Sound _Sound)
    {
        if(Raylib.IsSoundValid(_Sound) && SoundCache.Count < MaxSoundCacheAmount)
            SoundCache.Add(AudioRelativePath, _Sound);
        
        _logger.Output(Logger.OutputType.Info, $"Current SoundCache.Count: {SoundCache.Count}", Logger.OutputLevel.Info);
            
    }

    internal static Sound FindSoundInCache(string AudioRelativePath)
    {
        Sound RequestedSound;
        if (SoundCache.TryGetValue(AudioRelativePath, out RequestedSound))
        {
            if(Raylib.IsSoundValid(RequestedSound))
                return RequestedSound;
            else
            {
                _logger.Output(Logger.OutputType.Error, $"Cached sound is not valid?!: {AudioRelativePath}", Logger.OutputLevel.Error);

                Raylib.UnloadSound(RequestedSound);
                SoundCache.Remove(AudioRelativePath);

                CurrentBadAssetCount++;
                if (BadAssetNukeMargin <= CurrentBadAssetCount)
                { 
                    ClearSoundCache();
                    _logger.Output(Logger.OutputType.Notice, "Cleared sound cache.", Logger.OutputLevel.Info);
                    CurrentBadAssetCount = 0;
                }
                  

                return InvalidSound; // Invalid!
            }
        }
        else 
            return InvalidSound; // Invalid!
    }

    public static void ClearSoundCache()
    {
        foreach(var sound in SoundCache.Values)
        {
            Raylib.UnloadSound(sound);
        }

        SoundCache.Clear();
    }
}
