using FMODUnity;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "InstrumentData", menuName = "Instrument Data")]
public class InstrumentData : ScriptableObject
{
    public List<MoodEvent> moodEvents;

    public string instrumentName;

    public float maxDistance = 15f;

    public float intenseDistance = 5f;

    public float duetRadius = 8f;

    public float smoothing = 3f;

    public GameObject modelPrefab;

    public EventReference GetEvent(Mood mood)
    {
        foreach (MoodEvent moodEvent in moodEvents)
        {
            if (moodEvent.mood == mood)
            {
                return moodEvent.eventReference;
            }
        }

        return default;
    }
}