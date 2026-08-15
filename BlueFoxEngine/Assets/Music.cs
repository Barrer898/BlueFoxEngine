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
    
    /// <summary>
    /// Gets the number of loops remaining.
    /// 
    /// A value of 0 means that no additional loop limit is being
    /// requested by MusicAsset itself.
    /// </summary>
    public uint LoopCount { get; private set; }

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
    /// <param name="loopCount">
    /// The number of additional loops requested.
    /// </param>
    public MusicAsset(
        Raylib_cs.Music music,
        bool looping = false,
        uint loopCount = 0)
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
        LoopCount = loopCount;
    }

    /// <summary>
    /// Adds additional loops to the current loop count.
    /// </summary>
    public void AddLoops(uint count)
    {
        // Prevent uint overflow from silently wrapping around.
        if (uint.MaxValue - LoopCount < count)
        {
            LoopCount = uint.MaxValue;

            _logger.Output(
                Logger.OutputType.Warning,
                Logger.OutputLevel.Warning, "Music loop count reached uint.MaxValue.");

            return;
        }

        LoopCount += count;
    }

    /// <summary>
    /// Removes loops from the current loop count.
    /// 
    /// The value is clamped to zero instead of allowing uint underflow.
    /// </summary>
    public void SubtractLoops(uint count)
    {
        LoopCount = count >= LoopCount
            ? 0
            : LoopCount - count;
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
        LoopCount = music.LoopCount;
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
    /// 
    /// This behaves similarly to Clickteam Fusion's uninterruptible
    /// music concept.
    /// </summary>
    public bool Uninterruptible { get; set; }

    /// <summary>
    /// Gets whether this layer is currently playing.
    /// </summary>
    public bool IsPlaying { get; private set; }

    /// <summary>
    /// Gets whether this layer is currently paused.
    /// </summary>
    public bool IsPaused { get; private set; }

    /// <summary>
    /// Gets the music currently assigned to this layer.
    /// </summary>
    public MusicAsset? Music { get; private set; }

    public int LayerIndex { get; private set; } = -1;

    public void SetLayerIndex(int idx)
    {
        LayerIndex = idx;
    }
    
    /// <summary>
    /// Gets or sets the volume of this layer.
    /// </summary>
    public float Volume { get; private set; } = 1.0f;

    /// <summary>
    /// Gets the number of additional loops remaining for this layer.
    /// </summary>
    public uint LoopCount { get; private set; }
    
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

        SetMusic(music, true);
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
    public void Update()
    {
        if (!IsPlaying || IsPaused)
            return;

        if (Music == null || !Music.IsValid)
        {
            IsPlaying = false;
            IsPaused = false;
            return;
        }

        Raylib.UpdateMusicStream(Music.MusicValue);

        // Raylib's music stream can tell us whether playback has
        // reached the end when looping is disabled.
        if (!Music.Looping && !Raylib.IsMusicStreamPlaying(Music.MusicValue))
        {
            IsPlaying = false;
            IsPaused = false;

            if (ClearLayerAfterSongEnds)
                Music = null;
        }
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
        LoopCount = count;

        if (Music == null || !Music.IsValid)
            return;

        // Raylib handles indefinite looping through this property.
        // Finite loop counting is handled by MusicLayer.Update().
        Music.SetLooping(count > 0);
    }
}