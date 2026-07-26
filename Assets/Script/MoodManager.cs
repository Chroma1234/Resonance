using System.Collections.Generic;
using UnityEngine;

public class MoodManager : MonoBehaviour
{
    public static MoodManager Instance { get; private set; }

    private readonly Dictionary<InstrumentData, Mood> selectedMoods = new();

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public void SetMood(InstrumentData instrument, Mood mood)
    {
        if (instrument == null)
        {
            Debug.LogWarning(
                "MoodManager: Cannot assign a mood to a null instrument.");

            return;
        }

        selectedMoods[instrument] = mood;

        Debug.Log(
            $"MoodManager: {instrument.instrumentName} = {mood}");
    }

    public Mood GetMood(InstrumentData instrument)
    {
        if (instrument != null &&
            selectedMoods.TryGetValue(instrument, out Mood mood))
        {
            return mood;
        }

        return Mood.Happy;
    }

    public bool TryGetMood(
        InstrumentData instrument,
        out Mood mood)
    {
        if (instrument != null &&
            selectedMoods.TryGetValue(instrument, out mood))
        {
            return true;
        }

        mood = Mood.Happy;
        return false;
    }

    public void ClearSelections()
    {
        selectedMoods.Clear();
    }

    public void ApplySelections(
        IEnumerable<InstrumentMood> selections,
        bool clearExisting = true)
    {
        if (clearExisting)
        {
            selectedMoods.Clear();
        }

        if (selections == null)
        {
            return;
        }

        foreach (InstrumentMood selection in selections)
        {
            if (selection == null || selection.instrument == null)
            {
                continue;
            }

            SetMood(selection.instrument, selection.mood);
        }
    }
}