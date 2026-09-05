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
public class InvalidAssetException : Exception
{
    public InvalidAssetException()
    {
    }

    public InvalidAssetException(string message)
        : base(message)
    {
    }

    public InvalidAssetException(string message, Exception inner)
        : base(message, inner)
    {
    }
}

public class OutOfObjectSpaceException : Exception
{
    public OutOfObjectSpaceException()
    {
    }

    public OutOfObjectSpaceException(string message)
        : base(message)
    {
    }

    public OutOfObjectSpaceException(string message, Exception inner)
        : base(message, inner)
    {
    }
}