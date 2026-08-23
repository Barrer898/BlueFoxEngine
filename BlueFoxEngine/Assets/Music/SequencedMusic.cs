using Raylib_cs;

namespace BlueFoxEngine.Assets;


public class MusicSequence
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
}

public class SequencedMusicAsset
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
}