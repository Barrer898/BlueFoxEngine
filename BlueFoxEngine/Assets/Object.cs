using BlueFoxEngine.Components;
using System.Numerics;
using BlueFoxEngine.Logging;
using BlueFoxEngine.Scenes;

namespace BlueFoxEngine.Assets;



public class Object : IDisposable
{
    private Logger _logger = new Logger("ObjectClass");
 

    public Object()
    {
        SceneManager.RegisterNewObject(this);
    }
    
    private bool _disposed;

    private readonly List<Component> _components = new();

    public bool IsDisposed => _disposed;

    public IReadOnlyList<Component> Components => _components;

    protected virtual void DisposeLogic()
    {
    }
    

    public T AddComponent<T>() where T : Component
    {
        T component = Activator.CreateInstance<T>();

        component.Owner = this;

        _components.Add(component);
        
        component.Initialize();

        return component;
    }
    
    public T AddComponent<T>(Func<Object, T> factory)
        where T : Component
    {
        T component = factory(this);

        _components.Add(component);

        return component;
    }

    public T? GetComponent<T>() where T : Component
    {
        return _components.OfType<T>().FirstOrDefault();
    }

    public bool TryGetComponent<T>(out T? component)
        where T : Component
    {
        component = _components.OfType<T>().FirstOrDefault();
        return component != null;
    }

    public void RemoveComponent<T>() where T : Component
    {
        T? component = GetComponent<T>();

        if (component == null)
            return;

        _components.Remove(component);
        component.Dispose();
    }

    public void UpdateComponents(double deltaTime)
    {
        foreach (Component component in _components)
            component.Update(deltaTime);
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        _disposed = true;

        DisposeLogic();

        foreach (Component component in _components)
            component.Dispose();

        _components.Clear();

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