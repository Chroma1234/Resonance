using UnityEngine;

[CreateAssetMenu(
    fileName = "InstrumentStem",
    menuName = "Resonance/Instrument Stem")]
public class InstrumentStem : ScriptableObject
{
    [Header("Identity")]
    public string instrumentId;
    public PatternType pattern;
    public StyleType style;

    [Header("FMOD Event Paths")]
    public string normalEventPath;
    public string activeEventPath;
    public string soloEventPath;

    [Header("Musical Properties")]
    public float bpm = 98f;
}
