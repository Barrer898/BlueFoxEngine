using Raylib_cs;
using BlueFoxEngine.Assets.Textures;
using System.Numerics;

namespace BlueFoxEngine.Assets.Sprite;

public class Sprite
{
    public TextureAsset Texture { get; private set; }

    private Object2D ObjectData { get; set; } = new Object2D();

    public Object2D GetObjectData()
    {
        return ObjectData;
    }

    public Color Tint { get; set; } = Color.White;

    /// <summary>
    /// The portion of the texture that should be rendered.
    /// By default, the entire texture is used.
    /// </summary>
    public Rectangle SourceRectangle { get; set; }

    public Sprite(TextureAsset texture)
    {
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
        this.ObjectData = ObjectData;
        SourceRectangle =new(
            0,
            0,
            Texture.Width,
            Texture.Height
        );
        
    }

    public void SetOriginCenter()
    {
        this.ObjectData.Origin = new Vector2(
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
            ObjectData.Position.X,
            ObjectData.Position.Y,
            Texture.Width * ObjectData.Scale.X,
            Texture.Height * ObjectData.Scale.Y
        );

        Raylib.DrawTexturePro(
            Texture.Texture!.Value,
            source,
            destination,
            ObjectData.Origin,
            ObjectData.Rotation,
            Tint
        );
    }
}