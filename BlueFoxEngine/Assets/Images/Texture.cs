using BlueFoxEngine.Logging;
using System.Numerics;
using Raylib_cs;

namespace BlueFoxEngine.Assets.Textures;

public class TextureAsset : Object
{
    private Logger _logger = new("TextureAsset");
    public Texture2D? Texture { get; private set; }

    public Vector2 Dimensions => Texture?.Dimensions ?? new Vector2();
    /// <summary>
    /// Make sure that this variable ISN'T 0! if its zero then that could mean something is wrong.
    /// </summary>
    public uint OpenGlTextureId => Texture?.Id ?? 0;
    public PixelFormat Format => Texture?.Format ?? new PixelFormat();
    public int Height => Texture?.Height ?? 0;
    public int Width => Texture?.Width ?? 0;
    public int Mipmaps => Texture?.Mipmaps ?? 0;

    public bool IsValid => Texture.HasValue && Raylib.IsTextureValid(Texture.Value);

    public void SetTexture(Texture2D texture2D)
    {
        if (Raylib.IsTextureValid(texture2D))
        {
            this.Texture = texture2D;
        }
    }

    public TextureAsset()
    {
        
    }
    public TextureAsset(Texture2D texture2D)
    {
        if (Raylib.IsTextureValid(texture2D))
            this.Texture = texture2D;
        else
        {
            _logger.Output(
                Logger.OutputType.ExceptionThrownWarning,
                Logger.OutputLevel.Warning, 
                "Attempted to create TextureAsset from an invalid Texture2D variable.", 
                new InvalidAssetException("Given Texture2D wasn't valid to load."));
            
            return;
        }
    }
}

public static class MissingTexture
{
    private static TextureAsset? _missingTextureCache;
    
    public static TextureAsset Generate(
        int width = 128,
        int height = 128,
        int checkerSize = 16)
    {
        if (_missingTextureCache == null)
        {
            Image image = Raylib.GenImageColor(
                width,
                height,
                Color.Black
            );

            Color purple = new Color(255, 0, 255, 255);
            Color black = new Color(0, 0, 0, 255);

            // Generate the checkerboard.
            for (int y = 0; y < height; y += checkerSize)
            {
                for (int x = 0; x < width; x += checkerSize)
                {
                    bool even = ((x / checkerSize) + (y / checkerSize)) % 2 == 0;

                    Raylib.ImageDrawRectangle(
                        ref image,
                        x,
                        y,
                        checkerSize,
                        checkerSize,
                        even ? purple : black
                    );
                }
            }


            Texture2D texture = Raylib.LoadTextureFromImage(image);

            _missingTextureCache = new TextureAsset(texture);

            Raylib.UnloadImage(image);

            return _missingTextureCache;
        }
        else
            return _missingTextureCache;




    }
}