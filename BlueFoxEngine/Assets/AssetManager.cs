using BlueFoxEngine.Logging;
using BlueFoxEngine.Configuration;
using BlueFoxEngine.Assets;
using Raylib_cs;
using System.IO;

namespace BlueFoxEngine.Assets;


public static class AssetLoader
{
    private static Logger _logger = new Logger("AssetManager");
    
    private static readonly string BaseDirectory = AppContext.BaseDirectory;

    internal const uint BadAssetNukeMargin = 10;
    internal static uint CurrentBadAssetCount = 0;

    internal const uint MaxSoundCacheAmount = 250;
    private static readonly AssetCache<Sound> SoundCache = new();
    private static readonly AssetCache<Music> _musicCache = new();
    private static readonly AssetCache<Texture2D> _texturesCache = new();
    private static readonly AssetCache<Font> _fontsCache = new();
    private static bool SoundCacheClearingInProgress = false;
    
    public class CachedAsset<T>
    {
        public T Asset { get; }

        public int ReferenceCount { get; private set; }

        public bool IsValid { get; }

        public CachedAsset(T asset, bool valid)
        {
            Asset = asset;
            IsValid = valid;
            ReferenceCount = 1;
        }

        public void IncreaseReferenceCount()
            => ReferenceCount++;

        public void DecreaseReferenceCount()
            => ReferenceCount = Math.Max(0, ReferenceCount - 1);
    }
    
    public class AssetCache<T>()
    {
        private readonly Dictionary<string, CachedAsset<T>> _cache = new();
        internal IReadOnlyDictionary<string, CachedAsset<T>> PublicCache => _cache;

        public bool TryGet(string path, out CachedAsset<T> asset)
        {
            return _cache.TryGetValue(path, out asset!);
        }
        
        public void Add(string path, CachedAsset<T> asset)
        {
            _cache[path] = asset;
        }

        public bool Remove(string path)
        {
            return _cache.Remove(path);
        }

        public int Count => _cache.Count;

        public CachedAsset<T> GetViaIndex(int index)
        {
            return _cache.Values.ElementAt(index);
        }
        
        public void Clear()
        {
            _cache.Clear();
        }
    }
    
    public static CachedAsset<T> Load<T>(
        AssetCache<T> cache,
        string path,
        Func<string, T> loader,
        Func<T, bool> validator)
    {
        if (cache.TryGet(path, out var cached))
        {
            cached.IncreaseReferenceCount();
            return cached;
        }
        
        if (cache.PublicCache.ContainsKey(path))
            throw new InvalidOperationException(
                $"Asset '{path}' already exists in cache.");
        
        
        T asset = loader(path);

        if (!validator(asset))
            return new CachedAsset<T>(asset, false);

        cached = new CachedAsset<T>(asset, true);
        

        return cached;
    }

    public static CachedAsset<Sound> LoadSound(string path)
    {
        return Load(
            SoundCache,
            path,
            Raylib.LoadSound,
            sound => Raylib.IsSoundValid(sound));
    }
    
    public static Sound? LoadSoundResource(string audioRelativePath)
    {
        if (TryLoadSound(audioRelativePath, out var requestedSound))
        {
            return requestedSound.Asset;   
        }
        else
        {
            _logger.Output(Logger.OutputType.Warning, "Failed to load requested sound", Logger.OutputLevel.Warning);
            return null; // Invalid!
        }
    }

    public static bool UnloadSoundResource(string audioRelativePath)
    {
        Sound soundToUnload = FindSoundInCache(audioRelativePath);
        if(Raylib.IsSoundValid(soundToUnload) && SoundCache.TryGet(audioRelativePath, out var cachedSoundAsset))
        {
            cachedSoundAsset.DecreaseReferenceCount();
            if(cachedSoundAsset.ReferenceCount == 0)
            {
                Raylib.UnloadSound(soundToUnload);
                SoundCache.Remove(audioRelativePath); // We know that AudioRelativePath is valid as FindSoundInCache() didn't return null
            }
            return true;
        }
        else
        {
            _logger.Output(Logger.OutputType.ExceptionThrownWarning, "Failed to unload sound resource",new KeyNotFoundException($"The sound asset with relative path '{audioRelativePath}' was not found in the cache."),Logger.OutputLevel.Warning); 
            return false;
        }
    }

    public static bool TryLoadSound(string audioRelativePath, out CachedAsset<Sound>? sound)
    {
        try
        {
            audioRelativePath = audioRelativePath.TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);

            if(SoundCache.TryGet(audioRelativePath, out var cachedSoundAsset) && cachedSoundAsset.IsValid)
            {
                _logger.Output(Logger.OutputType.Info, "Found Cache!", Logger.OutputLevel.Debug);
                cachedSoundAsset.IncreaseReferenceCount();
                sound = cachedSoundAsset;
                return true;  
            }
            string FullLoadPath = System.IO.Path.Combine([BaseDirectory,
                            CurrentEngineConfig._EngineConfig.Assets.Directory,
                            "Audio",
                            audioRelativePath]
                        );

            CachedAsset<Sound> newRequestedSound = LoadSound(FullLoadPath);

            if(newRequestedSound.IsValid)
            {
                AddSoundToCache(audioRelativePath, newRequestedSound);
                sound = newRequestedSound;
                return true;
            }
            else
            {
                sound = null; // Invalid!
                _logger.Output(Logger.OutputType.Warning, $"Failed to load sound {audioRelativePath}", Logger.OutputLevel.Warning);
                return false;
            }

            
        } catch(Exception e)
        {
            _logger.Output(Logger.OutputType.ExceptionThrownError, "Uh oh! Failed to load sound...", e, Logger.OutputLevel.Error);
            sound = null; // Invalid!
            return false;
        }
    }

    internal static void AddSoundToCache(string AudioRelativePath, CachedAsset<Sound> _Sound)
    {
        if(_Sound.IsValid && SoundCache.Count < MaxSoundCacheAmount)
            SoundCache.Add(AudioRelativePath, _Sound);
        
        _logger.Output(Logger.OutputType.Info, $"Current SoundCache.Count: {SoundCache.Count}", Logger.OutputLevel.Info);
            
    }

    internal static Sound FindSoundInCache(string audioRelativePath)
    {
        CachedAsset<Sound> requestedSound;
        if (SoundCache.TryGet(audioRelativePath, out requestedSound))
        {
            if(requestedSound.IsValid)
                return requestedSound.Asset;
            else
            {
                _logger.Output(Logger.OutputType.Error, $"Cached sound is not valid?!: {audioRelativePath}", Logger.OutputLevel.Error);

                Raylib.UnloadSound(requestedSound.Asset);
                SoundCache.Remove(audioRelativePath);

                HandleInvalidCacheSounds();

                return InvalidAudio.InvalidSound; // Invalid!
            }
        }
        else 
            return InvalidAudio.InvalidSound; // Invalid!
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
        if(SoundCache.Count != 0)
        {
            foreach (var asset in SoundCache.PublicCache.Values)
                Raylib.UnloadSound(asset.Asset);
            SoundCache.Clear();
        }
    }
}
