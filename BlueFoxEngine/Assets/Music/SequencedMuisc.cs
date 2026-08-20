using System.ComponentModel;
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
       
        if (MusicSequences[CurrentSequenceIndex].SequenceEndTimestamp <=
            Raylib.GetMusicTimePlayed(MusicAsset.MusicValue))
        {
            if(MusicSequences[CurrentSequenceIndex].LoopCondition())
            {
                Raylib.SeekMusicStream(MusicAsset.MusicValue, MusicSequences[CurrentSequenceIndex].SequenceBeginTimestamp ); // I will most probably move this to a separate function -B.
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