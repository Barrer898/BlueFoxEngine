using BlueFoxEngine.Assets;
using BlueFoxEngine.Assets.Audio;
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

    public override void Load()
    {
        _time = 0;
        testSound = (Sound)AssetLoader.LoadSoundResource("Sound/blipClick.wav");
    }

    public override void Unload()
    {
    }

    public override void Update(double deltaTime)
    {
        _time += deltaTime;
    }

    public override void Draw()
    {
        float alpha = (float)((Math.Sin(_time * 2.0) + 1.0) * 0.5);

        // Replace these with your engine's text drawing API.
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
                testAudioLoader = true;
            }
            //_logger.Output(Logger.OutputType.Info, $"{testSound}", Logger.OutputLevel.Debug);
            AssetLoader.ClearSoundCache();
        }
        
        Raylib.DrawText("Loading...", x, y, 24, color);
    }
}