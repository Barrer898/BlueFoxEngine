using BlueFoxEngine.Configuration;
using BlueFoxEngine.Logging;
using BlueFoxEngine.Scripting;

namespace BlueFoxEngine.Assets.Scripts;

public class YueScriptAsset
{
    private Logger _logger = new Logger("YueScriptAsset");
    public string? RelativeTargetSourceFile { get; private set; }
    public string? CompiledLua { get; private set;  }
    public bool Compiled { get; private set;  }
    public bool CompileNow = false;

    public YueScriptAsset()
    {
        
    }
    public YueScriptAsset(string targetSourceFile)
    {
        this.RelativeTargetSourceFile = targetSourceFile;
    }
    public YueScriptAsset(string relativeTargetSourceFile, bool compileNow)
    {
        this.RelativeTargetSourceFile = relativeTargetSourceFile;
        if (compileNow)
        {
            CompileSourceFromFile();
        }
    }

    public void SetSourceFile(string relativePath)
    {
        this.RelativeTargetSourceFile = relativePath;
        this.Compiled = false;
    }
    
    public void CompileSourceFromFile()
    {
        if (RelativeTargetSourceFile.IsWhiteSpace() || RelativeTargetSourceFile == "" || RelativeTargetSourceFile == null)
        {
            _logger.Output(Logger.OutputType.Warning, Logger.OutputLevel.Warning, "Attempted to compile a script with no Path set.");
            return;
        }
        string fullLoadPath = System.IO.Path.Combine([AppContext.BaseDirectory, 
            CurrentEngineConfig._EngineConfig.Assets.Directory, "Scripts", RelativeTargetSourceFile]);
        if (File.Exists(fullLoadPath))
        {
            try
            {
                this.CompiledLua = YueScriptComplier.Compile(File.ReadAllText(fullLoadPath));
                this.Compiled = true;
            }
            catch (Exception e)
            {
                _logger.Output(Logger.OutputType.ExceptionThrownError, Logger.OutputLevel.Error, "YueScript Compilation Failed.", e);
            }
        }
    }
}