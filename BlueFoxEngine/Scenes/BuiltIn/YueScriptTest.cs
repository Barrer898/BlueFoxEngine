using System.Numerics;
using BlueFoxEngine.Assets;
using BlueFoxEngine.Assets.Scripts;
using BlueFoxEngine.Assets.Textures;
using BlueFoxEngine.Assets.Sprite;
using BlueFoxEngine.Components;
using BlueFoxEngine.Components.DebugComponents;
using BlueFoxEngine.Scripting;
using BlueFoxEngine.Helper;
using BlueFoxEngine.Logging;
using KeraLua;
using Raylib_cs;

namespace BlueFoxEngine.Scenes.BuiltIn;
public sealed class YueScriptTest : Scene
{
    private YueScriptRuntime yueRuntime = new YueScriptRuntime();
    private TextureAsset debugTexture;
    private Sprite TestSprite;
    private YueScriptComponent TestComponent;
    
    private double _time;
    
    public override void Load()
    {
        YueScriptAsset yueScript;
        YueScriptAsset yueScript2;
        AssetLoader.TryLoadYueScript("HelloWorld.yue", out yueScript);
        yueScript.CompileSourceFromFile();
        YueScriptInstance yueInstance = new YueScriptInstance(yueScript,yueRuntime);
        yueInstance.Execute();
        
        //2nd run test
        AssetLoader.TryLoadYueScript("HelloWorld.yue", out yueScript2);
        YueScriptInstance yueInstance2 = new YueScriptInstance(yueScript2,yueRuntime);
        yueInstance2.Execute();
        
        Console.WriteLine(
            ReferenceEquals(yueScript, yueScript2)
        );

        debugTexture = AssetLoader.LoadTextureResource("debug.png");
        TestSprite = new Sprite(debugTexture);

        TestComponent = new YueScriptComponent(AssetLoader.LoadYueScript("TestComponent.yue").Asset);
        TestSprite.AddComponent<YueScriptComponent>(TestComponent);
        
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