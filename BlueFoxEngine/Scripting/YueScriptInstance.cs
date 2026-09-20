using BlueFoxEngine.Assets.Scripts;
using BlueFoxEngine.Logging;
using BlueFoxEngine.Scripting;

namespace BlueFoxEngine.Scripting;

public class YueScriptInstance : Object
{
    private Logger _logger = new Logger("YueScriptInstance");
    public YueScriptAsset Asset { get; }

    public YueScriptRuntime Runtime { get; }

    public YueScriptInstance(
        YueScriptAsset asset,
        YueScriptRuntime runtime)
    {
        this.Asset = asset;
        this.Runtime = runtime;
    }

    public void Execute()
    {
        if (!Asset.Compiled || Asset.CompiledLua.IsWhiteSpace() || Asset.CompiledLua == "" || Asset.CompiledLua == null)
        {
            _logger.Output(Logger.OutputType.Warning, Logger.OutputLevel.Warning, "Given asset is not compiled or failed to compile");
            return;
        }
        Runtime.Execute(Asset.CompiledLua);
    }

}