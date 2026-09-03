using BlueFoxEngine.Assets.Textures;
using Raylib_cs;

namespace BlueFoxEngine.Assets.Sprite;

public class SpriteSheet : Object
{
    public TextureAsset Texture { get; }

    public int FrameWidth { get; }
    public int FrameHeight { get; }

    public int Columns => Texture.Width / FrameWidth;
    public int Rows => Texture.Height / FrameHeight;

    public int FrameCount => Columns * Rows;

    public SpriteSheet(
        TextureAsset texture,
        int frameWidth,
        int frameHeight)
    {
        if (frameWidth <= 0)
            throw new ArgumentOutOfRangeException(nameof(frameWidth));

        if (frameHeight <= 0)
            throw new ArgumentOutOfRangeException(nameof(frameHeight));

        if (texture.Width % frameWidth != 0)
            throw new ArgumentException(
                "Frame width does not evenly divide the texture width.",
                nameof(frameWidth)
            );

        if (texture.Height % frameHeight != 0)
            throw new ArgumentException(
                "Frame height does not evenly divide the texture height.",
                nameof(frameHeight)
            );

        Texture = texture;
        FrameWidth = frameWidth;
        FrameHeight = frameHeight;
    }

    public Rectangle GetFrame(int index)
    {
        if (index < 0 || index >= FrameCount)
            throw new ArgumentOutOfRangeException(nameof(index));

        int column = index % Columns;
        int row = index / Columns;

        return new Rectangle(
            column * FrameWidth,
            row * FrameHeight,
            FrameWidth,
            FrameHeight
        );
    }
}