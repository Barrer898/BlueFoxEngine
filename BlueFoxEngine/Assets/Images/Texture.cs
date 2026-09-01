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