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
public static class YueScriptCompiler
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
        if (string.IsNullOrWhiteSpace(source))
            throw new ArgumentException(
                "YueScript source cannot be null or empty.",
                nameof(source));

        Lua lua = _runtime.GetLuaCore();
        int originalTop = lua.GetTop();

        try
        {
            // Push yue.to_lua onto the stack.
            lua.GetGlobal("yue");
            lua.GetField(-1, "to_lua");
            lua.Remove(-2);

            // Push the source and request two return values:
            // compiled Lua source and a possible error message.
            lua.PushString(source);
            LuaStatus status = lua.PCall(1, 2, 0);

            if (status != LuaStatus.OK)
            {
                string error = lua.ToString(-1) ?? "Unknown YueScript error.";
                throw new Exception($"YueScript compilation failed: {error}");
            }

            // With two requested results, the first result is at -2
            // and the second result is at -1.
            string? result = lua.ToString(-2);
            string? errorMessage = lua.ToString(-1);

            if (string.IsNullOrWhiteSpace(result) ||
                string.Equals(result.Trim(), "nil", StringComparison.Ordinal))
            {
                throw new Exception(
                    "YueScript returned no compiled Lua source. " +
                    $"Compiler message: {errorMessage ?? "<none>"}");
            }

            return result;
        }
        finally
        {
            // Restore the exact stack state even if compilation fails.
            lua.SetTop(originalTop);
        }
    }
}
