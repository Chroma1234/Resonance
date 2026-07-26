using System.Collections.Generic;
using UnityEngine;

public class MoodManager : MonoBehaviour
{
    public static MoodManager Instance;

    private Dictionary<InstrumentData, Mood> selectedMoods = new();

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void SetMood(InstrumentData instrument, Mood mood)
    {
        selectedMoods[instrument] = mood;

        Debug.Log($"CompositionManager: {instrument.instrumentName} = {mood}");
    }

    public Mood GetMood(InstrumentData instrument)
    {
        if (selectedMoods.TryGetValue(instrument, out Mood mood))
        {
            return mood;
        }

        return Mood.Happy;
    }
}