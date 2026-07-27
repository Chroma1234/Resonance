using System;
using System.IO;
using System.Collections.Generic;
using UnityEngine;

public class ResonanceSaveManager : MonoBehaviour
{
    public static ResonanceSaveManager Instance { get; private set; }

    [Header("Current Resonance References")]
    [SerializeField]
    private InstrumentDatabase instrumentDatabase;

    [Tooltip(
        "Optional. Uses MoodManager.Instance if left empty.")]
    [SerializeField]
    private MoodManager moodManager;

    [Tooltip(
        "Optional. Found automatically if left empty.")]
    [SerializeField]
    private ResonanceSessionTracker sessionTracker;

    public string ActiveConfigurationId { get; private set; }

    private void Awake()
    {

        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        ActiveConfigurationId = string.Empty;

        if (transform.parent != null)
        {
            transform.SetParent(null);
        }

        DontDestroyOnLoad(gameObject);

        ResolveReferences();
    }

    private void Start()
    {
        ResolveReferences();
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    private void ResolveReferences()
    {
        if (moodManager == null)
        {
            moodManager = MoodManager.Instance;
        }

        if (sessionTracker == null)
        {
            sessionTracker =
                GetComponent<ResonanceSessionTracker>();
        }

        if (sessionTracker == null)
        {
            sessionTracker =
                FindFirstObjectByType<ResonanceSessionTracker>();
        }
    }

    public bool SaveActiveAsNew(string displayName)
    {
        ResolveReferences();

        if (!CanCaptureConfiguration())
        {
            return false;
        }

        string id = Guid.NewGuid().ToString("N");

        SavedConfiguration save =
            InstrumentMoodSaveConverter.Capture(
                instrumentDatabase,
                moodManager,
                id,
                displayName,
                string.Empty);

        if (save == null)
        {
            return Fail(
                "Could not capture the current mood configuration.");
        }

        if (!JsonSaveFile.Save(
                JsonSaveFile.ConfigurationPath(id),
                save,
                out string error))
        {
            return Fail(error);
        }

        ActiveConfigurationId = id;

        if (sessionTracker != null)
        {
            sessionTracker.RecordConfigurationSaved();
        }

        return true;
    }

    public bool OverwriteConfiguration(
        string configurationId,
        string displayName)
    {
        ResolveReferences();

        if (!CanCaptureConfiguration())
        {
            return false;
        }

        if (string.IsNullOrWhiteSpace(configurationId))
        {
            return Fail(
                "No saved configuration was selected.");
        }

        if (!JsonSaveFile.Load(
                JsonSaveFile.ConfigurationPath(configurationId),
                out SavedConfiguration existing,
                out string loadError))
        {
            return Fail(
                "Could not read the selected save: " +
                loadError);
        }

        string finalName =
            string.IsNullOrWhiteSpace(displayName)
                ? existing.displayName
                : displayName.Trim();

        SavedConfiguration save =
            InstrumentMoodSaveConverter.Capture(
                instrumentDatabase,
                moodManager,
                existing.id,
                finalName,
                existing.createdUtc);

        if (save == null)
        {
            return Fail(
                "Could not capture the current configuration.");
        }

        if (!JsonSaveFile.Save(
                JsonSaveFile.ConfigurationPath(existing.id),
                save,
                out string saveError))
        {
            return Fail(saveError);
        }

        ActiveConfigurationId = existing.id;
        return true;
    }

    public bool LoadConfiguration(string configurationId)
    {
        ResolveReferences();

        if (string.IsNullOrWhiteSpace(configurationId))
        {
            return Fail(
                "No saved configuration was selected.");
        }

        if (!JsonSaveFile.Load(
                JsonSaveFile.ConfigurationPath(configurationId),
                out SavedConfiguration save,
                out string loadMessage))
        {
            return Fail(loadMessage);
        }

        if (!InstrumentMoodSaveConverter.TryRestore(
                save,
                instrumentDatabase,
                moodManager,
                out string restoreError))
        {
            return Fail(restoreError);
        }

        ActiveConfigurationId = save.id;

        if (sessionTracker != null)
        {
            sessionTracker.RecordConfigurationLoaded();
        }

        if (!string.IsNullOrEmpty(loadMessage))
        {
            Debug.LogWarning(
                "ResonanceSaveManager: " + loadMessage);
        }

        Debug.Log(
            $"ResonanceSaveManager: Loaded '{save.displayName}'.");

        return true;
    }

    public bool RenameConfiguration(
        string configurationId,
        string newName)
    {
        if (string.IsNullOrWhiteSpace(configurationId))
        {
            return Fail(
                "No saved configuration was selected.");
        }

        if (string.IsNullOrWhiteSpace(newName))
        {
            return Fail(
                "Enter a new configuration name.");
        }

        if (!JsonSaveFile.Load(
                JsonSaveFile.ConfigurationPath(configurationId),
                out SavedConfiguration save,
                out string loadError))
        {
            return Fail(loadError);
        }

        save.displayName = newName.Trim();
        save.modifiedUtc = DateTime.UtcNow.ToString("o");

        if (!JsonSaveFile.Save(
                JsonSaveFile.ConfigurationPath(configurationId),
                save,
                out string saveError))
        {
            return Fail(saveError);
        }

        return true;
    }

    public bool DuplicateConfiguration(
        string configurationId,
        string newName)
    {
        if (string.IsNullOrWhiteSpace(configurationId))
        {
            return Fail(
                "No saved configuration was selected.");
        }

        if (!JsonSaveFile.Load(
                JsonSaveFile.ConfigurationPath(configurationId),
                out SavedConfiguration source,
                out string loadError))
        {
            return Fail(loadError);
        }

        string newId = Guid.NewGuid().ToString("N");
        string now = DateTime.UtcNow.ToString("o");

        source.id = newId;

        source.displayName =
            string.IsNullOrWhiteSpace(newName)
                ? source.displayName + " Copy"
                : newName.Trim();

        source.createdUtc = now;
        source.modifiedUtc = now;

        if (!JsonSaveFile.Save(
                JsonSaveFile.ConfigurationPath(newId),
                source,
                out string saveError))
        {
            return Fail(saveError);
        }

        if (sessionTracker != null)
        {
            sessionTracker.RecordConfigurationSaved();
        }

        return true;
    }

    public bool DeleteConfiguration(string configurationId)
    {
        if (string.IsNullOrWhiteSpace(configurationId))
        {
            return Fail(
                "No saved configuration was selected.");
        }

        if (!JsonSaveFile.Delete(
                JsonSaveFile.ConfigurationPath(configurationId),
                out string error))
        {
            return Fail(error);
        }

        if (ActiveConfigurationId == configurationId)
        {
            ActiveConfigurationId = string.Empty;
        }

        return true;
    }

    public List<SavedConfigurationEntry> GetConfigurations()
    {
        List<SavedConfigurationEntry> entries =
            new List<SavedConfigurationEntry>();

        string[] files =
            JsonSaveFile.ListConfigurationFiles();

        foreach (string path in files)
        {
            if (!JsonSaveFile.Load(
                    path,
                    out SavedConfiguration save,
                    out string error))
            {
                Debug.LogWarning(
                    $"ResonanceSaveManager: " +
                    $"Skipping unreadable save '{path}': {error}");

                continue;
            }

            entries.Add(
                new SavedConfigurationEntry
                {
                    id = save.id,
                    displayName = save.displayName,
                    createdUtc = save.createdUtc,
                    modifiedUtc = save.modifiedUtc
                });
        }

        entries.Sort(CompareByModifiedDescending);
        return entries;
    }

    private bool CanCaptureConfiguration()
    {
        if (instrumentDatabase == null)
        {
            return Fail(
                "InstrumentDatabase is not assigned.");
        }

        if (moodManager == null)
        {
            return Fail(
                "MoodManager does not exist.");
        }

        return true;
    }

    private static int CompareByModifiedDescending(
        SavedConfigurationEntry a,
        SavedConfigurationEntry b)
    {
        return string.CompareOrdinal(
            b.modifiedUtc,
            a.modifiedUtc);
    }

    private bool Fail(string message)
    {
        Debug.LogError(
            "[ResonanceSaveManager] " + message);

        return false;
    }

    [ContextMenu("Print Save Folder")]
    private void PrintSaveFolder()
    {
        Debug.Log(JsonSaveFile.RootPath);
    }
}