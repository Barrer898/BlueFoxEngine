namespace BlueFoxEngine.Components;

public class CSharpComponent : IDisposable 
{
    public Object Owner { get; internal set; }

    protected CSharpComponent(Object owner)
    {
        Owner = owner;
    }
    protected CSharpComponent()
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