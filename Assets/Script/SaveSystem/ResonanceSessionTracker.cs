using System;
using System.Collections.Generic;
using UnityEngine;

public class ResonanceSessionTracker : MonoBehaviour
{
    [SerializeField] private ResonanceSaveManager saveManager;

    private DateTime startedUtc;
    private bool sessionActive;
    private int duetActivations;
    private readonly HashSet<string> activatedInstruments = new();

    public void BeginSession()
    {
        startedUtc = DateTime.UtcNow;
        sessionActive = true;
        duetActivations = 0;
        activatedInstruments.Clear();

        saveManager.PlayerProfile.statistics.sessionsStarted++;
        saveManager.SavePlayerProfile();
    }

    public void RecordInstrumentActivated(string instrumentId)
    {
        if (sessionActive && !string.IsNullOrWhiteSpace(instrumentId))
            activatedInstruments.Add(instrumentId);
    }

    public void RecordDuetActivated()
    {
        if (sessionActive)
            duetActivations++;
    }

    public void EndSession(bool completed)
    {
        if (!sessionActive)
            return;

        DateTime endedUtc = DateTime.UtcNow;

        SessionRecord session = new SessionRecord
        {
            id = Guid.NewGuid().ToString("N"),
            configurationId = saveManager.ActiveConfigurationId,
            startedUtc = startedUtc.ToString("o"),
            endedUtc = endedUtc.ToString("o"),
            durationSeconds =
                (float)(endedUtc - startedUtc).TotalSeconds,
            completed = completed,
            duetActivations = duetActivations,
            activatedInstrumentIds =
                new List<string>(activatedInstruments)
        };

        saveManager.AddSessionRecord(session);
        sessionActive = false;
    }
}
