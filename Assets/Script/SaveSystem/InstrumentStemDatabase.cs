using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(
    fileName = "InstrumentStemDatabase",
    menuName = "Resonance/Instrument Stem Database")]
public class InstrumentStemDatabase : ScriptableObject
{
    [SerializeField] private List<InstrumentStem> stems = new();

    public InstrumentStem Find(
        string instrumentId,
        PatternType pattern,
        StyleType style)
    {
        return stems.Find(stem =>
            stem != null &&
            stem.instrumentId == instrumentId &&
            stem.pattern == pattern &&
            stem.style == style);
    }

    public bool Contains(
        string instrumentId,
        PatternType pattern,
        StyleType style)
    {
        return Find(instrumentId, pattern, style) != null;
    }
}
