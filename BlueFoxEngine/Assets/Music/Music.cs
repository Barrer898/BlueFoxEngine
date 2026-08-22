using BlueFoxEngine.Logging;
using Raylib_cs;

namespace BlueFoxEngine.Assets;

/// <summary>
/// Represents a loaded music asset managed by BlueFoxEngine.
/// 
/// MusicAsset is responsible for the underlying Raylib music stream
/// and basic playback configuration such as looping.
/// 
/// The asset itself does not control when the music is played.
/// That responsibility belongs to MusicLayer / MusicPlayer.
/// </summary>
public class MusicAsset
{
    private readonly Logger _logger = new("MusicAsset");

    /// <summary>
    /// The underlying Raylib music stream.
    /// 
    /// This should generally be treated as read-only by users of the
    /// engine. The AssetManager should ultimately be responsible for
    /// unloading the underlying resource.
    /// </summary>
    public Raylib_cs.Music? _Music { get; private set; }

    /// <summary>
    /// Gets the underlying Raylib Music structure.
    /// </summary>
    public Raylib_cs.Music MusicValue => _Music.GetValueOrDefault();
    
    /// <summary>
    /// Gets whether this music stream is configured to loop.
    /// </summary>
    public bool Looping => _Music?.Looping ?? false;
    
    internal double _internalCurrentTime;
    internal double _internalMusicLength;

    /// <summary>
    /// Gets whether the underlying Raylib music stream is valid.
    /// </summary>
    public bool IsValid =>
        _Music.HasValue && Raylib.IsMusicValid(_Music.Value);

    /// <summary>
    /// Creates a MusicAsset around an already-loaded Raylib music stream.
    /// </summary>
    /// <param name="music">The loaded Raylib music stream.</param>
    /// <param name="looping">Whether the music should loop.</param>
    public MusicAsset(
        Raylib_cs.Music music,
        bool looping = false)
    {
        if (!Raylib.IsMusicValid(music))
        {
            _logger.Output(
                Logger.OutputType.Warning,
                Logger.OutputLevel.Warning, "Attempted to create MusicAsset from an invalid music stream.");

            _Music = null;
            return;
        }

        // Raylib_cs.Music is a struct, so modify a local copy and
        // assign it back to the field.
        music.Looping = looping;

        _Music = music;
    }
    
    


    
    /// <summary>
    /// Sets whether Raylib should automatically loop this music stream.
    /// </summary>
    public void SetLooping(bool looping)
    {
        if (_Music == null)
        {
            _logger.Output(
                Logger.OutputType.Warning,
                Logger.OutputLevel.Warning, "Cannot change looping state: music is null.");

            return;
        }

        var music = _Music.Value;
        music.Looping = looping;
        _Music = music;
    }

    /// <summary>
    /// Replaces the underlying music reference with another MusicAsset.
    /// 
    /// This does not unload either asset. Asset lifetime should be
    /// handled by the AssetManager / AssetCache.
    /// </summary>
    public void SetMusic(MusicAsset music)
    {
        if (music == null)
        {
            _logger.Output(
                Logger.OutputType.Warning,
                Logger.OutputLevel.Warning, "Given MusicAsset is null!");

            return;
        }

        if (!music.IsValid)
        {
            _logger.Output(
                Logger.OutputType.Warning,
                Logger.OutputLevel.Warning, "Given MusicAsset is not valid!");

            return;
        }

        _Music = music._Music;
    }
}


/// <summary>
/// Represents one logical music layer.
/// 
/// A MusicLayer controls playback of a MusicAsset without owning
/// the underlying Raylib resource.
/// 
/// This allows multiple layers to be controlled independently,
/// which is useful for layered / dynamic music systems.
/// </summary>
public class MusicLayer
{

    private readonly Logger _logger = new("MusicLayer");

    /// <summary>
    /// Gets the name of this layer.
    /// 
    /// Example:
    /// "Base", "Battle", "Tension", "Danger", etc.
    /// </summary>
    public string LayerName { get; init; }

    /// <summary>
    /// Prevents the current music from being replaced while playing.
    /// This behaves similarly to Click Team Fusion's uninterruptible
    /// music concept.
    /// </summary>
    public bool Uninterruptible { get; set; }

    /// <summary>
    /// Gets whether this layer is currently playing.
    /// </summary>
    public bool IsPlaying { get; private set; }

    public bool IsSequenced { get; init; }
    

    /// <summary>
    /// Gets whether this layer is currently paused.
    /// </summary>
    public bool IsPaused { get; private set; }

    /// <summary>
    /// Gets the music currently assigned to this layer.
    /// </summary>
    public MusicAsset? Music { get; private set; }

    public SequencedMusicAsset? SequencedMusic { get; private set; }

    public int LayerIndex { get; private set; } = -1;
    
    public void SetLayerIndex(int index)
    {
        LayerIndex = index;
    }

    public void SetSequencedMusic(SequencedMusicAsset sequencedMusicAsset, bool force = false)
    {
        if (sequencedMusicAsset == null)
        {
            _logger.Output(
                Logger.OutputType.Warning,
                Logger.OutputLevel.Warning, "Given MusicAsset is null!");

            return;
        }

        if (!sequencedMusicAsset.MusicAsset.IsValid)
        {
            _logger.Output(
                Logger.OutputType.Warning,
                Logger.OutputLevel.Warning, "Given MusicAsset is not valid!");

            return;
        }

        if (Uninterruptible && IsPlaying && !force)
        {
            _logger.Output(
                Logger.OutputType.Warning,
                Logger.OutputLevel.Warning, "Cannot Update this layer, is set as Uninterruptible?");

            return;
        }
        
        SequencedMusic = sequencedMusicAsset;
    }
    
    public float GetMusicTimePlayed()
    {
        if (this.Music != null)
            return Raylib.GetMusicTimePlayed(this.Music.MusicValue);
        else
            return 0f;
    }
    
    public float GetMusicTimeLength()
    {
        if (this.Music != null)
            return Raylib.GetMusicTimeLength(this.Music.MusicValue);
        else
            return 0f;
    }
    
    public void SeekMusicStream(float position)
    {
        if (this.Music == null) return;
        Raylib.SeekMusicStream(this.Music.MusicValue, position);
        this.Music._internalCurrentTime = position;
    }
    
    /// <summary>
    /// Gets or sets the volume of this layer.
    /// </summary>
    public float Volume { get; private set; } = 1.0f;

    /// <summary>
    /// Gets the number of additional loops remaining for this layer.
    /// </summary>

    public uint LoopCountLeft { get; private set; }

    public void AddLoops(uint count)
    {
        // Prevent uint overflow from silently wrapping around.
        if (uint.MaxValue - LoopCountLeft < count)
        {
            LoopCountLeft = uint.MaxValue;

            _logger.Output(
                Logger.OutputType.Warning,
                Logger.OutputLevel.Warning, "Music loop count reached uint.MaxValue.");

            return;
        }

        LoopCountLeft += count;
    }

    /// <summary>
    /// Removes loops from the current loop count.
    /// 
    /// The value is clamped to zero instead of allowing uint underflow.
    /// </summary>
    public void SubtractLoops(uint count)
    {
        LoopCountLeft = count >= LoopCountLeft
            ? 0
            : LoopCountLeft - count;
    }
    
    /// <summary>
    /// Clears the Music property when the currently assigned song
    /// finishes playing.
    /// </summary>
    public bool ClearLayerAfterSongEnds { get; set; }

    /// <summary>
    /// Creates an empty music layer.
    /// </summary>
    /// <param name="layerName">The name used to identify the layer.</param>
    public MusicLayer(string layerName)
    {
        LayerName = layerName;
    }

    /// <summary>
    /// Creates a music layer with an assigned music asset.
    /// </summary>
    public MusicLayer(
        string layerName,
        MusicAsset music,
        bool uninterruptible = false,
        bool clearLayerAfterSongEnds = false)
    {
        LayerName = layerName;
        Uninterruptible = uninterruptible;
        ClearLayerAfterSongEnds = clearLayerAfterSongEnds;
        IsSequenced = false;
        SequencedMusic = null;
        
        SetMusic(music, true);
    }        
    public MusicLayer(
        string layerName,
        SequencedMusicAsset sequencedMusicAsset,
        bool uninterruptible = false,
        bool clearLayerAfterSongEnds = false)
    {
        LayerName = layerName;
        Uninterruptible = uninterruptible;
        ClearLayerAfterSongEnds = clearLayerAfterSongEnds;
        IsSequenced = true;
        SequencedMusic = sequencedMusicAsset;
        
        
        SetMusic(sequencedMusicAsset.MusicAsset, true);
    }      
    public MusicLayer(
        string layerName,
        bool isSequenced = false,
        bool uninterruptible = false,
        bool clearLayerAfterSongEnds = false)
    {
        LayerName = layerName;
        Uninterruptible = uninterruptible;
        ClearLayerAfterSongEnds = clearLayerAfterSongEnds;
        IsSequenced = isSequenced;
    }      
    /// <summary>
    /// Assigns a music asset to this layer.
    /// 
    /// If the layer is uninterruptible and currently playing,
    /// the request is ignored.
    /// </summary>
    /// <param name="music">Music to assign.</param>
    /// <param name="force">
    /// Allows the music to be replaced even if the layer is
    /// uninterruptible.
    /// </param>
    public void SetMusic(MusicAsset? music, bool force = false)
    {
        if (this.IsSequenced)
        {
            _logger.Output(
                Logger.OutputType.Warning,
                Logger.OutputLevel.Warning, $"Cannot assign Music to a SequencedMusic Layer.");

            return;
        }
        
        if (music == null)
        {
            _logger.Output(
                Logger.OutputType.Warning,
                Logger.OutputLevel.Warning, $"Cannot assign null music to layer '{LayerName}'.");

            return;
        }

        if (!music.IsValid)
        {
            _logger.Output(
                Logger.OutputType.Warning,
                Logger.OutputLevel.Warning, $"Cannot assign invalid music to layer '{LayerName}'.");

            return;
        }

        if (Uninterruptible && IsPlaying && !force)
        {
            _logger.Output(
                Logger.OutputType.Notice,
                Logger.OutputLevel.Info, $"Ignoring SetMusic for '{LayerName}': layer is uninterruptible.");

            return;
        }

        // Stop the previous stream before replacing it.
        if (IsPlaying)
            StopLayer();

        Music = music;
        IsPaused = false;
    }
    
    public void SetMusic(SequencedMusicAsset? music, bool force = false)
    {
        if (!this.IsSequenced)
        {
            _logger.Output(
                Logger.OutputType.Warning,
                Logger.OutputLevel.Warning, $"Cannot assign SequencedMusic to a non-SequencedMusic Layer.");

            return;
        }
        
        if (music == null)
        {
            _logger.Output(
                Logger.OutputType.Warning,
                Logger.OutputLevel.Warning, $"Cannot assign null music to layer '{LayerName}'.");

            return;
        }

        if (!music.MusicAsset.IsValid)
        {
            _logger.Output(
                Logger.OutputType.Warning,
                Logger.OutputLevel.Warning, $"Cannot assign invalid music to layer '{LayerName}'.");

            return;
        }

        if (Uninterruptible && IsPlaying && !force)
        {
            _logger.Output(
                Logger.OutputType.Notice,
                Logger.OutputLevel.Info, $"Ignoring SetMusic for '{LayerName}': layer is uninterruptible.");

            return;
        }

        // Stop the previous stream before replacing it.
        if (IsPlaying)
            StopLayer();

        Music = music.MusicAsset;
        IsPaused = false;
    }

    /// <summary>
    /// Starts playing the music assigned to this layer.
    /// </summary>
    public void PlayLayer()
    {
        if (Music == null || !Music.IsValid)
        {
            _logger.Output(
                Logger.OutputType.Warning,
                Logger.OutputLevel.Warning, $"Cannot play layer '{LayerName}': no valid music assigned.");

            IsPlaying = false;
            IsPaused = false;
            return;
        }

        this.Music._internalCurrentTime = Raylib.GetMusicTimePlayed(Music.MusicValue);

        this.Music._internalMusicLength = Raylib.GetMusicTimeLength(Music.MusicValue);
        
        Raylib.PlayMusicStream(Music.MusicValue);

        IsPlaying = true;
        IsPaused = false;

        _logger.Output(
            Logger.OutputType.Info,
            Logger.OutputLevel.Debug, $"Playing music layer '{LayerName}'.");
    }

    /// <summary>
    /// Stops playback of this layer and resets its playback state.
    /// </summary>
    public void StopLayer()
    {
        if (Music == null || !Music.IsValid)
        {
            IsPlaying = false;
            IsPaused = false;
            return;
        }

        Raylib.StopMusicStream(Music.MusicValue);

        IsPlaying = false;
        IsPaused = false;

        _logger.Output(
            Logger.OutputType.Info,
            Logger.OutputLevel.Debug, $"Stopped music layer '{LayerName}'.");
    }

    /// <summary>
    /// Pauses the currently playing music layer.
    /// </summary>
    public void PauseLayer()
    {
        if (!IsPlaying || IsPaused)
            return;

        if (Music == null || !Music.IsValid)
        {
            IsPlaying = false;
            IsPaused = false;
            return;
        }

        
        
        Raylib.PauseMusicStream(Music.MusicValue);

        IsPaused = true;

        _logger.Output(
            Logger.OutputType.Info,
            Logger.OutputLevel.Debug, $"Paused music layer '{LayerName}'.");
    }

    /// <summary>
    /// Resumes a paused music layer.
    /// </summary>
    public void ResumeLayer()
    {
        if (!IsPlaying || !IsPaused)
            return;

        if (Music == null || !Music.IsValid)
        {
            IsPlaying = false;
            IsPaused = false;
            return;
        }

        Raylib.ResumeMusicStream(Music.MusicValue);
        
        this.Music._internalCurrentTime = this.GetMusicTimePlayed();

        IsPaused = false;

        _logger.Output(
            Logger.OutputType.Info,
            Logger.OutputLevel.Debug, $"Resumed music layer '{LayerName}'.");
    }

    /// <summary>
    /// Updates the music stream.
    /// 
    /// Raylib music streams must be updated every frame to continue
    /// feeding audio data to the playback device.
    /// </summary>
    /// <param name="deltaTime"></param>
    public void Update(double deltaTime)
    {
        if (!IsPlaying || Music == null || !Music.IsValid)
            return;

        Raylib.UpdateMusicStream(Music.MusicValue);

        if (IsPlaying)
        {
            this.Music._internalCurrentTime += deltaTime;
        }
        
        if (this.Music._internalCurrentTime < this.Music._internalMusicLength )
            return;
        
        if (LoopCountLeft > 0)
        {
            SubtractLoops(1);
            this.Music._internalCurrentTime = 0.0;
            
            Raylib.SeekMusicStream(
                Music.MusicValue,
                0.0f);

            Raylib.PlayMusicStream(
                Music.MusicValue);

            return;
        }

        StopLayer();
    }
    /// <summary>
    /// Sets the playback volume for this layer.
    /// </summary>
    public void SetVolume(float volume)
    {
        Volume = Math.Clamp(volume, 0.0f, 1.0f);

        if (Music == null || !Music.IsValid)
            return;

        Raylib.SetMusicVolume(
            Music.MusicValue,
            Volume);
    }
    

    /// <summary>
    /// Sets the number of additional loops this layer should perform.
    /// </summary>
    public void SetLoopCount(uint count)
    {
        LoopCountLeft = count;

        if (Music == null || !Music.IsValid)
            return;

        // Raylib handles indefinite looping through this property.
        // Finite loop counting is handled by MusicLayer.Update().
        Music.SetLooping(count > 0);
    }
}