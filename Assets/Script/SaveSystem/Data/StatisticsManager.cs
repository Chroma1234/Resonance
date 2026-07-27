using System;
using System.IO;
using UnityEngine;

public class StatisticsManager : MonoBehaviour
{
    public static StatisticsManager Instance { get; private set; }

    [Header("Debug")]
    [SerializeField] private bool debugLogs = true;

    private PlayerStatisticsData statistics;
    private string statisticsFilePath;

    private float sessionStartTime;
    private bool sessionActive;

    public PlayerStatisticsData Statistics => statistics;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        statisticsFilePath = Path.Combine(
            Application.persistentDataPath,
            "statistics.json"
        );

        LoadOrCreateStatistics();
        StartSession();
    }

    private void LoadOrCreateStatistics()
    {
        Directory.CreateDirectory(Application.persistentDataPath);

        if (!File.Exists(statisticsFilePath))
        {
            statistics = new PlayerStatisticsData();
            SaveStatistics();

            Log("Created new statistics file.");
            return;
        }

        try
        {
            string json = File.ReadAllText(statisticsFilePath);
            statistics = JsonUtility.FromJson<PlayerStatisticsData>(json);

            if (statistics == null)
            {
                statistics = new PlayerStatisticsData();
                SaveStatistics();
            }

            Log("Loaded statistics file.");
        }
        catch (Exception exception)
        {
            Debug.LogError(
                "[StatisticsManager] Failed to load statistics: " +
                exception.Message
            );

            statistics = new PlayerStatisticsData();
            SaveStatistics();
        }
    }

    public void SaveStatistics()
    {
        if (statistics == null)
            statistics = new PlayerStatisticsData();

        try
        {
            statistics.lastPlayedUtc = DateTime.UtcNow.ToString("O");

            string json = JsonUtility.ToJson(statistics, true);

            Directory.CreateDirectory(Application.persistentDataPath);
            File.WriteAllText(statisticsFilePath, json);

            Log("Statistics saved to: " + statisticsFilePath);
            Log("Statistics JSON:\n" + json);
        }
        catch (Exception exception)
        {
            Debug.LogError(
                "[StatisticsManager] Failed to save statistics: " +
                exception.Message
            );
        }
    }

    private void StartSession()
    {
        sessionStartTime = Time.realtimeSinceStartup;
        sessionActive = true;

        statistics.totalSessions++;
        SaveStatistics();
    }

    private void EndSession()
    {
        if (!sessionActive)
            return;

        float sessionDuration =
            Time.realtimeSinceStartup - sessionStartTime;

        statistics.totalPlayTimeSeconds += sessionDuration;
        sessionActive = false;

        SaveStatistics();
    }

    public void RecordConfigurationCreated()
    {
        statistics.configurationsCreated++;
        SaveStatistics();
    }

    public void RecordConfigurationLoaded()
    {
        statistics.configurationsLoaded++;
        SaveStatistics();
    }

    public void RecordConfigurationOverwritten()
    {
        statistics.configurationsOverwritten++;
        SaveStatistics();
    }

    public void RecordConfigurationDeleted()
    {
        statistics.configurationsDeleted++;
        SaveStatistics();
    }

    public void RecordInstrumentActivated()
    {
        statistics.instrumentsActivated++;
        SaveStatistics();
    }

    public void RecordDuetTriggered()
    {
        statistics.duetsTriggered++;
        SaveStatistics();
    }

    private void OnApplicationPause(bool paused)
    {
        if (paused)
        {
            UpdateCurrentPlayTime();
            SaveStatistics();
        }
    }

    private void OnApplicationQuit()
    {
        EndSession();
    }

    private void UpdateCurrentPlayTime()
    {
        if (!sessionActive)
            return;

        float currentDuration =
            Time.realtimeSinceStartup - sessionStartTime;

        statistics.totalPlayTimeSeconds += currentDuration;
        sessionStartTime = Time.realtimeSinceStartup;
    }

    [ContextMenu("Save Statistics")]
    private void SaveStatisticsFromInspector()
    {
        UpdateCurrentPlayTime();
        SaveStatistics();
    }

    [ContextMenu("Open Statistics Folder")]
    private void OpenStatisticsFolder()
    {
        Directory.CreateDirectory(Application.persistentDataPath);

#if UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN
        System.Diagnostics.Process.Start(
            "explorer.exe",
            Application.persistentDataPath.Replace("/", "\\")
        );
#else
        Application.OpenURL(
            "file://" + Application.persistentDataPath
        );
#endif
    }

    [ContextMenu("Reset Statistics")]
    private void ResetStatistics()
    {
        statistics = new PlayerStatisticsData();
        sessionStartTime = Time.realtimeSinceStartup;
        sessionActive = true;

        SaveStatistics();

        Log("Statistics reset.");
    }

    private void Log(string message)
    {
        if (debugLogs)
            Debug.Log("[StatisticsManager] " + message);
    }
}