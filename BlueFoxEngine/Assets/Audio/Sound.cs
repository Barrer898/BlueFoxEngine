using Raylib_cs;
namespace BlueFoxEngine.Assets;

/*  CachedSound was an early instance of caching, for now ill leave it commented just in case. but expect this to be removed at any moment. including the entire file
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
}*/

public static class InvalidAudio
{
    public static readonly Sound InvalidSound = new Sound();
    //public static readonly CachedSound InvalidCachedSound = new CachedSound(InvalidSound, 0);
}
