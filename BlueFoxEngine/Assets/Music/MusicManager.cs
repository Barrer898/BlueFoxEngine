
using BlueFoxEngine.Assets;
using BlueFoxEngine.Configuration;
using BlueFoxEngine.Logging;
using BlueFoxEngine.Scenes;
using Raylib_cs;
namespace BlueFoxEngine.Assets;

/// <summary>
/// Controls all music layers used by the engine.
///
/// MusicPlayer is responsible for:
/// - Creating and managing music layers.
/// - Starting music on an available layer.
/// - Stopping individual/all music layers.
/// - Updating all active music streams.
/// - Handling global music volume.
///
/// MusicPlayer does not own the underlying MusicAsset resources.
/// Those are managed by AssetLoader and its asset cache.
/// </summary>
public class MusicPlayer
{
    private readonly Logger _logger = new("MusicPlayer");

    private String UID;
    
    /// <summary>
    /// Gets all music layers available to the music player.
    ///
    /// The number of layers is configured through EngineConfig.
    /// </summary>
    public MusicLayer[] Layers { get; private set;  }

    /// <summary>
    /// Gets or sets the global music volume.
    ///
    /// The value is clamped between 0.0 and 1.0.
    /// </summary>
    public float Volume
    {
        get => _volume;
        set => _volume = Math.Clamp(value, 0.0f, 1.0f);
    }

    private float _volume = 1.0f;

    /// <summary>
    /// Creates a new MusicPlayer using the configured number
    /// of music layers.
    /// </summary>
    public MusicPlayer()
    {
        int layerCount =
            CurrentEngineConfig._EngineConfig.Audio.MusicLayerCount;

        if (layerCount <= 0)
        {
            _logger.Output(
                Logger.OutputType.Warning,
                Logger.OutputLevel.Warning, "MusicLayerCount is zero or negative. MusicPlayer will contain no layers.");

            Layers = Array.Empty<MusicLayer>();
            return;
        }

        Layers = new MusicLayer[layerCount];

        UID = SceneManager.RegisterMusicPlayer(this);
    }

    public int AddLayer(MusicLayer musicLayer)
    {
        if (musicLayer.LayerIndex != -1)
            return -1;
        int idx = FindAvailableLayer();
        if(idx != -1)
        {
            this.Layers[idx] = musicLayer;
            musicLayer.SetLayerIndex(idx);
        }
        return idx;
    }
    
    public bool RemoveLayer(int idx)
    {
        if (idx > 0 && idx < CurrentEngineConfig._EngineConfig.Audio.MusicLayerCount)
            this.Layers[idx] = null;
        else
            return false;
        return true;
    }
    
    /// <summary>
    /// Attempts to play a MusicAsset on the first available layer.
    ///
    /// An available layer is one which is not currently playing.
    /// </summary>
    /// <param name="music">Music asset to play.</param>
    /// <param name="volume">
    /// Volume for this music stream, from 0.0 to 1.0.
    /// </param>
    /// <param name="loopCount">
    /// Number of additional loops requested by the caller.
    /// </param>
    /// <returns>
    /// True if the music was assigned to a layer and started.
    /// False if no layer was available or the asset was invalid.
    /// </returns>
    public bool PlayMusic(
        MusicAsset music,
        uint LayerIndex,
        float volume = 1.0f,
        uint loopCount = 0)
    {
        if (music == null || !music.IsValid)
        {
            _logger.Output(
                Logger.OutputType.Warning,
                Logger.OutputLevel.Warning, "Cannot play invalid music asset.");

            return false;
        }

        MusicLayer? availableLayer = this.Layers[LayerIndex];

        if (availableLayer == null)
        {
            _logger.Output(
                Logger.OutputType.Warning,
                Logger.OutputLevel.Warning, "No available music layers.");

            return false;
        }

        volume = Math.Clamp(volume, 0.0f, 1.0f);

        // Configure the requested number of loops.
        //
        // MusicAsset itself remains shared, so we do not modify its
        // LoopCount here. The layer needs to own playback-specific
        // state instead.
        music.SetLooping(loopCount > 0);

        availableLayer.SetVolume(volume * Volume);
        availableLayer.SetLoopCount(loopCount);
        availableLayer.SetMusic(music);
        availableLayer.PlayLayer();

        _logger.Output(
            Logger.OutputType.Info,
            Logger.OutputLevel.Debug, $"Playing music on layer '{availableLayer.LayerName}'.");

        return true;
    }

    /// <summary>
    /// Stops the first currently playing music layer.
    /// </summary>
    public void StopMusic()
    {
        foreach (MusicLayer layer in Layers)
        {
            if (!layer.IsPlaying)
                continue;

            layer.StopLayer();

            _logger.Output(
                Logger.OutputType.Info,
                Logger.OutputLevel.Debug, $"Stopped music layer '{layer.LayerName}'.");

            return;
        }
    }

    /// <summary>
    /// Stops every currently playing music layer.
    /// </summary>
    public void StopAllMusic()
    {
        foreach (MusicLayer layer in Layers)
        {
            if (!layer.IsPlaying)
                continue;

            layer.StopLayer();
        }

        _logger.Output(
            Logger.OutputType.Info,
            Logger.OutputLevel.Debug, "Stopped all music layers.");
    }

    /// <summary>
    /// Updates every active music layer.
    ///
    /// This should be called once per frame from the engine's
    /// update loop.
    /// </summary>
    public void Update()
    {
        foreach (MusicLayer layer in Layers)
        {
            if(layer != null)
                layer.Update();
        }
    }

    /// <summary>
    /// Finds the first index of a layer that is currently empty.
    /// </summary>
    private int FindAvailableLayer()
    {
        for (int i = 0; i < CurrentEngineConfig._EngineConfig.Audio.MusicLayerCount; i++ )  
        {
            if (Layers[i] == null)
                return i;
        }

        return -1;
    }
}
