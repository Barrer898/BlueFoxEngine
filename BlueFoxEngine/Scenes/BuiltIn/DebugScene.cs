using System.Numerics;
using BlueFoxEngine.Assets;
using BlueFoxEngine.Assets.Textures;
using BlueFoxEngine.Assets.Sprite;
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
    private SequencedMusicAsset testSong;
    private MusicLayer PrimaryMusicLayer = new MusicLayer("PrimaryMusicLayer", true);
    private MusicPlayer musicPlayer;
    private TextureAsset debugTexture;
    private Sprite testSprite;
    
    public override void Load()
    {
        _time = 0;
        testSound = AssetLoader.LoadSoundResource("Sound/blipClick.wav");
        MusicAsset temp = AssetLoader.LoadMusicResource("Music/Razormind.ogg");
        List<MusicSequence> list = new List<MusicSequence>();
        
        // just an example
        list.Add(new MusicSequence(0f, 102.52f, () => false));
        list.Add(new MusicSequence(102.52f, 103.44f, () => true));

        debugTexture = AssetLoader.LoadTextureResource("debug.png");
        
        testSong = new SequencedMusicAsset(list, temp);

        testSprite = new Sprite(debugTexture);
        testSprite.Position = new Vector2(640,360) ;
        testSprite.SetOriginCenter();
        
        musicPlayer = new MusicPlayer();
        
        
    }

    public override void Unload()
    { 
        musicPlayer.Destroy();
    }

    public override void Update(double deltaTime)
    {
        _time += deltaTime;
        testSprite.Rotation += 0.02f;
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
                Sound testSound2 = AssetLoader.LoadSoundResource("Sound/blipClick.wav");
                AssetLoader.ClearSoundCache();
                testAudioLoader = true;
                musicPlayer.AddLayer(PrimaryMusicLayer);
                
                
                musicPlayer.PlayMusic(testSong, (uint)PrimaryMusicLayer.LayerIndex, 0.75f, 2);
            }
            _logger.Output(Logger.OutputType.Info, Logger.OutputLevel.Debug, $"Razormind: {PrimaryMusicLayer.GetMusicTimePlayed()}, LoopsLeft: {PrimaryMusicLayer.LoopCountLeft}");
             
        }

        testSprite.Draw();
        
        Raylib.DrawText("Loading...", x, y, 24, color);
    }
}