using Raylib_cs;
using BlueFoxEngine.Assets.Textures;
using System.Numerics;

namespace BlueFoxEngine.Assets.Sprite;

public class Sprite : Object2D
{
    public TextureAsset Texture { get; private set; }

    //public Object2D ObjectTransform { get; }

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
        SourceRectangle =new(
            0,
            0,
            Texture.Width,
            Texture.Height
        );
        
    }

    public void SetOriginCenter()
    {
        this.Origin = new Vector2(
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
            this.Position.X,
            this.Position.Y,
            SourceRectangle.Width * this.Scale.X,
            SourceRectangle.Height * this.Scale.Y
        );

        Raylib.DrawTexturePro(
            Texture.Texture!.Value,
            source,
            destination,
            this.Origin,
            this.Rotation,
            Tint
        );
    }
}