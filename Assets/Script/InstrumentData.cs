using FMODUnity;
using UnityEngine;

[CreateAssetMenu(fileName = "InstrumentData", menuName = "Instrument Data")]
public class InstrumentData : ScriptableObject
{
    public EventReference eventReference;

    public string instrumentName;

    public float maxDistance = 15f;

    public float intenseDistance = 5f;

    public float duetRadius = 8f;

    public float smoothing = 3f;

    public GameObject modelPrefab;
}