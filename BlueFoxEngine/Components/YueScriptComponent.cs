using BlueFoxEngine.Assets.Scripts;
using BlueFoxEngine.Logging;
using BlueFoxEngine.Scripting;

namespace BlueFoxEngine.Components;

/// <summary>
/// Attaches a YueScript to an Object so it is executed automatically.
///
/// The component holds a YueScriptInstance which wraps a YueScriptAsset.
/// When the object initializes, the component attempts to compile the
/// script if it has not been compiled yet. On every update, the
/// compiled bytecode is re-executed — callers should gate their logic
/// in a way that avoids unintended re-execution each frame.
///
/// If the component is added to an Object that is disposed, the
/// associated script asset is also disposed.
/// </summary>
public class YueScriptComponent : CSharpComponent
{
    private Logger _logger = new("YueScriptComponent");

    /// <summary>
    /// The script instance this component owns and executes.
    /// Null until SetAsset is called or the asset is resolved.
    /// </summary>
    public YueScriptInstance? Instance { get; private set; }

    /// <summary>
    /// The underlying script asset. May be null if SetAsset was not called.
    /// </summary>
    public YueScriptAsset? Asset => Instance?.Asset;

    private YueScriptRuntime? _runtime;

    /// <summary>
    /// Creates an empty component. Call SetAsset before adding to an Object.
    /// </summary>
    public YueScriptComponent()
    {
    }

    /// <summary>
    /// Creates a component attached to the given script asset.
    /// </summary>
    public YueScriptComponent(YueScriptAsset asset)
    {
        SetAsset(asset);
    }

    /// <summary>
    /// Sets the script asset and creates a dedicated runtime for it.
    /// </summary>
    public void SetAsset(YueScriptAsset asset)
    {
        if (asset == null)
        {
            _logger.Output(Logger.OutputType.Warning, Logger.OutputLevel.Warning, "Cannot assign null asset to YueScriptComponent.");
            return;
        }

        // Each component gets its own runtime so scripts don't collide.
        _runtime = new YueScriptRuntime();

        Instance = new YueScriptInstance(asset, _runtime);
    }

    /// <summary>
    /// Executes the assigned script once.
    ///
    /// If the asset has not been compiled yet, this method attempts
    /// compilation automatically.
    /// </summary>
    public void Execute()
    {
        if (Instance == null)
        {
            _logger.Output(Logger.OutputType.Warning, Logger.OutputLevel.Warning, "No script assigned to YueScriptComponent. Call SetAsset() first.");
            return;
        }

        // Attempt lazy compilation if not yet compiled.
        if (!Instance.Asset.Compiled)
        {
            Instance.Asset.CompileSourceFromFile();
        }

        Instance.Execute();
    }

    /// <summary>
    /// Runs Execute() on every update tick.
    ///
    /// Be cautious — this re-executes the script every frame. Use a
    /// guard inside the script itself if per-frame execution is not desired.
    /// </summary>
    public override void Update(double deltaTime)
    {
        Execute();
    }

    /// <summary>
    /// Overrides the base dispose logic to clean up the Lua runtime.
    /// </summary>
    public override void Dispose()
    {
        _runtime?.Dispose();
        _runtime = null;
        base.Dispose();
    }
}
