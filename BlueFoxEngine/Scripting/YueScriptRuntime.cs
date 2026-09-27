using System.Collections.Generic;
using BlueFoxEngine.Logging;
using KeraLua;

namespace BlueFoxEngine.Scripting;

/// <summary>
/// Represents a single script entry stored inside a YueScriptRuntime.
/// Maps one script ID to its per-script Lua table (_G.Scripts[scriptId]).
/// </summary>
public class ScriptEntry
{
    /// <summary>
    /// The unique identifier for this script within the runtime.
    /// </summary>
    public string ScriptId { get; internal set; } = "";
}

public sealed class YueScriptRuntime : IDisposable
{
    private Logger _logger;
    public string? ComponentName { get; private set; }
    public bool IsComponent { get; init; }
    private readonly Lua _lua;
    private bool _disposed;

    /// <summary>
    /// Ordered collection of script IDs registered with this runtime.
    /// Used by YueScriptComponent.Update to iterate and call OnUpdate on each.
    /// </summary>
    internal List<string> RegisteredScripts { get; } = new();

    /// <summary>
    /// A counter used to generate unique script IDs. Incremented monotonically.
    /// </summary>
    private int _scriptIdCounter;

    protected internal Lua GetLuaCore()
    {
        return _lua;
    }

    /// <summary>
    /// Registers a new script in this runtime and initializes its per-script
    /// Lua table (_G.Scripts[scriptId].Script). Returns a ScriptEntry that the
    /// caller must associate with the corresponding YueScriptInstance.
    ///
    /// The Script table is exposed as a local variable named 'Script' so scripts
    /// can simply write:  Script.OnUpdate = (dt) -> print(dt)
    /// </summary>
    internal ScriptEntry RegisterScript()
    {
        if (_disposed)
            throw new ObjectDisposedException(nameof(YueScriptRuntime));

        int nextId = Interlocked.Increment(ref _scriptIdCounter);
        string scriptId = $"s_{nextId:D4}";

        // Create _G.Scripts[scriptId] = { Script = {...} }
        bool hasError = _lua.DoString($@"
            _G.Scripts['{scriptId}'] = {{}}
            _G.Scripts['{scriptId}'].Script = {{}}
            _G.Scripts['{scriptId}'].Script.Parent = _G.Scripts['{scriptId}']
        ");
        if (hasError)
        {
            string error = _lua.ToString(-1);
            _lua.Pop(1);
            _logger.Output(Logger.OutputType.ExceptionThrownError, Logger.OutputLevel.Error,
                $"Failed to create Script table for {scriptId}: {error}");
        }

        RegisteredScripts.Add(scriptId);

        return new ScriptEntry { ScriptId = scriptId };
    }

    /// <summary>
    /// Removes a script from the runtime, cleaning up its Lua table and
    /// removing it from the registered scripts list.
    /// </summary>
    internal void UnregisterScript(ScriptEntry entry)
    {
        if (entry == null) return;

        _lua.DoString($"_G.Scripts['{entry.ScriptId}'] = nil");
        RegisteredScripts.Remove(entry.ScriptId);
    }

    /// <summary>
    /// Returns the ScriptEntry (and its Lua table) for the given script ID.
    /// Returns null if the ID is not registered in this runtime.
    /// </summary>
    internal ScriptEntry? GetScriptEntry(string scriptId)
    {
        return RegisteredScripts.Contains(scriptId) ? new ScriptEntry { ScriptId = scriptId } : null;
    }


    public YueScriptRuntime(string? runtimeName = null, bool isComponent = false)
    {
        _lua = new Lua();

        this.IsComponent = isComponent;

        if (!String.IsNullOrEmpty(runtimeName))
        {
            this.ComponentName = runtimeName;
            this._logger = new Logger(runtimeName);
        }
        else
        {
            this._logger = new Logger("YueScriptRuntime");
        }

        ConfigureLua();
        LoadYueScript();

        LuaFunction printFunction = (IntPtr ptr) =>
        {
            int argumentCount = _lua.GetTop();
            for (int i = 1; i <= argumentCount; i++)
            {
                _logger.Output(Logger.OutputType.Info, Logger.OutputLevel.Info, _lua.ToString(i));
            }

            return 0;
        };

        _lua.PushCFunction(printFunction);
        _lua.SetGlobal("print");
    }

    private void ConfigureLua()
    {
        _lua.DoString("""
                          package.cpath = "./?.so;" .. package.cpath
                      """);
    }

    /// <summary>
    /// Initializes the YueScript compiler module and the per-script Script table registry.
    /// Each script instance gets its own table under _G.Scripts[scriptId].Script
    /// so OnUpdate callbacks do not conflict between scripts sharing the same runtime.
    /// </summary>
    private void LoadYueScript()
    {
        _lua.DoString("""
                          yue = require("yue")
                          _G.Scripts = {}
                      """);
    }

    /// <summary>
    /// Executes compiled Lua source in this runtime's Lua state.
    /// Before execution, the 'Script' local variable is bound to this
    /// instance's per-script table, so scripts can reference it directly.
    ///
    /// The 'Script' local also provides convenient access to the owning
    /// object via Script.Parent.
    /// </summary>
    public void Execute(string source, string? scriptId = null)
    {
        if (_disposed)
            throw new ObjectDisposedException(nameof(YueScriptRuntime));

        _logger.Output(Logger.OutputType.Info, Logger.OutputLevel.Trace, source);

        bool hasError = false;

        if (!string.IsNullOrEmpty(scriptId))
        {
            // Inject a local 'Script' variable scoped to this script instance.
            // The 'do ... end' block ensures Script is not visible outside,
            // and DoString handles arbitrary Lua statements (if/for/while etc.).
            string wrapper = $"local Script = _G.Scripts['{scriptId}'].Script; do {source} end";
            hasError = _lua.DoString(wrapper);
        }
        else
        {
            hasError = _lua.DoString(source);
        }

        if (hasError)
        {
            string error = _lua.ToString(-1);
            _lua.Pop(1);
            _logger.Output(Logger.OutputType.ExceptionThrownError, Logger.OutputLevel.Error,
                $"YueScript execution failed: {error}");
        }
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        _disposed = true;
        _lua.Dispose();
    }
}
