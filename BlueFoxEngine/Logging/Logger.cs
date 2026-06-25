using BlueFoxEngine.Configuration;
namespace BlueFoxEngine.Logging;
public class Logger
{
    public enum OutputType
    {
        Info,
        Notice,
        Warning,
        ExceptionThrownWarning,
        Error,
        ExceptionThrownError,
        CriticalError
    }
    public enum OutputLevel
    {
        None = 0,
        Critical = 1,
        Error = 2,
        Warning = 3,
        Info = 4,
        Verbose = 5,
        Debug = 6,
        Trace = 7
    }
    public Logger(string systemName)
    {
        SystemName = systemName;
        UpdateOutputLevel();
    }

    public Logger(string systemName, ConsoleColor infoColor, ConsoleColor bracketsInfoColor, ConsoleColor warningColor,
        ConsoleColor bracketsWarningColor, ConsoleColor errorColor, ConsoleColor bracketsErrorColor)
    {
        SystemName = systemName;
        InfoColor = infoColor;
        BracketsInfoColor = bracketsInfoColor;
        WarningColor = warningColor;
        BracketsWarningColor = bracketsWarningColor;
        ErrorColor = errorColor;
        BracketsErrorColor = bracketsErrorColor;
        UpdateOutputLevel();
    }

    private string SystemName { get; }
    private ConsoleColor InfoColor { get; } = ConsoleColor.DarkGray;
    private ConsoleColor BracketsInfoColor { get; } = ConsoleColor.Gray;
    private ConsoleColor WarningColor { get; } = ConsoleColor.Yellow;
    private ConsoleColor BracketsWarningColor { get; } = ConsoleColor.DarkYellow;
    private ConsoleColor ErrorColor { get; } = ConsoleColor.DarkRed;
    private ConsoleColor BracketsErrorColor { get; } = ConsoleColor.Red;

    internal OutputLevel CurrentOutputLevel { get; set;  }

    public void UpdateOutputLevel()
    {
        OutputLevel newOutputLevel = OutputLevel.Trace;
        
        if (CurrentEngineConfig._EngineConfig != null && OutputLevel.TryParse(CurrentEngineConfig._EngineConfig.Logging.Level, out newOutputLevel))
            this.CurrentOutputLevel = newOutputLevel;
        else
            this.CurrentOutputLevel = newOutputLevel;
    }
    
    public void Output(OutputType outputType, string message, ConsoleColor? primaryColorOverride,
        ConsoleColor? secondaryColorOverride, OutputLevel outputLevel, Exception? exception = null)
    {
        if (outputLevel <= this.CurrentOutputLevel)
        {
            ConsoleColor originalColor = Console.ForegroundColor;
            if (secondaryColorOverride == null)
            {
                Console.ForegroundColor = GetLoggerColorFor(outputType, true);
                Console.Write("[");
            }
            else
            {
                Console.ForegroundColor = (ConsoleColor)secondaryColorOverride;
                Console.Write("[");
            }

            if (primaryColorOverride == null)
            {
                Console.ForegroundColor = GetLoggerColorFor(outputType);
                Console.Write(SystemName);
            }
            else
            {
                Console.ForegroundColor = (ConsoleColor)primaryColorOverride;
                Console.Write(SystemName);
            }

            if (secondaryColorOverride == null)
            {
                Console.ForegroundColor = GetLoggerColorFor(outputType, true);
                Console.Write("]");
            }
            else
            {
                Console.ForegroundColor = (ConsoleColor)secondaryColorOverride;
                Console.Write("]");
            }

            Console.ForegroundColor = originalColor;

            Console.Write($" {message}\n");
            if (exception != null)
                Console.WriteLine($"Exception: {exception.Message}: {exception.StackTrace}");
        }
    }

    public void Output(OutputType outputType, string message, Exception? exception, OutputLevel outputLevel)
    {
        Output(outputType, message, null, null, outputLevel, exception);
    }

    public void Output(OutputType outputType, string message , OutputLevel outputLevel)
    {
        Output(outputType, message, null, outputLevel);
    }

    public ConsoleColor GetLoggerColorFor(OutputType outputType, bool secondary = false)
    {
        if (secondary)
            switch (outputType)
            {
                case OutputType.Info:
                    return BracketsInfoColor;
                case OutputType.Notice:
                    return BracketsInfoColor;
                case OutputType.Warning:
                    return BracketsWarningColor;
                case OutputType.ExceptionThrownWarning:
                    return BracketsWarningColor;
                case OutputType.Error:
                    return BracketsErrorColor;
                case OutputType.ExceptionThrownError:
                    return BracketsErrorColor;
                case OutputType.CriticalError:
                    return BracketsErrorColor;
            }
        else
            switch (outputType)
            {
                case OutputType.Info:
                    return InfoColor;
                case OutputType.Notice:
                    return InfoColor;
                case OutputType.Warning:
                    return WarningColor;
                case OutputType.ExceptionThrownWarning:
                    return WarningColor;
                case OutputType.Error:
                    return ErrorColor;
                case OutputType.ExceptionThrownError:
                    return ErrorColor;
                case OutputType.CriticalError:
                    return ErrorColor;
            }

        return ConsoleColor.Black;
    }
}