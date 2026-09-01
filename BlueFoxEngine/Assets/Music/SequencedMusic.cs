using Raylib_cs;

namespace BlueFoxEngine.Assets;


public class MusicSequence : Object // To Be Rewritten: I'll adapt to Payday3's method of SequencedMusic. But that gets put off for later. -B
{
    public float SequenceBeginTimestamp { get; private set; }
    public float SequenceEndTimestamp { get; private set; }
    
    internal Func<bool> LoopCondition;

    public MusicSequence(float sequenceBeginTimestamp, float sequenceEndTimestamp, Func<bool> loopCondition)
    {
        this.SequenceBeginTimestamp = sequenceBeginTimestamp;
        this.SequenceEndTimestamp = sequenceEndTimestamp;
        this.LoopCondition = loopCondition;
    }

    public override void Destroy()
    {
        base.Destroy();
    }
}

public class SequencedMusicAsset : Object
{
    public List<MusicSequence> MusicSequences { get; private set; }
    public MusicAsset MusicAsset { get; private set; }

    public int CurrentSequenceIndex { get; private set; } = 0;

    public SequencedMusicAsset()
    {
    }

    public void Update()
    {
        if (CurrentSequenceIndex == -1) return; // nothing to update...
        MusicSequence currentSequence = MusicSequences[CurrentSequenceIndex];
        if (currentSequence.SequenceEndTimestamp <=
            Raylib.GetMusicTimePlayed(MusicAsset.MusicValue))
        {
            if(currentSequence.LoopCondition())
            {
                MusicAsset.SeekMusicStream(currentSequence.SequenceBeginTimestamp);
            }
            else
            {
                if (CurrentSequenceIndex + 1 > MusicSequences.Count - 1 )
                {
                    CurrentSequenceIndex = -1;
                }
                else
                {   
                    CurrentSequenceIndex++;
                }
            }
           
        }
        
    }
    
    public SequencedMusicAsset(List<MusicSequence> musicSequences, MusicAsset musicAsset)
    {
        this.MusicSequences = musicSequences;
        this.MusicAsset = musicAsset;
    }

    public void SetMusic(MusicAsset musicAsset)
    {
        MusicAsset = musicAsset;
    }

    public int GetIndexOfCurrentSequence()
    {
        return CurrentSequenceIndex;
    }
    
    public override void Destroy() // actual destroy/unload logic should be in the MusicAsset this just uses the MusicObject. 
    {
        base.Destroy();
    }
}