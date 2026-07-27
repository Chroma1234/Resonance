using FMODUnity;
using UnityEngine;

[CreateAssetMenu(menuName = "Music/Chord Data")]
public class ChordData : ScriptableObject
{
    // Name shown in the UI.
    public string chordName;

    // FMOD event that plays this chord.
    public EventReference chordEvent;
}

/*I used a ScriptableObject so each chord’s data is stored separately 
 * from the UI and playback code. This lets me add new chords without changing the scripts.*/