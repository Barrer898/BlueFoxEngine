using BlueFoxEngine.Configuration;
using BlueFoxEngine.Logging;
using BlueFoxEngine.Scripting;

namespace BlueFoxEngine.Assets.Scripts;

/// <summary>
/// Represents a YueScript (.ys) asset stored on disk.
///
/// The asset can hold either the raw source path or the pre-compiled
/// Lua source. Compilation is performed lazily on first use, or
/// eagerly when requested at construction time.
///
/// Inherits from Object so it can be managed by the SceneManager
/// and participate in the engine's standard lifecycle.
/// </summary>
public class YueScriptAsset
{
    private Logger _logger = new("YueScriptAsset");

    /// <summary>
    /// Relative path from the configured assets root to the source file.
    /// Null means no file has been assigned yet.
    /// </summary>
    public string? RelativeTargetSourceFile { get; private set; }

    /// <summary>
    /// The compiled Lua source. Null before successful compilation.
    /// </summary>
    public string? CompiledLua { get; private set; }

    /// <summary>
    /// Whether compilation has been attempted and succeeded.
    /// </summary>
    public bool Compiled { get; private set; }

    /// <summary>
    /// When true, compiles the source eagerly on construction.
    /// </summary>
    public bool CompileNow = false;

    /// <summary>
    /// Creates an empty asset with no file assigned.
    /// </summary>
    public YueScriptAsset()
    {
    }

    /// <summary>
    /// Creates an asset pointing to a source file.
    /// Compilation is deferred until first execution.
    /// </summary>
    public YueScriptAsset(string targetSourceFile)
    {
        this.RelativeTargetSourceFile = targetSourceFile;
    }

    /// <summary>
    /// Creates an asset and optionally compiles immediately.
    /// </summary>
    public YueScriptAsset(string relativeTargetSourceFile, bool compileNow)
    {
        this.RelativeTargetSourceFile = relativeTargetSourceFile;
        if (compileNow)
        {
            CompileSourceFromFile();
        }
    }

    /// <summary>
    /// Updates the source file path and marks the asset as uncompiled.
    /// </summary>
    public void SetSourceFile(string relativePath)
    {
        this.RelativeTargetSourceFile = relativePath;
        this.Compiled = false;
        this.CompiledLua = null;
    }

    /// <summary>
    /// Reads the source file from disk and compiles it to Lua source.
    /// </summary>
    public void CompileSourceFromFile()
    {
        if (string.IsNullOrWhiteSpace(RelativeTargetSourceFile) || RelativeTargetSourceFile == null)
        {
            _logger.Output(Logger.OutputType.Warning, Logger.OutputLevel.Warning, "Attempted to compile a script with no path set.");
            return;
        }

        // Resolve the absolute path to the source file.
        string fullLoadPath = System.IO.Path.Combine(AppContext.BaseDirectory,
            CurrentEngineConfig._EngineConfig.Assets.Directory, "Scripts", RelativeTargetSourceFile);

        if (File.Exists(fullLoadPath))
        {
            try
            {
                this.CompiledLua = YueScriptComplier.Compile(File.ReadAllText(fullLoadPath));
                this.Compiled = true;
            }
            catch (Exception e)
            {
                _logger.Output(Logger.OutputType.ExceptionThrownError, Logger.OutputLevel.Error, "YueScript compilation failed.", e);
            }
        }
        else
        {
            _logger.Output(Logger.OutputType.Warning, Logger.OutputLevel.Warning, $"Failed to find file {fullLoadPath}");
        }
    }
}
