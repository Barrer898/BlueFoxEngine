namespace BlueFoxEngine;


public class EngineInitializationException : Exception
{
    public EngineInitializationException()
    {
    }

    public EngineInitializationException(string message)
        : base(message)
    {
    }

    public EngineInitializationException(string message, Exception inner)
        : base(message, inner)
    {
    }
}
