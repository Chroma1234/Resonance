using System;
using System.Collections.Generic;

[Serializable]
public class SavedInstrumentConfig
{
    public string instrumentName;
    public string mood;
}

[Serializable]
public class SavedConfiguration
{
    public string id;
    public string displayName;
    public string createdUtc;
    public string modifiedUtc;

    public List<SavedInstrumentConfig> instruments =
        new List<SavedInstrumentConfig>();
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
public class ResonanceStatistics
{
    public int sessionsPlayed;
    public int configurationsSaved;
    public int configurationsLoaded;
    public float totalPlaytimeSeconds;
    public int duetsTriggered;
}