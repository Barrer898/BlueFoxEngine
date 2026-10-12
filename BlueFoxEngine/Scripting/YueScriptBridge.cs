using System.Numerics;
using System.Reflection;
using BlueFoxEngine.Assets;
using BlueFoxEngine.Assets.Sprite;
using BlueFoxEngine.Components;
using BlueFoxEngine.Logging;
using BlueFoxEngine.Scenes;
using KeraLua;
using Raylib_cs;
using Object = global::BlueFoxEngine.Assets.Object;

namespace BlueFoxEngine.Scripting;

/// <summary>
/// Provides engine functionality to YueScript scripts.
/// Bridge methods are exposed as Lua functions attached to the Script.Owner table.
/// </summary>
public static class YueScriptBridge
{
    // Mapping from numeric Lua identifiers back to C# Object instances.
    // We use int handles as Lua-side references.
    private static readonly Dictionary<int, WeakReference<Object>> _objectRegistry = new();
    // Separate registry for non-Object items (e.g., components)
    private static readonly Dictionary<int, WeakReference<object>> _genericRegistry = new();
    // Handles in generic registry start at 100000 to avoid collisions
    private const int GenericRegistryBase = 100000;
    
    private static readonly object _registryLock = new();
    private static int _nextObjectHandle = 1;
    private static int _nextGenericHandle = GenericRegistryBase;

    private static readonly Logger _logger = new Logger("YueScriptBridge");

    /// <summary>
    /// Creates a unique numeric handle for a C# Object that Lua can use as an identifier.
    /// Returns an int that Lua scripts can pass back to other bridge functions.
    /// </summary>
    public static int RegisterObject(Object obj)
    {
        int handle;
        lock (_registryLock)
        {
            handle = _nextObjectHandle++;
            _objectRegistry[handle] = new WeakReference<Object>(obj);
        }
        return handle;
    }

    /// <summary>
    /// Registers a non-Object item (e.g., component) and returns a handle.
    /// </summary>
    public static int RegisterGeneric(object item)
    {
        int handle;
        lock (_registryLock)
        {
            handle = _nextGenericHandle++;
            _genericRegistry[handle] = new WeakReference<object>(item);
        }
        return handle;
    }

    /// <summary>
    /// Looks up a C# Object by its numeric handle.
    /// Returns null if the handle is invalid or the object has been GC'd.
    /// </summary>
    public static Object? ResolveObject(int handle)
    {
        lock (_registryLock)
        {
            if (_objectRegistry.TryGetValue(handle, out var weak))
            {
                if (weak.TryGetTarget(out var obj))
                    return obj;
                // Object has been collected — clean up the stale entry.
                _objectRegistry.Remove(handle);
            }
        }
        return null;
    }

    /// <summary>
    /// Looks up a generic item by its handle. Used for components and other non-Object items.
    /// </summary>
    public static object? ResolveGeneric(int handle)
    {
        lock (_registryLock)
        {
            if (_genericRegistry.TryGetValue(handle, out var weak))
            {
                if (weak.TryGetTarget(out var item))
                    return item;
                _genericRegistry.Remove(handle);
            }
        }
        return null;
    }

    /// <summary>
    /// Tries to resolve an item as either an Object or a generic item. Returns the object or null.
    /// </summary>
    public static object? ResolveAny(int handle)
    {
        var obj = ResolveObject(handle);
        if (obj != null) return obj;
        return ResolveGeneric(handle);
    }

    /// <summary>
    /// Called when an Object is disposed — cleans up its registry entry.
    /// </summary>
    public static void UnregisterObject(Object obj)
    {
        lock (_registryLock)
        {
            var keysToRemove = _objectRegistry
                .Where(kvp => kvp.Value.TryGetTarget(out var target) && ReferenceEquals(target, obj))
                .Select(kvp => kvp.Key)
                .ToList();
            foreach (var key in keysToRemove)
                _objectRegistry.Remove(key);
        }
    }

    /// <summary>
    /// Tries to read a Vector2 from a Lua table at the given stack index.
    /// Expects {x = ..., y = ...} or {1 = ..., 2 = ...}
    /// </summary>
    public static bool TryReadVector2(Lua lua, int index, out Vector2 result)
    {
        result = Vector2.Zero;
        bool hasX = false, hasY = false;

        // Try table field access first (x/y keys)
        lua.PushString("x");
        lua.GetTable(index);
        if (!lua.IsNil(-1))
        {
            result.X = (float)lua.ToNumber(-1);
            hasX = true;
        }
        lua.Pop(1);

        lua.PushString("y");
        lua.GetTable(index);
        if (!lua.IsNil(-1))
        {
            result.Y = (float)lua.ToNumber(-1);
            hasY = true;
        }
        lua.Pop(1);

        // Fallback: try array-style {x, y}
        if (!hasX)
        {
            lua.PushNumber(1);
            lua.GetTable(index);
            if (!lua.IsNil(-1))
            {
                result.X = (float)lua.ToNumber(-1);
                hasX = true;
            }
            lua.Pop(1);
        }
        if (!hasY)
        {
            lua.PushNumber(2);
            lua.GetTable(index);
            if (!lua.IsNil(-1))
            {
                result.Y = (float)lua.ToNumber(-1);
                hasY = true;
            }
            lua.Pop(1);
        }

        return hasX && hasY;
    }

    /// <summary>
    /// Pushes a Lua table representing a Vector2: {x = ..., y = ...}
    /// </summary>
    public static void PushVector2(Lua lua, Vector2 v)
    {
        lua.NewTable();
        lua.PushNumber(v.X);
        lua.SetField(-1, "x");
        lua.PushNumber(v.Y);
        lua.SetField(-1, "y");
    }

    /// <summary>
    /// Tries to read a Color from a Lua table at the given stack index.
    /// Expects {r = ..., g = ..., b = ..., a = ...} with values 0-255 or 0-1.
    /// </summary>
    public static bool TryReadColor(Lua lua, int index, out Color result)
    {
        byte r = 255, g = 255, b = 255, a = 255;
        bool hasR = false, hasG = false, hasB = false, hasA = false;

        foreach (string key in new[] { "r", "g", "b", "a" })
        {
            lua.PushString(key);
            lua.GetTable(index);
            if (!lua.IsNil(-1))
            {
                float val = (float)lua.ToNumber(-1);
                // Auto-detect scale: if any value > 1, assume 0-255 range
                if (val > 255f) val = 1f;
                else if (val > 1f) val /= 255f;

                switch (key)
                {
                    case "r": r = (byte)MathF.Round(val * 255f); hasR = true; break;
                    case "g": g = (byte)MathF.Round(val * 255f); hasG = true; break;
                    case "b": b = (byte)MathF.Round(val * 255f); hasB = true; break;
                    case "a": a = (byte)MathF.Round(val * 255f); hasA = true; break;
                }
            }
            lua.Pop(1);
        }

        if (hasR || hasG || hasB || hasA)
        {
            result = new Color(r, g, b, a);
            return true;
        }
        result = Color.White;
        return false;
    }

    /// <summary>
    /// Pushes a Lua table representing a Color: {r=255, g=255, b=255, a=255}
    /// Values are in 0-255 range.
    /// </summary>
    public static void PushColor(Lua lua, Color c)
    {
        lua.NewTable();
        lua.PushNumber(c.R);
        lua.SetField(-1, "r");
        lua.PushNumber(c.G);
        lua.SetField(-1, "g");
        lua.PushNumber(c.B);
        lua.SetField(-1, "b");
        lua.PushNumber(c.A);
        lua.SetField(-1, "a");
    }

    /// <summary>
    /// Tries to read a Rectangle from a Lua table at the given stack index.
    /// Expects {x = ..., y = ..., w = ..., h = ...}.
    /// </summary>
    public static bool TryReadRectangle(Lua lua, int index, out Rectangle result)
    {
        result = new Rectangle();
        bool hasX = false, hasY = false, hasW = false, hasH = false;

        foreach (string key in new[] { "x", "y", "w", "h" })
        {
            lua.PushString(key);
            lua.GetTable(index);
            if (!lua.IsNil(-1))
            {
                int val = (int)lua.ToNumber(-1);
                switch (key)
                {
                    case "x": result.X = val; hasX = true; break;
                    case "y": result.Y = val; hasY = true; break;
                    case "w": result.Width = val; hasW = true; break;
                    case "h": result.Height = val; hasH = true; break;
                }
            }
            lua.Pop(1);
        }
        return hasX && hasY && hasW && hasH;
    }

    /// <summary>
    /// Pushes a Lua table representing a Rectangle: {x=0, y=0, w=0, h=0}
    /// </summary>
    public static void PushRectangle(Lua lua, Rectangle rect)
    {
        lua.NewTable();
        lua.PushNumber(rect.X);
        lua.SetField(-1, "x");
        lua.PushNumber(rect.Y);
        lua.SetField(-1, "y");
        lua.PushNumber(rect.Width);
        lua.SetField(-1, "w");
        lua.PushNumber(rect.Height);
        lua.SetField(-1, "h");
    }

    // =========================================================================
    // Lua-callable bridge functions
    // =========================================================================

    /// <summary>
    /// createObject(typeName, ...) — Creates a new engine Object (or subclass).
    ///   typeName: "Object", "Object2D", "Sprite"
    ///   For Sprite: additional arg is texture reference or TextureAsset.
    /// Returns the Object reference (C# object, usable in subsequent calls).
    /// </summary>
    internal static int CreateObject(Lua lua)
    {
        string typeName = lua.ToString(1)?.ToLowerInvariant();
        lua.Pop(1);

        Object obj = typeName switch
        {
            "object2d" => new Object2D(),
            "sprite" => new Sprite(null!), // Texture null; assign later via SetProperty
            _ => new Object()
        };

        // Register in our Lua→C# map and push the C# object back to Lua
        int handle = RegisterObject(obj);
        lua.PushNumber(handle);
        return 1;
    }

    /// <summary>
    /// destroyObject(objRef) — Disposes an Object and removes it from the scene.
    ///   objRef: the C# object returned by createObject (or Script.Owner handle).
    /// </summary>
    internal static int DestroyObject(Lua lua)
    {
        int handle = (int)lua.ToNumber(1);
        lua.Pop(1);

        var obj = ResolveObject(handle);
        if (obj != null)
        {
            obj.Dispose();
            UnregisterObject(obj);
        }
        else
        {
            _logger.Output(Logger.OutputType.Warning, Logger.OutputLevel.Warning,
                $"destroyObject: no object found for handle {handle}");
        }
        return 0;
    }

    /// <summary>
    /// getProperty(objRef, propertyName) — Gets a property value from an Object.
    /// Returns the value (may be a primitive, Vector2 table, or object handle).
    /// </summary>
    internal static int GetProperty(Lua lua)
    {
        int handle = (int)lua.ToNumber(1);
        lua.Pop(1);
        string propName = lua.ToString(1);
        lua.Pop(1);

        var obj = ResolveObject(handle);
        if (obj == null)
        {
            lua.PushNil();
            return 1;
        }

        try
        {
            // Try via reflection on the concrete type
            var prop = obj.GetType().GetProperty(propName, BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);
            if (prop != null)
            {
                var value = prop.GetValue(obj);
                PushLuaValue(lua, value);
                return 1;
            }

            // Try fields (e.g., SourceRectangle on Sprite)
            var field = obj.GetType().GetField(propName, BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);
            if (field != null)
            {
                var value = field.GetValue(obj);
                PushLuaValue(lua, value);
                return 1;
            }

            _logger.Output(Logger.OutputType.Warning, Logger.OutputLevel.Warning,
                $"getProperty: property '{propName}' not found on {obj.GetType().Name}");
            lua.PushNil();
        }
        catch (Exception ex)
        {
            _logger.Output(Logger.OutputType.ExceptionThrownError, Logger.OutputLevel.Error,
                $"getProperty failed for '{propName}' on {obj.GetType().Name}: {ex.Message}");
            lua.PushNil();
        }
        return 1;
    }

    /// <summary>
    /// setProperty(objRef, propertyName, value) — Sets a property on an Object.
    /// value can be a primitive, a Vector2 table {x=..., y=...}, a Color table, or a Rectangle table.
    /// </summary>
    internal static int SetProperty(Lua lua)
    {
        int handle = (int)lua.ToNumber(1);
        lua.Pop(1);
        string propName = lua.ToString(1);
        lua.Pop(1);

        var obj = ResolveObject(handle);
        if (obj == null)
        {
            return 0;
        }

        // Check if value arg is a table (Vector2, Color, Rectangle) or primitive
        int valueIndex = 3;
        try
        {
            // Try via reflection
            var prop = obj.GetType().GetProperty(propName, BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);
            if (prop != null && prop.CanWrite)
            {
                object value = ParseLuaValue(lua, valueIndex, prop.PropertyType);
                prop.SetValue(obj, value);
                return 0;
            }

            // Try fields
            var field = obj.GetType().GetField(propName, BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);
            if (field != null)
            {
                object value = ParseLuaValue(lua, valueIndex, field.FieldType);
                field.SetValue(obj, value);
                return 0;
            }

            _logger.Output(Logger.OutputType.Warning, Logger.OutputLevel.Warning,
                $"setProperty: property '{propName}' not found on {obj.GetType().Name}");
        }
        catch (Exception ex)
        {
            _logger.Output(Logger.OutputType.ExceptionThrownError, Logger.OutputLevel.Error,
                $"setProperty failed for '{propName}' on {obj.GetType().Name}: {ex.Message}");
        }
        return 0;
    }

    /// <summary>
    /// getComponent(objRef, componentType) — Gets a component of the specified type from an Object.
    /// Returns the component's handle, or nil if not found.
    /// </summary>
    internal static int GetComponent(Lua lua)
    {
        int handle = (int)lua.ToNumber(1);
        lua.Pop(1);
        string componentType = lua.ToString(1);
        lua.Pop(1);

        var obj = ResolveObject(handle);
        if (obj == null)
        {
            lua.PushNil();
            return 1;
        }

        try
        {
            Type t = Type.GetType(componentType);
            if (t == null)
            {
                _logger.Output(Logger.OutputType.Warning, Logger.OutputLevel.Warning,
                    $"getComponent: unknown component type '{componentType}'");
                lua.PushNil();
                return 1;
            }

            var method = typeof(Object).GetMethod("GetComponent")!.MakeGenericMethod(t);
            var result = method.Invoke(obj, null);

            if (result != null)
            {
                // Register the component in the generic registry
                int componentHandle = RegisterGeneric(result);
                lua.PushNumber(componentHandle);
            }
            else
            {
                lua.PushNil();
            }
        }
        catch (Exception ex)
        {
            _logger.Output(Logger.OutputType.ExceptionThrownError, Logger.OutputLevel.Error,
                $"getComponent failed for '{componentType}': {ex.Message}");
            lua.PushNil();
        }
        return 1;
    }

    /// <summary>
    /// addComponent(objRef, componentType, ...) — Adds a component of the specified type to an Object.
    /// Returns the component's handle.
    /// </summary>
    internal static int AddComponent(Lua lua)
    {
        int handle = (int)lua.ToNumber(1);
        lua.Pop(1);
        string componentType = lua.ToString(1);
        lua.Pop(1);

        var obj = ResolveObject(handle);
        if (obj == null)
        {
            return 0;
        }

        try
        {
            Type t = Type.GetType(componentType);
            if (t == null)
            {
                _logger.Output(Logger.OutputType.Warning, Logger.OutputLevel.Warning,
                    $"addComponent: unknown component type '{componentType}'");
                return 0;
            }

            var method = typeof(Object).GetMethod("AddComponent")!.MakeGenericMethod(t);
            var result = method.Invoke(obj, null);

            if (result != null)
            {
                // Register the component in the generic registry
                int componentHandle = RegisterGeneric(result);
                lua.PushNumber(componentHandle);
            }
            else
            {
                lua.PushNil();
            }
        }
        catch (Exception ex)
        {
            _logger.Output(Logger.OutputType.ExceptionThrownError, Logger.OutputLevel.Error,
                $"addComponent failed for '{componentType}': {ex.Message}");
            lua.PushNil();
        }
        return 1;
    }

    /// <summary>
    /// removeObjectFromScene(objRef) — Convenience function to remove an object from the scene.
    /// </summary>
    internal static int RemoveObjectFromScene(Lua lua)
    {
        int handle = (int)lua.ToNumber(1);
        lua.Pop(1);

        var obj = ResolveObject(handle);
        if (obj != null)
        {
            SceneManager.DisposeObjectAndRemoveFromSceneObjectList(obj);
            UnregisterObject(obj);
        }
        return 0;
    }

    /// <summary>
    /// cloneObject(objRef) — Creates a shallow copy of an Object's properties into a new Object.
    /// Returns the new Object's handle.
    /// </summary>
    internal static int CloneObject(Lua lua)
    {
        int handle = (int)lua.ToNumber(1);
        lua.Pop(1);

        var obj = ResolveObject(handle);
        if (obj == null)
        {
            lua.PushNil();
            return 1;
        }

        // Clone: create a new Object2D if the source is Object2D, otherwise plain Object
        Object clone;
        if (obj is Object2D src2d)
        {
            clone = new Object2D(src2d.Position, src2d.Scale, src2d.Rotation, src2d.Origin);
        }
        else
        {
            clone = new Object();
        }

        int handleNew = RegisterObject(clone);
        lua.PushNumber(handleNew);
        return 1;
    }

    // =========================================================================
    // Helper: convert C# values to Lua values
    // =========================================================================

    private static void PushLuaValue(Lua lua, object? value)
    {
        if (value == null)
        {
            lua.PushNil();
            return;
        }

        if (value is Vector2 v2)
        {
            PushVector2(lua, v2);
            return;
        }

        if (value is Color c)
        {
            PushColor(lua, c);
            return;
        }

        if (value is Rectangle rect)
        {
            PushRectangle(lua, rect);
            return;
        }

        if (value is float f)
        {
            lua.PushNumber(f);
            return;
        }

        if (value is int i)
        {
            lua.PushNumber(i);
            return;
        }

        if (value is double d)
        {
            lua.PushNumber(d);
            return;
        }

        if (value is string s)
        {
            lua.PushString(s);
            return;
        }

        // For anything else, push as string representation
        lua.PushString(value.ToString() ?? "");
    }

    private static object ParseLuaValue(Lua lua, int index, Type targetType)
    {
        if (targetType == typeof(Vector2))
        {
            if (TryReadVector2(lua, index, out var v))
                return v;
            throw new ArgumentException($"Cannot convert Lua value to Vector2");
        }

        if (targetType == typeof(float) || targetType == typeof(double) || targetType == typeof(int))
        {
            return Convert.ChangeType(lua.ToNumber(index), targetType);
        }

        if (targetType == typeof(string))
        {
            return lua.ToString(index);
        }

        if (targetType == typeof(Color))
        {
            if (TryReadColor(lua, index, out var c))
                return c;
            throw new ArgumentException($"Cannot convert Lua value to Color");
        }

        if (targetType == typeof(Rectangle))
        {
            if (TryReadRectangle(lua, index, out var r))
                return r;
            throw new ArgumentException($"Cannot convert Lua value to Rectangle");
        }

        if (targetType == typeof(bool))
        {
            return lua.ToBoolean(index);
        }

        // Default: try to convert from number
        return Convert.ChangeType(lua.ToNumber(index), targetType);
    }

    /// <summary>
    /// Initializes the bridge by attaching all engine API functions to a Lua table.
    /// The returned table can be assigned to _G.YueScriptEngine or similar.
    /// </summary>
    internal static void RegisterBridgeFunctions(Lua lua)
    {
        Console.WriteLine("2-1");
        lua.NewTable();
        Console.WriteLine("2-2");
        // Top-level engine functions — create LuaFunction delegates first
        Console.WriteLine("2-3"); // CRASHES HERE 
        LuaFunction fnCreateObject = (IntPtr ptr) => CreateObject(lua);
        lua.PushCFunction(fnCreateObject);
        lua.SetField(-1, "createObject");
        Console.WriteLine("2-4");
        LuaFunction fnDestroyObject = (IntPtr ptr) => DestroyObject(lua);
        lua.PushCFunction(fnDestroyObject);
        lua.SetField(-1, "destroyObject");
        Console.WriteLine("2-5");
        LuaFunction fnRemoveObject = (IntPtr ptr) => RemoveObjectFromScene(lua);
        lua.PushCFunction(fnRemoveObject);
        lua.SetField(-1, "removeObjectFromScene");
        Console.WriteLine("2-6");
        LuaFunction fnCloneObject = (IntPtr ptr) => CloneObject(lua);
        lua.PushCFunction(fnCloneObject);
        lua.SetField(-1, "cloneObject");
        Console.WriteLine("2-7");
        LuaFunction fnGetProperty = (IntPtr ptr) => GetProperty(lua);
        lua.PushCFunction(fnGetProperty);
        lua.SetField(-1, "getProperty");
        Console.WriteLine("2-8");
        LuaFunction fnSetProperty = (IntPtr ptr) => SetProperty(lua);
        lua.PushCFunction(fnSetProperty);
        lua.SetField(-1, "setProperty");
        Console.WriteLine("2-9");
        LuaFunction fnGetComponent = (IntPtr ptr) => GetComponent(lua);
        lua.PushCFunction(fnGetComponent);
        lua.SetField(-1, "getComponent");
        Console.WriteLine("2-10");
        LuaFunction fnAddComponent = (IntPtr ptr) => AddComponent(lua);
        lua.PushCFunction(fnAddComponent);
        lua.SetField(-1, "addComponent");

        // Store in a global for scripts to access directly
        lua.SetGlobal("engine");
    }
}
