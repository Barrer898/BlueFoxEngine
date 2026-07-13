using Raylib_cs;
namespace BlueFoxEngine.Assets;


public class CachedSound
{
    public Sound Sound;
    public int ReferenceCount { get; private set; }
    public bool IsValid => Raylib.IsSoundValid(this.Sound);

    public void IncreaseReferenceCount()
    {
        if(this.IsValid)
            this.ReferenceCount++;
    }
    public void DecreaseReferenceCount()
    {
        this.ReferenceCount--;
    }
        
    public CachedSound(Sound sound, int referenceCount)
    {
        this.Sound = sound;
        this.ReferenceCount = referenceCount;
    }
}

public static class InvalidAudio
{
    public static readonly Sound InvalidSound = new Sound();
    public static readonly CachedSound InvalidCachedSound = new CachedSound(InvalidSound, 0);
}
