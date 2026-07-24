using System;
using System.Collections.Generic;

[Serializable]
public class SavedInstrumentConfig
{
    public string instrumentId;
    public string pattern;
    public string style;
}

[Serializable]
public class SavedConfiguration
{
    public string id;
    public string displayName;
    public string createdUtc;
    public string modifiedUtc;
    public List<SavedInstrumentConfig> instruments = new();
}

[Serializable]
public class SavedConfigurationEntry
{
    public string id;
    public string displayName;
    public string createdUtc;
    public string modifiedUtc;
}

[Serializable]
public class SessionRecord
{
    public string id;
    public string configurationId;
    public string startedUtc;
    public string endedUtc;
    public float durationSeconds;
    public bool completed;
    public int duetActivations;
    public List<string> activatedInstrumentIds = new();
}

[Serializable]
public class ResonanceStatistics
{
    public int sessionsStarted;
    public int sessionsCompleted;
    public float totalPlaytimeSeconds;
    public float longestSessionSeconds;
    public int configurationsCreated;
    public int configurationsLoaded;
    public int duetActivations;
}

[Serializable]
public class ResonanceUnlockData
{
    public List<string> instrumentIds = new();
    public List<string> patternIds = new();
    public List<string> styleIds = new();

    public bool UnlockInstrument(string id) => AddUnique(instrumentIds, id);
    public bool UnlockPattern(string id) => AddUnique(patternIds, id);
    public bool UnlockStyle(string id) => AddUnique(styleIds, id);

    public bool IsInstrumentUnlocked(string id) =>
        !string.IsNullOrWhiteSpace(id) && instrumentIds.Contains(id);

    private static bool AddUnique(List<string> list, string id)
    {
        if (string.IsNullOrWhiteSpace(id) || list.Contains(id))
            return false;

        list.Add(id);
        return true;
    }
}

[Serializable]
public class ResonancePlayerProfile
{
    public const int CurrentVersion = 1;

    public int version = CurrentVersion;
    public string playerId;
    public string playerName = "Player";
    public string createdUtc;
    public string modifiedUtc;

    public ResonanceUnlockData unlocks = new();
    public ResonanceStatistics statistics = new();
    public List<SavedConfigurationEntry> configurations = new();
    public List<SessionRecord> sessions = new();

    public SavedConfigurationEntry FindConfiguration(string id) =>
        configurations.Find(item => item.id == id);
}
