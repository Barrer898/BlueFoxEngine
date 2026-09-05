using System.Numerics;
using BlueFoxEngine.Assets;
using BlueFoxEngine.Assets.Textures;
using BlueFoxEngine.Assets.Sprite;
using BlueFoxEngine.Components.DebugComponents;
using BlueFoxEngine.Helper;
using BlueFoxEngine.Logging;
using Raylib_cs;

namespace BlueFoxEngine.Scenes.BuiltIn;
public sealed class DebugSceneButDifferent : Scene
{

    private double _time;
    
    public override void Load()
    {
        
        
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
        Raylib.DrawText("Loading...", 500, 500, 15, Color.White);
    }
}