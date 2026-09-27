using BlueFoxEngine.Assets.Scripts;
using BlueFoxEngine.Logging;
using BlueFoxEngine.Scripting;
using KeraLua;

namespace BlueFoxEngine.Components;

/// <summary>
/// Attaches a YueScript to an Object so it is executed automatically.
///
/// The component owns a YueScriptInstance which wraps a YueScriptAsset.
/// When the object initializes, the component attempts to compile the
/// script if it has not been compiled yet, then executes it.
///
/// Each script instance is registered in the runtime under its own
/// _G.Scripts[scriptId].Script namespace, so OnUpdate callbacks do
/// not conflict between scripts sharing the same runtime.
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
    /// Sets the script asset and creates a dedicated runtime for it,
    /// unless one is already provided (external runtime).
    ///
    /// The instance is automatically registered in the runtime under
    /// _G.Scripts[scriptId] so its OnUpdate callback will be called
    /// independently of other scripts sharing the same runtime.
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

    /// <summary>
    /// Called every frame by the engine's game loop.
    ///
    /// Iterates over all scripts registered in the runtime and calls
    /// each one's OnUpdate(deltaTime) independently, so that multiple
    /// scripts sharing the same runtime do not overwrite each other's
    /// Update callbacks.
    /// </summary>
    public override void Update(double deltaTime)
    {
        if (Instance == null)
            return;

        Lua lua = Instance.Runtime.GetLuaCore();

        // Iterate over all registered script entries and call each OnUpdate.
        int registeredCount = Instance.Runtime.RegisteredScripts.Count;
        for (int i = 0; i < registeredCount; i++)
        {
            string scriptId = Instance.Runtime.RegisteredScripts[i];
            CallScriptOnUpdate(lua, scriptId, deltaTime);
        }
    }

    /// <summary>
    /// Finds _G.Scripts[scriptId].Script.OnUpdate and calls it with the
    /// given deltaTime. The Lua stack is kept clean — all pushed values
    /// are popped before this method returns.
    /// </summary>
    private void CallScriptOnUpdate(Lua lua, string scriptId, double deltaTime)
    {
        try
        {
            // Navigate the stack to find OnUpdate:
            //   stack[0] = _G, stack[1] = Scripts, stack[2] = Scripts[scriptId],
            //   stack[3] = Scripts[scriptId].Script, stack[4] = Scripts[scriptId].Script.OnUpdate
            int top = lua.GetTop();

            lua.GetGlobal("_G");                     // 0: _G
            lua.GetField(-1, "Scripts");              // 1: Scripts (top=1)
            lua.Remove(-2);                           // 0: Scripts

            lua.GetField(-1, scriptId);               // 1: Scripts[scriptId] (top=1)
            lua.Remove(-2);                           // 0: Scripts[scriptId]

            lua.GetField(-1, "Script");               // 1: Script (top=1)
            lua.Remove(-2);                           // 0: Script

            lua.GetField(-1, "OnUpdate");             // 1: OnUpdate (top=1)

            // If no OnUpdate for this script, skip.
            if (lua.IsNil(-1))
            {
                // Clean up: remove OnUpdate, Script, Scripts[scriptId], Scripts
                lua.Pop(4);
                return;
            }

            // Push deltaTime argument (top=2, [1]=OnUpdate, [2]=dt).
            lua.PushNumber(deltaTime);

            // Call OnUpdate(1 arg, 0 results), leaving 0 results on stack (top=1).
            LuaStatus status = lua.PCall(1, 0, 0);

            if (status != LuaStatus.OK)
            {
                string error = lua.ToString(-1);
                lua.Pop(1);  // pop error message
                _logger.Output(Logger.OutputType.ExceptionThrownError, Logger.OutputLevel.Error,
                    $"YueScript OnUpdate failed for script '{scriptId}': {error}");
            }

            // Pop remaining OnUpdate from stack (PCall with 0 results still leaves it).
            lua.Pop(1);

            // Validate the stack was cleaned up (debug guard).
            if (lua.GetTop() != top)
            {
                _logger.Output(Logger.OutputType.Warning, Logger.OutputLevel.Warning,
                    $"Lua stack corruption in CallScriptOnUpdate for '{scriptId}': " +
                    $"expected {top}, got {lua.GetTop()}");
                lua.SetTop(top);  // force clean
            }
        }
        catch (Exception ex)
        {
            _logger.Output(Logger.OutputType.ExceptionThrownError, Logger.OutputLevel.Error,
                $"YueScript OnUpdate crashed for script '{scriptId}': {ex.Message}");
        }
    }

    /// <summary>
    /// Overrides the base dispose logic to clean up the Lua runtime
    /// and unregister this instance's Script table from the runtime.
    /// </summary>
    public override void Dispose()
    {
        if (Instance != null)
        {
            // Unregister this script from the runtime's list so it
            // is no longer iterated in Update loops.
            if (Instance.Runtime != null)
            {
                Instance.Runtime.UnregisterScript(Instance.ScriptEntry);
            }

            Instance = null;
        }

        if (this.externalRuntime == false)
        {
            _runtime?.Dispose();
            _runtime = null;
        }
        base.Dispose();
    }
}
