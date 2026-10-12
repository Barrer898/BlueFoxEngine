using BlueFoxEngine.Assets.Scripts;
using BlueFoxEngine.Logging;
using BlueFoxEngine.Scripting;
using KeraLua;

namespace BlueFoxEngine.Components;

/// <summary>
///     Attaches a YueScript to an Object so it is executed automatically.
///     The component owns a YueScriptInstance which wraps a YueScriptAsset.
///     When the object initializes, the component attempts to compile the
///     script if it has not been compiled yet, then executes it.
///     Each script instance is registered in the runtime under its own
///     _G.Scripts[scriptId].Script namespace, so OnUpdate callbacks do
///     not conflict between scripts sharing the same runtime.
///     If the component is added to an Object that is disposed, the
///     associated script asset is also disposed.
/// </summary>
public class YueScriptComponent : CSharpComponent
{
    private readonly Logger _logger;

    private YueScriptRuntime? _runtime;
    private bool externalRuntime;


    /// <summary>
    ///     Creates an empty component. Call SetAsset before adding to an Object.
    /// </summary>
    public YueScriptComponent(string? componentName = null)
    {
        if (!string.IsNullOrEmpty(componentName))
        {
            ComponentName = componentName;
            _logger = new Logger(componentName);
        }
        else
        {
            _logger = new Logger("UnNamedComponent");
        }

        externalRuntime = false;
    }

    /// <summary>
    ///     Creates a component attached to the given script asset with its own YueScriptRuntime.
    /// </summary>
    public YueScriptComponent(YueScriptAsset asset, string? componentName = null)
    {
        if (!string.IsNullOrEmpty(componentName))
        {
            ComponentName = componentName;
            _logger = new Logger(componentName);
        }
        else
        {
            _logger = new Logger("UnNamedComponent");
        }

        externalRuntime = false;
        SetAsset(asset);
    }

    public YueScriptComponent(YueScriptAsset asset, YueScriptRuntime runtime, string? componentName = null)
    {
        if (!string.IsNullOrEmpty(componentName))
        {
            ComponentName = componentName;
            _logger = new Logger(componentName);
        }
        else
        {
            _logger = new Logger("UnnamedYueComponent");
        }

        _runtime = runtime;
        externalRuntime = true;
        SetAsset(asset);
    }

    public string ComponentName { get; } = "";

    /// <summary>
    ///     The script instance this component owns and executes.
    ///     Null until SetAsset is called or the asset is resolved.
    /// </summary>
    public YueScriptInstance? Instance { get; private set; }

    /// <summary>
    ///     The underlying script asset. May be null if SetAsset was not called.
    /// </summary>
    public YueScriptAsset? Asset => Instance?.Asset;

    /// <summary>
    ///     Sets the script asset and creates a dedicated runtime for it,
    ///     unless one is already provided (external runtime).
    ///     The instance is automatically registered in the runtime under
    ///     _G.Scripts[scriptId] so its OnUpdate callback will be called
    ///     independently of other scripts sharing the same runtime.
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
                ComponentName,
                true
            );

            externalRuntime = false;
        }

        Instance = new YueScriptInstance(asset, _runtime);
    }

    private bool EnsureCompiled()
    {
        if (Instance == null)
            return false;

        if (!Instance.Asset.Compiled)
            Instance.Asset.CompileSourceFromFile();

        if (!Instance.Asset.Compiled ||
            string.IsNullOrWhiteSpace(Instance.Asset.CompiledLua))
        {
            _logger.Output(
                Logger.OutputType.ExceptionThrownError,
                Logger.OutputLevel.Error,
                $"Cannot execute YueScript: compilation failed or produced no output. " +
                $"Source file: {Instance.Asset.RelativeTargetSourceFile ?? "<unset>"}"
            );
            Console.Write("EnsureCompile return false");
            return false;
        }
        Console.Write("EnsureCompile return true");
        return true;
    }
    
    /// <summary>
    ///     Executes the assigned script once.
    ///     If the asset has not been compiled yet, this method attempts
    ///     compilation automatically.
    /// </summary>
    public void Execute()
    {
        if (Instance == null)
        {
            _logger.Output(
                Logger.OutputType.Warning,
                Logger.OutputLevel.Warning,
                "No script assigned to YueScriptComponent. Call SetAsset() first."
            );

            return;
        }

        if (!EnsureCompiled())
            return;

        Instance.Execute();
    }
    public override void Initialize()
    {
        base.Initialize();

        if (Instance == null || !EnsureCompiled())
            return;

        if (base.Owner != null)
        {
            Instance.SetOwner(base.Owner);

            Instance.Runtime.AttachObject(
                Instance.ScriptEntry.ScriptId,
                base.Owner
            );
        }

        Instance.Execute();

        CallScriptOnInitialize(
            Instance.Runtime.GetLuaCore(),
            Instance.ScriptEntry.ScriptId
        );
    }

    /// <summary>
    ///     Called every frame by the engine's game loop.
    ///     Iterates over all scripts registered in the runtime and calls
    ///     each one's OnUpdate(deltaTime) independently, so that multiple
    ///     scripts sharing the same runtime do not overwrite each other's
    ///     Update callbacks.
    /// </summary>
    public override void Update(double deltaTime)
    {
        if (Instance == null)
            return;

        var lua = Instance.Runtime.GetLuaCore();

        CallScriptOnUpdate(lua, Instance.ScriptEntry.ScriptId, deltaTime);
    }

    /// <summary>
    ///     Finds _G.Scripts[scriptId].Script.OnUpdate and calls it with the
    ///     given deltaTime. The Lua stack is kept clean — all pushed values
    ///     are popped before this method returns.
    /// </summary>
    private void CallScriptOnUpdate(Lua lua, string scriptId, double deltaTime)
    {
        // Remember the stack state when we enter.
        // Everything we push must be cleaned up before returning.
        var top = lua.GetTop();

        try
        {
            // Get the global Scripts table.
            //
            // Stack:
            // [Scripts]
            lua.GetGlobal("Scripts");

            // Get this script's entry.
            //
            // Stack:
            // [Scripts]
            // [Scripts[scriptId]]
            lua.GetField(-1, scriptId);

            // We no longer need the Scripts table.
            //
            // Stack:
            // [Scripts[scriptId]]
            lua.Remove(-2);

            // Get this script's Script table.
            //
            // Stack:
            // [Scripts[scriptId].Script]
            lua.GetField(-1, "Script");

            // We no longer need the script entry.
            //
            // Stack:
            // [Script]
            lua.Remove(-2);

            // Get the OnUpdate function.
            //
            // Stack:
            // [OnUpdate]
            lua.GetField(-1, "OnUpdate");

            // Remove the Script table.
            //
            // Stack:
            // [OnUpdate]
            lua.Remove(-2);

            // The script does not have an OnUpdate function.
            if (lua.IsNil(-1))
            {
                // Remove the nil value.
                //
                // Stack:
                // []
                lua.Pop(1);
                return;
            }

            // Push deltaTime as the argument to OnUpdate.
            //
            // Stack:
            // [OnUpdate]
            // [deltaTime]
            lua.PushNumber(deltaTime);

            // Call:
            //
            // OnUpdate(deltaTime)
            //
            // 1 argument
            // 0 return values
            //
            // The function and argument are consumed by the call.
            //
            // Stack afterward:
            // []
            var status = lua.PCall(1, 0, 0);

            if (status != LuaStatus.OK)
            {
                // On failure, Lua leaves the error object on the stack.
                //
                // Stack:
                // [error]
                var error = lua.ToString(-1);

                lua.Pop(1);

                _logger.Output(
                    Logger.OutputType.ExceptionThrownError,
                    Logger.OutputLevel.Error,
                    $"YueScript OnUpdate failed for script '{scriptId}': {error}"
                );
            }
        }
        catch (Exception ex)
        {
            _logger.Output(
                Logger.OutputType.ExceptionThrownError,
                Logger.OutputLevel.Error,
                $"YueScript OnUpdate crashed for script '{scriptId}': {ex.Message}"
            );
        }
        finally
        {
            // Nuclear safety net:
            // restore the exact stack state we had when entering.
            if (lua.GetTop() != top)
            {
                _logger.Output(
                    Logger.OutputType.Warning,
                    Logger.OutputLevel.Warning,
                    $"Lua stack imbalance in CallScriptOnUpdate for '{scriptId}'. " +
                    $"Expected {top}, got {lua.GetTop()}."
                );

                lua.SetTop(top);
            }
        }
    }

    private void CallScriptOnInitialize(Lua lua, string scriptId)
    {
        var top = lua.GetTop();

        try
        {
            lua.GetGlobal("Scripts");

            lua.GetField(-1, scriptId);

            lua.Remove(-2);

            lua.GetField(-1, "Script");

            lua.Remove(-2);

            lua.GetField(-1, "OnInitialize");

            lua.Remove(-2);
            
            if (lua.IsNil(-1))
            {
                lua.Pop(1);
                return;
            }

            var status = lua.PCall(0, 0, 0);

            if (status != LuaStatus.OK)
            {
                var error = lua.ToString(-1);

                lua.Pop(1);

                _logger.Output(
                    Logger.OutputType.ExceptionThrownError,
                    Logger.OutputLevel.Error,
                    $"YueScript OnInitialize failed for script '{scriptId}': {error}"
                );
            }
        }
        catch (Exception ex)
        {
            _logger.Output(
                Logger.OutputType.ExceptionThrownError,
                Logger.OutputLevel.Error,
                $"YueScript OnInitialize crashed for script '{scriptId}': {ex.Message}"
            );
        }
        finally
        {
            if (lua.GetTop() != top)
            {
                _logger.Output(
                    Logger.OutputType.Warning,
                    Logger.OutputLevel.Warning,
                    $"Lua stack imbalance in CallScriptOnUpdate for '{scriptId}'. " +
                    $"Expected {top}, got {lua.GetTop()}."
                );

                lua.SetTop(top);
            }
        }
    }

    /// <summary>
    ///     Overrides the base dispose logic to clean up the Lua runtime
    ///     and unregister this instance's Script table from the runtime.
    /// </summary>
    public override void Dispose()
    {
        if (Instance != null)
        {
            // Unregister this script from the runtime's list so it
            // is no longer iterated in Update loops.
            if (Instance.Runtime != null) Instance.Runtime.UnregisterScript(Instance.ScriptEntry);

            Instance = null;
        }

        if (!externalRuntime)
        {
            _runtime?.Dispose();
            _runtime = null;
        }

        base.Dispose();
    }
}