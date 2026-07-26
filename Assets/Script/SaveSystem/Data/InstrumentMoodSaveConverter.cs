using System;
using System.Collections.Generic;
using UnityEngine;

public static class InstrumentMoodSaveConverter
{
    public static SavedConfiguration Capture(
        InstrumentDatabase database,
        MoodManager moodManager,
        string id,
        string displayName,
        string createdUtc)
    {
        if (database == null)
        {
            Debug.LogError(
                "InstrumentMoodSaveConverter: InstrumentDatabase is null.");

            return null;
        }

        if (moodManager == null)
        {
            Debug.LogError(
                "InstrumentMoodSaveConverter: MoodManager is null.");

            return null;
        }

        string now = DateTime.UtcNow.ToString("o");

        SavedConfiguration save = new SavedConfiguration
        {
            id = id,
            displayName = string.IsNullOrWhiteSpace(displayName)
                ? "Untitled Configuration"
                : displayName.Trim(),

            createdUtc = string.IsNullOrWhiteSpace(createdUtc)
                ? now
                : createdUtc,

            modifiedUtc = now
        };

        if (database.instruments == null)
        {
            Debug.LogWarning(
                "InstrumentMoodSaveConverter: " +
                "The database contains no instrument list.");

            return save;
        }

        foreach (InstrumentData instrument in database.instruments)
        {
            if (instrument == null)
            {
                continue;
            }

            // Only save instruments that appear in the mood selection menu.
            if (!instrument.mixable)
            {
                continue;
            }

            if (string.IsNullOrWhiteSpace(instrument.instrumentName))
            {
                Debug.LogWarning(
                    "InstrumentMoodSaveConverter: " +
                    "Skipped an InstrumentData with no instrumentName.");

                continue;
            }

            Mood selectedMood = moodManager.GetMood(instrument);

            SavedInstrumentConfig savedInstrument =
                new SavedInstrumentConfig
                {
                    instrumentName = instrument.instrumentName,
                    mood = selectedMood.ToString()
                };

            save.instruments.Add(savedInstrument);
        }

        return save;
    }

    public static bool TryRestore(
        SavedConfiguration save,
        InstrumentDatabase database,
        MoodManager moodManager,
        out string error)
    {
        if (save == null)
        {
            error = "The loaded configuration is null.";
            return false;
        }

        if (database == null)
        {
            error = "InstrumentDatabase is not assigned.";
            return false;
        }

        if (moodManager == null)
        {
            error = "MoodManager does not exist in the scene.";
            return false;
        }

        if (save.instruments == null)
        {
            error = "The save contains no instrument list.";
            return false;
        }

        List<InstrumentMood> restoredSelections =
            new List<InstrumentMood>();

        foreach (SavedInstrumentConfig savedInstrument
                 in save.instruments)
        {
            if (savedInstrument == null)
            {
                continue;
            }

            InstrumentData instrument =
                FindInstrument(
                    database,
                    savedInstrument.instrumentName);

            if (instrument == null)
            {
                error =
                    $"Instrument '{savedInstrument.instrumentName}' " +
                    "was not found in the InstrumentDatabase.";

                return false;
            }

            if (!Enum.TryParse(
                    savedInstrument.mood,
                    true,
                    out Mood mood))
            {
                error =
                    $"Mood '{savedInstrument.mood}' is invalid for " +
                    $"instrument '{savedInstrument.instrumentName}'.";

                return false;
            }

            if (!SupportsMood(instrument, mood))
            {
                error =
                    $"Instrument '{instrument.instrumentName}' " +
                    $"does not contain a MoodEvent for '{mood}'.";

                return false;
            }

            restoredSelections.Add(
                new InstrumentMood
                {
                    instrument = instrument,
                    mood = mood
                });
        }

        moodManager.ApplySelections(
            restoredSelections,
            clearExisting: true);

        error = string.Empty;
        return true;
    }

    private static InstrumentData FindInstrument(
        InstrumentDatabase database,
        string instrumentName)
    {
        if (database.instruments == null ||
            string.IsNullOrWhiteSpace(instrumentName))
        {
            return null;
        }

        foreach (InstrumentData instrument in database.instruments)
        {
            if (instrument == null)
            {
                continue;
            }

            if (string.Equals(
                    instrument.instrumentName,
                    instrumentName,
                    StringComparison.OrdinalIgnoreCase))
            {
                return instrument;
            }
        }

        return null;
    }

    private static bool SupportsMood(
        InstrumentData instrument,
        Mood mood)
    {
        if (instrument == null || instrument.moodEvents == null)
        {
            return false;
        }

        foreach (MoodEvent moodEvent in instrument.moodEvents)
        {
            if (moodEvent != null && moodEvent.mood == mood)
            {
                return true;
            }
        }

        return false;
    }
}