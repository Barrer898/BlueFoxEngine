using BlueFoxEngine.Assets.Scripts;
using BlueFoxEngine.Logging;
using BlueFoxEngine.Scripting;

namespace BlueFoxEngine.Scripting;

/// <summary>
/// Wraps a YueScriptAsset and provides an execution layer backed by
/// a dedicated Lua runtime instance.
///
/// Inherits from Object so the script instance participates in the
/// engine's standard lifecycle (SceneObjectList, dispose chain, etc.).
/// </summary>
public class YueScriptInstance : BlueFoxEngine.Assets.Object
{
    private Logger _logger = new("YueScriptInstance");

    /// <summary>
    /// The underlying script asset containing the compiled Lua bytecode.
    /// </summary>
    public YueScriptAsset Asset { get; }

    /// <summary>
    /// The Lua runtime this instance executes within.
    /// </summary>
    public YueScriptRuntime Runtime { get; }

    public YueScriptInstance(
        YueScriptAsset asset,
        YueScriptRuntime runtime)
    {
        this.Asset = asset;
        this.Runtime = runtime;
    }

    /// <summary>
    /// Executes the compiled Lua bytecode in this instance's runtime.
    ///
    /// If the asset has not been compiled (or compilation failed), the
    /// call is silently skipped with a warning.
    /// </summary>
    public void Execute()
    {
        if (!Asset.Compiled || string.IsNullOrWhiteSpace(Asset.CompiledLua) || Asset.CompiledLua == null)
        {
            _logger.Output(Logger.OutputType.Warning, Logger.OutputLevel.Warning, "Given asset is not compiled or failed to compile.");
            return;
        }
        Runtime.Execute(Asset.CompiledLua);
    }
}
