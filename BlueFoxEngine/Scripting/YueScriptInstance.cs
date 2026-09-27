using BlueFoxEngine.Assets.Scripts;
using BlueFoxEngine.Logging;
using BlueFoxEngine.Scripting;

namespace BlueFoxEngine.Scripting;

/// <summary>
/// Wraps a YueScriptAsset and provides an execution layer backed by
/// a Lua runtime.
///
/// Each instance is registered with its own unique Script table in the
/// runtime, so OnUpdate callbacks from different instances do not
/// overwrite each other.
///
/// Inherits from Object so the script instance participates in the
/// engine's standard lifecycle (SceneObjectList, dispose chain, etc.).
/// </summary>
public class YueScriptInstance
{
    private Logger _logger = new("YueScriptInstance");

    /// <summary>
    /// The underlying script asset containing the compiled Lua source.
    /// </summary>
    public YueScriptAsset Asset { get; }

    /// <summary>
    /// The Lua runtime this instance executes within.
    /// </summary>
    public YueScriptRuntime Runtime { get; }

    /// <summary>
    /// Unique identifier for this script within the runtime.
    /// Generated automatically when the instance is created.
    /// </summary>
    public ScriptEntry ScriptEntry { get; }

    public YueScriptInstance(
        YueScriptAsset asset,
        YueScriptRuntime runtime)
    {
        this.Asset = asset;
        this.Runtime = runtime;
        this.ScriptEntry = runtime.RegisterScript();
    }

    /// <summary>
    /// Executes the compiled Lua source in this instance's runtime.
    ///
    /// The asset's Script table is scoped to this instance's unique
    /// _G.Scripts[scriptEntry.ScriptId].Script, so OnUpdate functions
    /// from other scripts in the same runtime will not be affected.
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
        Runtime.Execute(Asset.CompiledLua, ScriptEntry.ScriptId);
    }
}
