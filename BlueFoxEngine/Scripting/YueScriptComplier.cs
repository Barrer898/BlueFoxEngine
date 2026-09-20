using System.Text;
using KeraLua;

namespace BlueFoxEngine.Scripting;

public static class YueScriptComplier 
{
    private static YueScriptRuntime _runtime = new();
    
    public static string Compile(string source)
    {
        Lua lua = _runtime.GetLuaCore();

        // Get yue table.
        lua.GetGlobal("yue");

        // Get yue.to_lua.
        lua.GetField(-1, "to_lua");

        // Remove the yue table, leaving to_lua on stack.
        lua.Remove(-2);

        // Push source.
        lua.PushString(source);

        // Call to_lua(source).
        LuaStatus status = lua.PCall(1, 1, 0);

        if (status != LuaStatus.OK)
        {
            string error = lua.ToString(-1);
            lua.Pop(1);

            throw new Exception($"YueScript compilation failed: {error}");
        }

        string result = lua.ToString(-1);

        lua.Pop(1);

        return result;
    }
}