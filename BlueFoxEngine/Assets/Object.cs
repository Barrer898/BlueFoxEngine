using System.Numerics;

namespace BlueFoxEngine.Assets;

public class Object : IDisposable
{
    public virtual void DisposeLogic() { }
    
    private Action _disposeLogic => DisposeLogic;
    
    public void Dispose()
    {
        _disposeLogic();
        GC.SuppressFinalize(this);
    }
}

public class Object2D : Object
{
    public Vector2 Position { get; set; }

    public Vector2 Scale { get; set; } = Vector2.One;

    public float Rotation { get; set; }

    public Vector2 Origin { get; set; } = Vector2.Zero;
    
    
    public float GetPositionX()
    {
        return Position.X;
    }
    public float GetPositionY()
    {
        return Position.Y;
    }
    
    public Object2D()
    {
        
    }

    public Object2D(Vector2 position)
    {
        this.Position = position;
    }
    
    public Object2D(float positionX, float positionY)
    {
        this.Position = new Vector2(positionX, positionY);
    }
    
    public Object2D(Vector2 position, Vector2 scale)
    {
        this.Position = position;
        this.Scale = scale;
    }
    
    public Object2D(Vector2 position, float scaleX, float scaleY)
    {
        this.Position = position;
        this.Scale = new Vector2(scaleX, scaleY);
    }
    
    
    public Object2D(Vector2 position, Vector2 scale, float rotation)
    {
        this.Position = position;
        this.Scale = scale;
        this.Rotation = rotation;
    }
    
    public Object2D(Vector2 position, Vector2 scale, float rotation, Vector2 origin)
    {
        this.Position = position;
        this.Scale = scale;
        this.Rotation = rotation;
        this.Origin = origin;
    }
}