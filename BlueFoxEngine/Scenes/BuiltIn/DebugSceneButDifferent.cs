using System.Numerics;
using BlueFoxEngine.Assets;
using BlueFoxEngine.Assets.Scripts;
using BlueFoxEngine.Assets.Textures;
using BlueFoxEngine.Assets.Sprite;
using BlueFoxEngine.Components.DebugComponents;
using BlueFoxEngine.Scripting;
using BlueFoxEngine.Helper;
using BlueFoxEngine.Logging;
using KeraLua;
using Raylib_cs;

namespace BlueFoxEngine.Scenes.BuiltIn;
public sealed class DebugSceneButDifferent : Scene
{
    private YueScriptRuntime yueRuntime = new YueScriptRuntime();
   
    
    private double _time;
    
    public override void Load()
    {
        YueScriptAsset yueScriptAsset = new YueScriptAsset("HelloWorld.yue");
        yueScriptAsset.CompileSourceFromFile();
        YueScriptInstance yueScriptInstance = new YueScriptInstance(yueScriptAsset, yueRuntime);
        yueScriptInstance.Execute();
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