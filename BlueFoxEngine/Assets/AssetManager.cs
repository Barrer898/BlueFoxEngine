using BlueFoxEngine.Logging;
using BlueFoxEngine.Configuration;
using Raylib_cs;
using System.IO;

namespace BlueFoxEngine.Assets;


public static class AssetLoader
{
    private static readonly string BaseDirectory = AppContext.BaseDirectory;

    internal const uint BadAssetNukeMargin = 10;
    internal static uint CurrentBadAssetCount = 0;

    internal const uint MaxSoundCacheAmount = 250;
    private static readonly Sound InvalidSound = new Sound();
    public class CachedSound
    {
        public Sound Sound;
        public int ReferenceCount { get; private set; }
        public bool IsValid => Raylib.IsSoundValid(this.Sound);

        public void increaseReferenceCount()
        {
            if(this.IsValid)
                this.ReferenceCount++;
        }
        public void decreaseReferenceCount()
        {
            if(this.IsValid)
            this.ReferenceCount--;
        }
        
        public CachedSound(Sound sound, int referenceCount)
        {
            this.Sound = sound;
            this.ReferenceCount = referenceCount;
        }
    }
    private static readonly CachedSound InvalidCachedSound = new CachedSound(InvalidSound, 0);
    private static Dictionary<string, CachedSound> SoundCache = new Dictionary<string, CachedSound>();
    
    

    private static Logger _logger = new Logger("AssetManager");

    public static CachedSound LoadSoundResource(string AudioRelativePath)
    {
        CachedSound RequestedSound;
        if (TryLoadSound(AudioRelativePath, out RequestedSound))
            return RequestedSound;
        else
        {
            return InvalidCachedSound; // Invalid!
        }
    }

    public static bool UnloadSoundResource(string AudioRelativePath)
    {
        CachedSound SoundToUnload = FindSoundInCache(AudioRelativePath);
        if(SoundToUnload.IsValid)
        {
            SoundToUnload.decreaseReferenceCount();
            if(SoundToUnload.ReferenceCount == 0)
            {
                Raylib.UnloadSound(SoundToUnload.Sound);
                SoundCache.Remove(AudioRelativePath); // We know that AudioRelativePath is valid as FindSoundInCache() didn't return null
            }
            return true;
        }
        else
            return false;
        
    }

    public static bool TryLoadSound(string AudioRelativePath, out CachedSound sound)
    {
        try
        {
            AudioRelativePath = AudioRelativePath.TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);

            CachedSound RequestedSound = FindSoundInCache(AudioRelativePath);

            if(RequestedSound.IsValid)
            {
                _logger.Output(Logger.OutputType.Info, "Found Cache!", Logger.OutputLevel.Debug);
                sound = RequestedSound;
                RequestedSound.increaseReferenceCount();
                return true;  
            }
            string FullLoadPath = System.IO.Path.Combine([BaseDirectory,
                            CurrentEngineConfig._EngineConfig.Assets.Directory,
                            "Audio",
                            AudioRelativePath]
                        );

            Sound newRequestedSound = Raylib.LoadSound(FullLoadPath);

            if(Raylib.IsSoundValid(newRequestedSound))
            {
                AddSoundToCache(AudioRelativePath, newRequestedSound);
                sound = SoundCache[AudioRelativePath];
                return true;
            }
            else
            {
                sound = InvalidCachedSound; // Invalid!
                return false;
            }

            
        } catch(Exception e)
        {
            _logger.Output(Logger.OutputType.ExceptionThrownError, "Uh oh! Failed to load sound...", e, Logger.OutputLevel.Error);
            sound = InvalidCachedSound; // Invalid!
            return false;
        }
    }

    internal static void AddSoundToCache(string AudioRelativePath, Sound _Sound)
    {
        if(Raylib.IsSoundValid(_Sound) && SoundCache.Count < MaxSoundCacheAmount)
            SoundCache.Add(AudioRelativePath, new CachedSound(_Sound, 1));
        
        _logger.Output(Logger.OutputType.Info, $"Current SoundCache.Count: {SoundCache.Count}", Logger.OutputLevel.Info);
            
    }

    internal static CachedSound FindSoundInCache(string audioRelativePath)
    {
        CachedSound requestedSound;
        if (SoundCache.TryGetValue(audioRelativePath, out requestedSound))
        {
            if(requestedSound.IsValid)
                return requestedSound;
            else
            {
                _logger.Output(Logger.OutputType.Error, $"Cached sound is not valid?!: {audioRelativePath}", Logger.OutputLevel.Error);

                Raylib.UnloadSound(requestedSound.Sound);
                SoundCache.Remove(audioRelativePath);

                HandleInvalidCacheSounds();

                return InvalidCachedSound; // Invalid!
            }
        }
        else 
            return InvalidCachedSound; // Invalid!
    }

    internal static void HandleInvalidCacheSounds()
    {
        CurrentBadAssetCount++;
        if (BadAssetNukeMargin <= CurrentBadAssetCount)
        { 
            ClearSoundCache();
            _logger.Output(Logger.OutputType.Warning, "Cleared sound cache due to invalid sounds", Logger.OutputLevel.Warning);
            CurrentBadAssetCount = 0;
        }
    }
    
    public static void ClearSoundCache()
    {
        foreach(var sound in SoundCache.Values)
        {
            Raylib.UnloadSound(sound.Sound);
        }

        SoundCache.Clear();
    }
}
