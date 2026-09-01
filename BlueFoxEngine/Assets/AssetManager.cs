using BlueFoxEngine.Logging;
using BlueFoxEngine.Configuration;
using BlueFoxEngine.Assets.Textures;
using Raylib_cs;
using System.IO;

namespace BlueFoxEngine.Assets;


public static class AssetLoader
{
    private static Logger _logger = new Logger("AssetManager");
    
    private static readonly string BaseDirectory = AppContext.BaseDirectory;

    internal const uint BadAssetNukeMargin = 10;
    internal static uint CurrentBadAssetCount = 0;

    internal const uint MaxSoundCacheAmount = 250; // TODO: add this to config later or remove this limit
    private static readonly AssetCache<Sound> SoundCache = new();
    private static readonly AssetCache<MusicAsset> MusicCache = new();
    private static readonly AssetCache<TextureAsset> TextureCache = new();
    private static readonly AssetCache<Font> _fontsCache = new();

    #region Cache
    
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
            if (_cache.ContainsKey(path))
                throw new InvalidOperationException(
                    $"Asset '{path}' already exists in cache.");
            
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
        
        T asset = loader(path);

        if (!validator(asset))
            return new CachedAsset<T>(asset, false);

        cached = new CachedAsset<T>(asset, true);
        

        return cached;
    }
    
    #endregion
    #region Sound
    public static CachedAsset<Sound> LoadSound(string path)
    {
        return Load(
            SoundCache,
            path,
            Raylib.LoadSound,
            sound => Raylib.IsSoundValid(sound));
    }
    
    public static Sound LoadSoundResource(string audioRelativePath)
    {
        if (TryLoadSound(audioRelativePath, out var requestedSound))
        {
            return requestedSound.Asset;   
        }
        else
        {
            _logger.Output(Logger.OutputType.Warning, Logger.OutputLevel.Warning, "Failed to load requested sound");
            return InvalidAudio.InvalidSound; // Invalid!
        }
    }

    public static bool ReleaseSoundResource(string audioRelativePath)
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
            _logger.Output(Logger.OutputType.ExceptionThrownWarning,Logger.OutputLevel.Warning, "Failed to unload sound resource", new KeyNotFoundException($"The sound asset with relative path '{audioRelativePath}' was not found in the cache.")); 
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
                _logger.Output(Logger.OutputType.Info, Logger.OutputLevel.Debug, "Found Cache!");
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
                _logger.Output(Logger.OutputType.Warning, Logger.OutputLevel.Warning, $"Failed to load sound {audioRelativePath}");
                return false;
            }

            
        } catch(Exception e)
        {
            _logger.Output(Logger.OutputType.ExceptionThrownError, Logger.OutputLevel.Error, "Uh oh! Failed to load sound...", e);
            sound = null; // Invalid!
            return false;
        }
    }

    internal static void AddSoundToCache(string AudioRelativePath, CachedAsset<Sound> _Sound)
    {
        if(_Sound.IsValid && SoundCache.Count < MaxSoundCacheAmount)
            SoundCache.Add(AudioRelativePath, _Sound);
        
        _logger.Output(Logger.OutputType.Info, Logger.OutputLevel.Info, $"Current SoundCache.Count: {SoundCache.Count}");
            
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
                _logger.Output(Logger.OutputType.Error, Logger.OutputLevel.Error, $"Cached sound is not valid?!: {audioRelativePath}");

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
            _logger.Output(Logger.OutputType.Warning, Logger.OutputLevel.Warning, "Cleared sound cache due to invalid sounds");
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
    #endregion 
    #region Music

    /// <summary>
    /// Loads a Raylib music stream and wraps it inside a MusicAsset.
    /// 
    /// This method does not automatically add the asset to the cache.
    /// The caller is responsible for adding it using the normalized
    /// engine-relative asset path.
    /// </summary>
    public static CachedAsset<MusicAsset> LoadMusic(string path)
    {
        return Load(
            MusicCache,
            path,
            musicPath => new MusicAsset(Raylib.LoadMusicStream(musicPath)),
            music => music.IsValid);
    }

    /// <summary>
    /// Loads a music resource using an engine-relative path.
    ///
    /// Example:
    ///     "Music/Battle.ogg"
    ///
    /// The path is resolved relative to the engine's configured
    /// Audio directory before being passed to Raylib.
    /// </summary>
    public static MusicAsset? LoadMusicResource(string musicRelativePath)
    {
        if (TryLoadMusic(musicRelativePath, out var requestedMusic))
        {
            return requestedMusic!.Asset;
        }

        _logger.Output(
            Logger.OutputType.Warning,
            Logger.OutputLevel.Warning, "Failed to load requested music.");

        return null;
    }

    /// <summary>
    /// Attempts to load a music resource from the cache or disk.
    ///
    /// If the music is already cached, its reference count is increased.
    /// Otherwise, the music is loaded from disk and inserted into the cache.
    /// </summary>
    public static bool TryLoadMusic(
        string musicRelativePath,
        out CachedAsset<MusicAsset>? music)
    {
        try
        {
            // Normalize the path so that:
            // "Music/Battle.ogg"
            // "/Music/Battle.ogg"
            // are treated as the same asset.
            musicRelativePath = musicRelativePath.TrimStart(
                Path.DirectorySeparatorChar,
                Path.AltDirectorySeparatorChar);

            // Check whether this music is already loaded.
            if (MusicCache.TryGet(
                    musicRelativePath,
                    out var cachedMusicAsset) &&
                cachedMusicAsset.IsValid)
            {
                _logger.Output(
                    Logger.OutputType.Info,
                    Logger.OutputLevel.Debug, $"Found music cache entry: {musicRelativePath}");

                cachedMusicAsset.IncreaseReferenceCount();

                music = cachedMusicAsset;
                return true;
            }

            // Convert the engine-relative path into the actual filesystem path.
            string fullLoadPath = Path.Combine(
                BaseDirectory,
                CurrentEngineConfig._EngineConfig.Assets.Directory,
                "Audio",
                musicRelativePath);

            // Load the music stream through the generic loader.
            CachedAsset<MusicAsset> newRequestedMusic =
                LoadMusic(fullLoadPath);

            if (newRequestedMusic.IsValid)
            {
                AddMusicToCache(
                    musicRelativePath,
                    newRequestedMusic);

                music = newRequestedMusic;
                return true;
            }

            music = null;

            _logger.Output(
                Logger.OutputType.Warning,
                Logger.OutputLevel.Warning, $"Failed to load music {musicRelativePath}");

            return false;
        }
        catch (Exception e)
        {
            _logger.Output(
                Logger.OutputType.ExceptionThrownError,
                Logger.OutputLevel.Error, "Uh oh! Failed to load music...", e);

            music = null;
            return false;
        }
    }

    /// <summary>
    /// Adds a valid MusicAsset to the music cache.
    ///
    /// The cache key is always the engine-relative asset path rather
    /// than the absolute filesystem path.
    /// </summary>
    internal static void AddMusicToCache(
        string musicRelativePath,
        CachedAsset<MusicAsset> music)
    {
        if (music.IsValid)
            MusicCache.Add(musicRelativePath, music);

        _logger.Output(
            Logger.OutputType.Info,
            Logger.OutputLevel.Debug, $"Current MusicCache.Count: {MusicCache.Count}");
    }

    /// <summary>
    /// Unloads one reference to a music resource.
    ///
    /// The underlying Raylib music stream is only unloaded when the
    /// reference count reaches zero.
    /// </summary>
    public static bool ReleaseMusicResource(string musicRelativePath)
    {
        musicRelativePath = musicRelativePath.TrimStart(
            Path.DirectorySeparatorChar,
            Path.AltDirectorySeparatorChar);

        if (MusicCache.TryGet(
                musicRelativePath,
                out var cachedMusicAsset) &&
            cachedMusicAsset.IsValid)
        {
            cachedMusicAsset.DecreaseReferenceCount();

            _logger.Output(
                Logger.OutputType.Info,
                Logger.OutputLevel.Debug, $"Unloading music reference: {musicRelativePath}");

            _logger.Output(
                Logger.OutputType.Info,
                Logger.OutputLevel.Trace, $"Current Ref count: {cachedMusicAsset.ReferenceCount}");

            // Other users still have a reference to this asset.
            if (cachedMusicAsset.ReferenceCount > 0)
                return true;

            // Nobody owns the asset anymore.
            Raylib.UnloadMusicStream(
                cachedMusicAsset.Asset.MusicValue);

            MusicCache.Remove(musicRelativePath);

            return true;
        }

        _logger.Output(
            Logger.OutputType.ExceptionThrownWarning,
            Logger.OutputLevel.Warning, "Failed to unload music resource.", new KeyNotFoundException(
                $"The music asset with relative path " +
                $"'{musicRelativePath}' was not found in the cache."));

        return false;
    }

    /// <summary>
    /// Retrieves a music asset directly from the cache.
    ///
    /// Returns null if the asset is not currently cached or if the
    /// cached asset has become invalid.
    /// </summary>
    internal static MusicAsset? FindMusicInCache(
        string musicRelativePath)
    {
        musicRelativePath = musicRelativePath.TrimStart(
            Path.DirectorySeparatorChar,
            Path.AltDirectorySeparatorChar);

        if (MusicCache.TryGet(
                musicRelativePath,
                out var requestedMusic))
        {
            if (requestedMusic.IsValid)
                return requestedMusic.Asset;

            _logger.Output(
                Logger.OutputType.Error,
                Logger.OutputLevel.Error, $"Cached music is not valid?!: {musicRelativePath}");

            // Remove the invalid cache entry.
            MusicCache.Remove(musicRelativePath);

            return null;
        }

        return null;
    }

    /// <summary>
    /// Unloads every music stream currently stored in the cache.
    ///
    /// This should generally only be used when shutting down the engine
    /// or when deliberately clearing all loaded music.
    /// </summary>
    public static void ClearMusicCache()
    {
        if (MusicCache.Count == 0)
            return;

        foreach (var asset in MusicCache.PublicCache.Values)
        {
            if (!asset.IsValid)
                continue;

            Raylib.UnloadMusicStream(
                asset.Asset.MusicValue);
        }

        MusicCache.Clear();

        _logger.Output(
            Logger.OutputType.Info,
            Logger.OutputLevel.Debug, "Cleared music cache.");
    }

    #endregion
    #region Texture
    
    public static CachedAsset<TextureAsset> LoadTexture(string path)
    {
        return Load(TextureCache,
            path,
            textureLoader => new TextureAsset(Raylib.LoadTexture(textureLoader))
            ,validator => validator.IsValid);
    }

    public static bool TryLoadTexture(
        string textureRelativePath,
        out CachedAsset<TextureAsset>? texture)
    {
        try 
        {
            textureRelativePath = textureRelativePath.TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            
            if(TextureCache.TryGet(textureRelativePath, out var cachedTextureAsset) && cachedTextureAsset.IsValid) 
            { 
                _logger.Output(Logger.OutputType.Info, Logger.OutputLevel.Debug, "Found Cache!"); 
                cachedTextureAsset.IncreaseReferenceCount(); 
                texture = cachedTextureAsset; 
                return true;  
            } 
            string FullLoadPath = System.IO.Path.Combine([BaseDirectory, 
                    CurrentEngineConfig._EngineConfig.Assets.Directory, 
                    "Textures",
                    textureRelativePath]);

            CachedAsset<TextureAsset> newRequestedTexture = LoadTexture(FullLoadPath);
            
            if(newRequestedTexture.IsValid) 
            { 
                AddTextureToCache(textureRelativePath, newRequestedTexture); 
                texture = newRequestedTexture; 
                return true;
            }
            else 
            { 
                texture = null; // Invalid!
                _logger.Output(Logger.OutputType.Warning, Logger.OutputLevel.Warning, $"Failed to load sound {textureRelativePath}"); 
                return false;
            }

            
        } catch(Exception e) 
        {
            _logger.Output(Logger.OutputType.ExceptionThrownError, Logger.OutputLevel.Error, "Uh oh! Failed to load texture...", e);
            texture = null; // Invalid!
            return false;
        }
    }
    public static TextureAsset LoadTextureResource(string textureRelativePath)
    {
        if (TryLoadTexture(textureRelativePath, out var requestedTexture))
        {
            if(requestedTexture != null)  // why does this give me a compiler warning? annoying, but will try to clean out all warnings in the future. -B
                return requestedTexture.Asset;
            else
            {
                _logger.Output(Logger.OutputType.Warning, Logger.OutputLevel.Warning, "Failed to load requested texture");
                return new TextureAsset();
            }
        }
        else
        {
            _logger.Output(Logger.OutputType.Warning, Logger.OutputLevel.Warning, "Failed to load requested texture");
            return new TextureAsset();
        }
    }
    
    public static bool ReleaseTextureResource(string textureRelativePath)
    {
        TextureAsset? textureToUnload = FindTextureInCache(textureRelativePath);
        if(textureToUnload != null && Raylib.IsTextureValid(textureToUnload.Texture.Value) && TextureCache.TryGet(textureRelativePath, out var cachedTextureAsset))
        {
            cachedTextureAsset.DecreaseReferenceCount();
            if(cachedTextureAsset.ReferenceCount == 0)
            {
                Raylib.UnloadTexture(textureToUnload.Texture.Value);
                TextureCache.Remove(textureRelativePath);
            }
            return true;
        }
        else
        {
            _logger.Output(Logger.OutputType.ExceptionThrownWarning,Logger.OutputLevel.Warning, "Failed to unload Texture resource", new KeyNotFoundException($"The texture asset with relative path '{textureRelativePath}' was not found in the cache.")); 
            return false;
        }
    }
    
    internal static TextureAsset? FindTextureInCache(
        string textureRelativePath)
    {
        textureRelativePath = textureRelativePath.TrimStart(
            Path.DirectorySeparatorChar,
            Path.AltDirectorySeparatorChar);

        if (TextureCache.TryGet(
                textureRelativePath,
                out var requestedTexture))
        {
            if (requestedTexture.IsValid)
                return requestedTexture.Asset;

            _logger.Output(
                Logger.OutputType.Error,
                Logger.OutputLevel.Error, $"Cached texture is not valid?!: {textureRelativePath}");

            // Remove the invalid cache entry.
            TextureCache.Remove(textureRelativePath);

            return null;
        }

        return null;
    }
    
    internal static void AddTextureToCache(string textureRelativePath, CachedAsset<TextureAsset> texture)
    {
        if (texture.IsValid)
            TextureCache.Add(textureRelativePath, texture);

        _logger.Output(
            Logger.OutputType.Info,
            Logger.OutputLevel.Debug, $"Current TextureCache.Count: {TextureCache.Count}");
            
    }
    
    public static void ClearTextureCache()
    {
        if (TextureCache.Count == 0)
            return;

        foreach (var asset in TextureCache.PublicCache.Values)
        {
            if (!asset.IsValid)
                continue;

            if (asset.Asset.Texture == null) return;    
            
            Raylib.UnloadTexture(
                asset.Asset.Texture.Value);
        }

        TextureCache.Clear();

        _logger.Output(
            Logger.OutputType.Info,
            Logger.OutputLevel.Debug, "Cleared texture cache.");
    }
  
    #endregion
}
