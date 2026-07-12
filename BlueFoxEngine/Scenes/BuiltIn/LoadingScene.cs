using BlueFoxEngine.Scenes;
using BlueFoxEngine.Helper;
using Raylib_cs;

namespace BlueFoxEngine.Scenes.BuiltIn;
public sealed class LoadingScene : Scene
{
    private double _time;

    public override void Load()
    {
        _time = 0;
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

        Raylib.DrawText("Loading...", x, y, 24, Color.White);
    }
}