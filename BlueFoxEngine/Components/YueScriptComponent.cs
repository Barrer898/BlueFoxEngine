using BlueFoxEngine.Assets.Scripts;
using BlueFoxEngine.Logging;
using BlueFoxEngine.Scripting;
using KeraLua;

namespace BlueFoxEngine.Components;

/// <summary>
/// Attaches a YueScript to an Object so it is executed automatically.
///
/// The component holds a YueScriptInstance which wraps a YueScriptAsset.
/// When the object initializes, the component attempts to compile the
/// script if it has not been compiled yet.
///
/// If the component is added to an Object that is disposed, the
/// associated script asset is also disposed.
/// </summary>
public class YueScriptComponent : CSharpComponent
{
    public string ComponentName { get; private set; } = "";
    private Logger _logger;
    private bool externalRuntime;

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
    public YueScriptComponent(string? componentName = null)
    {
        if (!String.IsNullOrEmpty(componentName))
        {
            this.ComponentName = componentName;
            this._logger = new Logger(componentName);
        }
        else
        {
            this._logger = new Logger("UnNamedComponent");
        }

        this.externalRuntime = false;
    }

    /// <summary>
    /// Creates a component attached to the given script asset with its own YueScriptRuntime.
    /// </summary>
    public YueScriptComponent(YueScriptAsset asset, string? componentName = null)
    {
        if (!String.IsNullOrEmpty(componentName))
        {
            this.ComponentName = componentName;
            this._logger = new Logger(componentName);
        }
        else
        {
            this._logger = new Logger("UnNamedComponent");
        }
        this.externalRuntime = false;
        SetAsset(asset);
    }
    public YueScriptComponent(YueScriptAsset asset, YueScriptRuntime runtime, string? componentName = null)
    {
        if (!String.IsNullOrEmpty(componentName))
        {
            this.ComponentName = componentName;
            this._logger = new Logger(componentName);
        }
        else
        {
            this._logger = new Logger("UnnamedYueComponent");
        }

        this._runtime = runtime;
        this.externalRuntime = true;
        SetAsset(asset);
    }

    /// <summary>
    /// Sets the script asset and creates a dedicated runtime for it, unless one already exists.
    /// </summary>
    public void SetAsset(YueScriptAsset asset)
    {
        if (asset == null)
        {
            _logger.Output(
                Logger.OutputType.Warning,
                Logger.OutputLevel.Warning,
                "Cannot assign null asset to YueScriptComponent."
            );

            return;
        }

        if (Instance != null)
            return;

        if (_runtime == null)
        {
            _runtime = new YueScriptRuntime(
                this.ComponentName,
                isComponent: true
            );

            externalRuntime = false;
        }

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
            _logger.Output(Logger.OutputType.Warning, Logger.OutputLevel.Warning,
                "No script assigned to YueScriptComponent. Call SetAsset() first.");
            return;
        }

        // Attempt lazy compilation if not yet compiled.
        if (!Instance.Asset.Compiled)
        {
            Instance.Asset.CompileSourceFromFile();
        }

        Instance.Execute();
    }

    public override void Initialize()
    {
        base.Initialize();
        if (Instance == null) return;
        if (!Instance.Asset.Compiled)
        {
            Instance.Asset.CompileSourceFromFile();
        }

        Instance.Execute();
    }

    public override void Update(double deltaTime)
    {
        if (Instance == null)
            return;
        
        Lua lua = Instance.Runtime.GetLuaCore();

        lua.GetGlobal("Script");

        if (lua.IsNil(-1))
        {
            _logger.Output(
                Logger.OutputType.Warning,
                Logger.OutputLevel.Warning,
                "Script table does not exist."
            );

            lua.Pop(1);
            return;
        }

        lua.GetField(-1, "OnUpdate");

        // Remove the Script table, leaving only OnUpdate.
        lua.Remove(-2);

        if (lua.IsNil(-1))
        {
            lua.Pop(1);
            return;
        }

        lua.PushNumber(deltaTime);

        LuaStatus status = lua.PCall(1, 0, 0);

        if (status != LuaStatus.OK)
        {
            string error = lua.ToString(-1);
            lua.Pop(1);

            throw new Exception(
                $"YueScriptComponent OnUpdate failed: {error}"
            );
        }
    }
    /// <summary>
    /// Overrides the base dispose logic to clean up the Lua runtime.
    /// </summary>
    public override void Dispose()
    {
        if (this.externalRuntime == false)
        {
            _runtime?.Dispose();
            _runtime = null;
        }
        base.Dispose();
    }
}
