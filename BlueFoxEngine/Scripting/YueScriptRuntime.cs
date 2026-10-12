using System.Collections.Generic;
using BlueFoxEngine.Logging;
using KeraLua;
using Object = global::BlueFoxEngine.Assets.Object;

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
            _G.Scripts['{scriptId}'].Script.Owner = {{}}
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
    /// Attaches a C# Object to a script by populating Script.Owner with the object's handle
    /// and setting up methods on the Lua Owner table for engine interaction.
    /// </summary>
    internal void AttachObject(string scriptId, Object owner)
    {
        if (_disposed)
            throw new ObjectDisposedException(nameof(YueScriptRuntime));

        int handle = YueScriptBridge.RegisterObject(owner);

        // Set up Script.Owner as a Lua table with methods
        _lua.DoString($@"
            local owner = _G.Scripts['{scriptId}'].Script.Owner
            -- Store the C# object handle
            owner._handle = {handle}
            -- Set a metatable so owner methods (e.g., owner:GetPosition()) resolve via __index
            local mt = {{}}

            -- Helper: createObject(type) -> Owner (returns Owner with _handle)
            mt.createObject = function(self, typeName)
                local obj = engine.createObject(typeName)
                -- Wrap the returned object in a meta-table with convenience methods
                local newObj = {{ _handle = obj }}
                setmetatable(newObj, mt)
                return newObj
            end

            -- Helper: destroy() on the Owner itself
            mt.destroy = function(self)
                engine.destroyObject(self._handle)
            end

            -- Helper: setPosition(x, y)  (convenience for Object2D)
            mt.setPosition = function(self, x, y)
                engine.setProperty(self._handle, ""Position"", {{x = x, y = y}})
            end

            -- Helper: getPosition() -> x, y
            mt.getPosition = function(self)
                local v = engine.getProperty(self._handle, ""Position"")
                return v.x, v.y
            end

            -- Helper: setScale(x, y)
            mt.setScale = function(self, x, y)
                engine.setProperty(self._handle, ""Scale"", {{x = x, y = y}})
            end

            -- Helper: getScale() -> x, y
            mt.getScale = function(self)
                local v = engine.getProperty(self._handle, ""Scale"")
                return v.x, v.y
            end

            -- Helper: setRotation(radians)
            mt.setRotation = function(self, r)
                engine.setProperty(self._handle, ""Rotation"", r)
            end

            -- Helper: getRotation() -> radians
            mt.getRotation = function(self)
                return engine.getProperty(self._handle, ""Rotation"")
            end

            -- Helper: setProperty(name, value)
            mt.setProperty = function(self, name, value)
                engine.setProperty(self._handle, name, value)
            end

            -- Helper: getProperty(name) -> value
            mt.getProperty = function(self, name)
                return engine.getProperty(self._handle, name)
            end

            -- Helper: addComponent(componentType)
            mt.addComponent = function(self, componentType)
                return engine.addComponent(self._handle, componentType)
            end

            -- Helper: getComponent(componentType)
            mt.getComponent = function(self, componentType)
                return engine.getComponent(self._handle, componentType)
            end

            -- Helper: clone()
            mt.clone = function(self)
                local cloned = engine.cloneObject(self._handle)
                local newObj = {{ _handle = cloned }}
                setmetatable(newObj, mt)
                return newObj
            end

            mt.__index = mt
            setmetatable(owner, mt)

            setmetatable(owner, mt)
        ");
        Console.WriteLine("hasError Check HERE!");
        bool hasError = _lua.DoString($@"
            local owner = _G.Scripts['{scriptId}'].Script.Owner
        
            assert(owner ~= nil, 'Script.Owner is nil')
            assert(getmetatable(owner) ~= nil, 'Owner metatable is missing')
            assert(type(owner.createObject) == 'function',
                'Owner.createObject is not a function')
        ");
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
        // Register the engine API bridge (exposes 'engine' global table)
        YueScriptBridge.RegisterBridgeFunctions(_lua);
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

        if (string.IsNullOrWhiteSpace(source) ||
            string.Equals(source.Trim(), "nil", StringComparison.Ordinal))
        {
            _logger.Output(
                Logger.OutputType.ExceptionThrownError,
                Logger.OutputLevel.Error,
                $"Execute received invalid source. ScriptId: {scriptId ?? "<none>"}, " +
                $"Source: '{source ?? "<null>"}'");

            return;
        }
        
        _logger.Output(Logger.OutputType.Info, Logger.OutputLevel.Critical, source);

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
