using System.Text;
using KeraLua;

namespace BlueFoxEngine.Scripting;

/// <summary>
/// Compiles YueScript source code to Lua source using the
/// yue.to_lua() function from the pre-configured Lua runtime.
///
/// Each compilation reuses a shared Lua runtime to avoid the cost
/// of creating a fresh Lua state per call. The runtime's Lua stack
/// is cleaned up after every compile.
/// </summary>
public static class YueScriptComplier
{
    private static YueScriptRuntime _runtime = new();

    /// <summary>
    /// Compiles YueScript source code to Lua source.
    ///
    /// The compiler uses the yue.to_lua() function exposed by the
    /// shared Lua runtime. Any compilation error is re-thrown as a
    /// .NET exception with the original YueScript error message.
    /// </summary>
    /// <param name="source">YueScript source code to compile.</param>
    /// <returns>Compiled Lua source as a string.</returns>
    public static string Compile(string source)
    {
        Lua lua = _runtime.GetLuaCore();

        // Push yue.to_lua onto the stack (1 value returned).
        lua.GetGlobal("yue");
        lua.GetField(-1, "to_lua");
        lua.Remove(-2);

        // Push source argument and call to_lua(source).
        lua.PushString(source);
        LuaStatus status = lua.PCall(1, 1, 0);

        if (status != LuaStatus.OK)
        {
            // Clean up the error message on the stack.
            string error = lua.ToString(-1);
            lua.Pop(1);

            throw new Exception($"YueScript compilation failed: {error}");
        }

        // Read and clean up the compiled source result.
        string result = lua.ToString(-1);
        lua.Pop(1);

        return result;
    }
}
