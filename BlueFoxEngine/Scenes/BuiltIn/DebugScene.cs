using BlueFoxEngine.Assets;
using BlueFoxEngine.Helper;
using BlueFoxEngine.Logging;
using Raylib_cs;

namespace BlueFoxEngine.Scenes.BuiltIn;
public sealed class DebugScene : Scene
{
    private double _time;
    private Sound testSound;
    private Logger _logger = new("LoadingScene");
    private bool testAudioLoader = false;
    private MusicAsset testSong;
    private MusicLayer PrimaryMusicLayer = new MusicLayer("PrimaryMusicLayer");
    private MusicPlayer musicPlayer;
    
    public override void Load()
    {
        _time = 0;
        testSound = (Sound)AssetLoader.LoadSoundResource("Sound/blipClick.wav");
        testSong = AssetLoader.LoadMusicResource("Music/loveshy - Like That.ogg");
        musicPlayer = new MusicPlayer();
    }

    public override void Unload()
    {
    }

    public override void Update(double deltaTime)
    {
        _time += deltaTime;
        //musicPlayer.Update();
    }

    public override void Draw()
    {
        float alpha = (float)((Math.Sin(_time * 2.0) + 1.0) * 0.5);
        
        Color color = new Color((byte)255, (byte)255, (byte)255, (byte)(alpha * 255));

        int margin = 20;
        int x = Screen.Width - Text.MeasureTextWidth("Loading...", 24) - margin;
        int y = Screen.Height - 24 - margin;

        if (alpha < 0.0001f)
        {
            SoundPlayer.PlaySound(testSound ,1f);
            //_logger.Output(Logger.OutputType.Info, $"{testSound.ReferenceCount}", Logger.OutputLevel.Debug);
            if (!testAudioLoader)
            {
                Sound testSound2 = (Sound)AssetLoader.LoadSoundResource("Sound/blipClick.wav");
                AssetLoader.ClearSoundCache();
                testAudioLoader = true;
                musicPlayer.AddLayer(PrimaryMusicLayer);
                musicPlayer.PlayMusic(testSong, (uint)PrimaryMusicLayer.LayerIndex);
            }
            _logger.Output(Logger.OutputType.Info, Logger.OutputLevel.Debug, $"loveshy - Like That: {Raylib.GetMusicTimePlayed(testSong.MusicValue)}");
             
        }
        
        Raylib.DrawText("Loading...", x, y, 24, color);
    }
}