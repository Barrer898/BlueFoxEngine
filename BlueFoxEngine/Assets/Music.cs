using BlueFoxEngine.Logging;
using Raylib_cs;

namespace BlueFoxEngine.Assets;

public class Music
{
    private Logger _logger = new Logger("MusicClass");

    public Raylib_cs.Music? _Music { get; private set; } = null;
    public Raylib_cs.Music MusicValue => _Music.GetValueOrDefault();

    public bool Looping => _Music.GetValueOrDefault().Looping;
    //public int CurrentOffset = _Music.GetValueOrDefault();
    public uint LoopCount { get; private set; }
        

    /// <summary>
    /// Creates the internal Music class object.
    /// </summary>
    /// <param name="music">The Song itself</param>
    /// <param name="looping">Do music looping logic</param>
    /// <param name="loopCount">How many loops should the song do</param>
    Music(Raylib_cs.Music music, bool looping=false, uint loopCount=0)
    {
        this._Music = music;
        Raylib_cs.Music musicValue = this._Music.GetValueOrDefault();
        musicValue.Looping = looping;
        this.LoopCount = loopCount;
    }

    public void AddLoops(uint count)
    {
        LoopCount += count;
    }
    public void SubtractLoops(uint count)
    {
        LoopCount -= count;
    }
    
    public void SetMusic(Music music)
    {
        if (music._Music == null)
        {
            _logger.Output(Logger.OutputType.Warning, "Given music variable is null!", Logger.OutputLevel.Warning);
            return;
        }
        if (Raylib.IsMusicValid((Raylib_cs.Music)music._Music))
        {
            this._Music = music._Music;
        }
        else
        {
            _logger.Output(Logger.OutputType.Warning, "Given music variable is not valid!", Logger.OutputLevel.Warning);
        }
    }
}

public class MusicLayer
{ 
    private Logger _logger = new Logger("MusicLayerClass");
    public string LayerName { get; init; }


    /// <summary>
    /// Works like in ClickTeamFusion, prevents the song to be changed whilst one is playing.
    /// </summary>
    public bool Uninterruptible { get; set; }
    private Music? Music { get;  set; }
    /// <summary>
    /// Clears the Music variable (Sets as NULL) after completing the Song.
    /// </summary>
    public bool ClearLayerAfterSongEnds { get; set; }

    public void SetMusic(Music music)
    {
        if (Uninterruptible)
        {
            _logger.Output(Logger.OutputType.Notice, "Ignoring SetMusic, Layer set as Uninterruptible", Logger.OutputLevel.Info);
            return;
        }

        if (music._Music == null)
        {
            _logger.Output(Logger.OutputType.Warning, "Given music variable is null!", Logger.OutputLevel.Warning);
            return;
        }
        if (Raylib.IsMusicValid((Raylib_cs.Music)music._Music))
        {
            this.Music = music;
        }
        else
        {
            _logger.Output(Logger.OutputType.Warning, "Given music variable is not valid!", Logger.OutputLevel.Warning);
        }
    }

    public void PlayLayer()
    {
        // TODO
    }
    public void StopLayer()
    {
        // TODO
    }
    public void PauseLayer()
    {
        // TODO
    }
    public void ResumeLayer()
    {
        // TODO    
    }
    
    MusicLayer(string layerName, Music music)
    {
        this.LayerName = layerName;
        this.Music = music;
    }
    MusicLayer(string layerName, Music music, bool uninterruptible)
    {
        this.LayerName = layerName;
        this.Music = music;
        this.Uninterruptible = uninterruptible;
    }
    MusicLayer(string layerName, Music music, bool uninterruptible, bool clearLayerAfterSongEnds)
    {
        this.LayerName = layerName;
        this.Music = music;
        this.Uninterruptible = uninterruptible;
        this.ClearLayerAfterSongEnds = clearLayerAfterSongEnds;
    }
}