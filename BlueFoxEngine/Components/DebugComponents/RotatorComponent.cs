using BlueFoxEngine.Assets;
namespace BlueFoxEngine.Components.DebugComponents;

/// <summary>
/// These Components WILL be removed later on, testing only.
/// </summary>

public class RotatorComponent : Component
{
    public float Speed { get; set; } = 1f;

    public override void Update(double deltaTime)
    {
        if (Owner is Object2D object2D)
            object2D.Rotation += Speed * (float)deltaTime;
    }
}