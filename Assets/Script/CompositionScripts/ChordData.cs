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