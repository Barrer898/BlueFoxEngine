namespace BlueFoxEngine.Components;

public class Component : Object
{
    public Object? Owner { get; internal set; }

    protected Component(Object owner)
    {
        Owner = owner;
    }
    protected Component()
    {
    }

    public virtual void Update(double deltaTime)
    {
    }

    public virtual void Initialize()
    {
    }

    public virtual void Dispose()
    {
        
    }
}