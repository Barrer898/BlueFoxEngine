using BlueFoxEngine.Logging;
using KeraLua;

namespace BlueFoxEngine.Scripting;

public sealed class YueScriptRuntime : IDisposable
{
    private Logger _logger = new Logger("YueScriptRuntime");
    private readonly Lua _lua;
    private bool _disposed;

    protected internal Lua GetLuaCore()
    {
        return _lua;
    }
        
    
    public YueScriptRuntime()
    {
        _lua = new Lua();

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

    private void LoadYueScript()
    {
        _lua.DoString("""
                          yue = require("yue")
                          _G.Script = {}
                          _G.Script.Parent = {}
                      """);
    }

    public void Execute(string source)
    {
        if (_disposed)
            throw new ObjectDisposedException(nameof(YueScriptRuntime));

        _logger.Output(Logger.OutputType.Info, Logger.OutputLevel.Trace, source);
        
        _lua.DoString(source);
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        _disposed = true;
        _lua.Dispose();
    }
}