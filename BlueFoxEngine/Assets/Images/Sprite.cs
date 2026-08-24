using Raylib_cs;
using BlueFoxEngine.Assets.Textures;
using System.Numerics;

namespace BlueFoxEngine.Assets.Sprite;

public class Sprite
{
    public TextureAsset Texture { get; private set; }

    public Object2D ObjectTransform { get; }
    public Object2D GetObjectTransformData()
    {
        return ObjectTransform;
    }

    public Color Tint { get; set; } = Color.White;

    /// <summary>
    /// The portion of the texture that should be rendered.
    /// By default, the entire texture is used.
    /// </summary>
    public Rectangle SourceRectangle { get; set; }

    public Sprite(TextureAsset texture)
    {
        this.ObjectTransform = new Object2D();
        this.Texture = texture;
        SourceRectangle =new(
            0,
            0,
            Texture.Width,
            Texture.Height
        );
        
    }
    public Sprite(TextureAsset texture, Object2D objectData)
    {
        this.Texture = texture;
        this.ObjectTransform = objectData;
        SourceRectangle =new(
            0,
            0,
            Texture.Width,
            Texture.Height
        );
        
    }

    public void SetOriginCenter()
    {
        this.ObjectTransform.Origin = new Vector2(
            SourceRectangle.Width / 2f,
            SourceRectangle.Height / 2f
        );
    }
    
    public void Draw()
    {
        if (!Texture.IsValid)
            return;

        Rectangle source = SourceRectangle;

        Rectangle destination = new(
            ObjectTransform.Position.X,
            ObjectTransform.Position.Y,
            SourceRectangle.Width * ObjectTransform.Scale.X,
            SourceRectangle.Height * ObjectTransform.Scale.Y
        );

        Raylib.DrawTexturePro(
            Texture.Texture!.Value,
            source,
            destination,
            ObjectTransform.Origin,
            ObjectTransform.Rotation,
            Tint
        );
    }
}