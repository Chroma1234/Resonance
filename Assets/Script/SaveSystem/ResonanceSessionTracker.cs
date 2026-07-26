using System;
using UnityEngine;

public class ResonanceSessionTracker : MonoBehaviour
{
    public static ResonanceSessionTracker Instance { get; private set; }

    public ResonanceStatistics Statistics { get; private set; }

    private DateTime sessionStartUtc;
    private bool sessionActive;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this);
            return;
        }

        Instance = this;
        LoadStatistics();
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    public void BeginSession()
    {
        if (sessionActive)
        {
            Debug.LogWarning("ResonanceSessionTracker: BeginSession called while a session is already active. Ignoring.");
            return;
        }

        sessionStartUtc = DateTime.UtcNow;
        sessionActive = true;
        Debug.Log("ResonanceSessionTracker: Session started.");
    }

    public void EndSession()
    {
        if (!sessionActive)
        {
            return;
        }

        float elapsedSeconds = (float)(DateTime.UtcNow - sessionStartUtc).TotalSeconds;

        Statistics.sessionsPlayed++;
        Statistics.totalPlaytimeSeconds += elapsedSeconds;
        sessionActive = false;

        SaveStatistics();
        Debug.Log("ResonanceSessionTracker: Session ended after " + elapsedSeconds.ToString("F1") + " seconds.");
    }

    public void RecordDuetActivated()
    {
        Statistics.duetsTriggered++;
        SaveStatistics();
    }

    public void RecordConfigurationSaved()
    {
        Statistics.configurationsSaved++;
        SaveStatistics();
    }

    public void RecordConfigurationLoaded()
    {
        Statistics.configurationsLoaded++;
        SaveStatistics();
    }

    private void LoadStatistics()
    {
        ResonanceStatistics loaded;
        string error;
        if (JsonSaveFile.Load(JsonSaveFile.StatisticsPath, out loaded, out error))
        {
            Statistics = loaded;
            if (!string.IsNullOrEmpty(error))
            {
                Debug.LogWarning("ResonanceSessionTracker: " + error);
            }
        }
        else
        {
            Statistics = new ResonanceStatistics();
            Debug.Log("ResonanceSessionTracker: No statistics file found. Starting a fresh statistics record.");
        }
    }

    private void SaveStatistics()
    {
        string error;
        if (!JsonSaveFile.Save(JsonSaveFile.StatisticsPath, Statistics, out error))
        {
            Debug.LogError("ResonanceSessionTracker: Failed to save statistics: " + error);
        }
    }

    private void OnApplicationQuit()
    {
        EndSession();
    }
}
