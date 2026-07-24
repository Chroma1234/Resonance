using System;
using System.Collections.Generic;
using UnityEngine;

public class ResonanceSaveManager : MonoBehaviour
{
    public static ResonanceSaveManager Instance { get; private set; }

    [Header("Existing Resonance References")]
    [SerializeField] private ConfigurationProfile startingProfile;
    [SerializeField] private InstrumentStemDatabase stemDatabase;
    [SerializeField] private SoundManager soundManager;

    [Header("Profile")]
    [SerializeField] private string defaultPlayerName = "Player";
    [SerializeField] private bool persistAcrossScenes = true;

    public ResonancePlayerProfile PlayerProfile { get; private set; }
    public ConfigurationProfile ActiveConfiguration { get; private set; }
    public string ActiveConfigurationId { get; private set; } = string.Empty;

    private ConfigurationProfile runtimeLoadedProfile;

    private void Awake()
    {
        if (persistAcrossScenes)
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        if (soundManager == null)
            soundManager = SoundManager.Instance;

        LoadOrCreatePlayerProfile();
    }

    private void Start()
    {
        if (startingProfile != null)
            StartConfiguration(startingProfile, string.Empty);
    }

    public void StartConfiguration(
        ConfigurationProfile profile,
        string configurationId)
    {
        if (profile == null)
        {
            Debug.LogWarning("Cannot start a null ConfigurationProfile.");
            return;
        }

        ActiveConfiguration = profile;
        ActiveConfigurationId = configurationId ?? string.Empty;

        if (soundManager == null)
            soundManager = SoundManager.Instance;

        soundManager?.InitializeSession(profile);
    }

    public bool SaveActiveAsNew(string displayName)
    {
        if (ActiveConfiguration == null)
            return Fail("No active ConfigurationProfile exists.");

        string id = Guid.NewGuid().ToString("N");

        SavedConfiguration save =
            ConfigurationProfileSaveConverter.Capture(
                ActiveConfiguration,
                id,
                displayName,
                string.Empty);

        if (!JsonSaveFile.Save(
                JsonSaveFile.ConfigurationPath(id),
                save,
                out string error))
        {
            return Fail(error);
        }

        PlayerProfile.configurations.Add(new SavedConfigurationEntry
        {
            id = save.id,
            displayName = save.displayName,
            createdUtc = save.createdUtc,
            modifiedUtc = save.modifiedUtc
        });

        PlayerProfile.statistics.configurationsCreated++;
        SavePlayerProfile();

        ActiveConfigurationId = id;
        return true;
    }

    public bool OverwriteActive(string displayName = null)
    {
        if (string.IsNullOrWhiteSpace(ActiveConfigurationId))
            return Fail("The active configuration has not been saved yet.");

        SavedConfigurationEntry entry =
            PlayerProfile.FindConfiguration(ActiveConfigurationId);

        if (entry == null)
            return Fail("The active configuration metadata is missing.");

        string finalName = string.IsNullOrWhiteSpace(displayName)
            ? entry.displayName
            : displayName.Trim();

        SavedConfiguration save =
            ConfigurationProfileSaveConverter.Capture(
                ActiveConfiguration,
                entry.id,
                finalName,
                entry.createdUtc);

        if (!JsonSaveFile.Save(
                JsonSaveFile.ConfigurationPath(entry.id),
                save,
                out string error))
        {
            return Fail(error);
        }

        entry.displayName = save.displayName;
        entry.modifiedUtc = save.modifiedUtc;
        SavePlayerProfile();
        return true;
    }

    public bool LoadConfiguration(string id)
    {
        if (!JsonSaveFile.Load(
                JsonSaveFile.ConfigurationPath(id),
                out SavedConfiguration save,
                out string loadMessage))
        {
            return Fail(loadMessage);
        }

        if (!ConfigurationProfileSaveConverter.TryBuildRuntimeProfile(
                save,
                stemDatabase,
                out ConfigurationProfile profile,
                out string conversionError))
        {
            return Fail(conversionError);
        }

        DestroyRuntimeProfile();

        runtimeLoadedProfile = profile;
        StartConfiguration(runtimeLoadedProfile, save.id);

        PlayerProfile.statistics.configurationsLoaded++;
        SavePlayerProfile();

        if (!string.IsNullOrWhiteSpace(loadMessage))
            Debug.LogWarning(loadMessage);

        return true;
    }

    public bool RenameConfiguration(string id, string newName)
    {
        if (string.IsNullOrWhiteSpace(newName))
            return Fail("Enter a configuration name.");

        if (!JsonSaveFile.Load(
                JsonSaveFile.ConfigurationPath(id),
                out SavedConfiguration save,
                out string error))
        {
            return Fail(error);
        }

        save.displayName = newName.Trim();
        save.modifiedUtc = DateTime.UtcNow.ToString("o");

        if (!JsonSaveFile.Save(
                JsonSaveFile.ConfigurationPath(id),
                save,
                out error))
        {
            return Fail(error);
        }

        SavedConfigurationEntry entry =
            PlayerProfile.FindConfiguration(id);

        if (entry != null)
        {
            entry.displayName = save.displayName;
            entry.modifiedUtc = save.modifiedUtc;
        }

        SavePlayerProfile();
        return true;
    }

    public bool DuplicateConfiguration(string id, string newName)
    {
        if (!JsonSaveFile.Load(
                JsonSaveFile.ConfigurationPath(id),
                out SavedConfiguration source,
                out string error))
        {
            return Fail(error);
        }

        string newId = Guid.NewGuid().ToString("N");
        string now = DateTime.UtcNow.ToString("o");

        source.id = newId;
        source.displayName = string.IsNullOrWhiteSpace(newName)
            ? source.displayName + " Copy"
            : newName.Trim();
        source.createdUtc = now;
        source.modifiedUtc = now;

        if (!JsonSaveFile.Save(
                JsonSaveFile.ConfigurationPath(newId),
                source,
                out error))
        {
            return Fail(error);
        }

        PlayerProfile.configurations.Add(new SavedConfigurationEntry
        {
            id = source.id,
            displayName = source.displayName,
            createdUtc = source.createdUtc,
            modifiedUtc = source.modifiedUtc
        });

        PlayerProfile.statistics.configurationsCreated++;
        SavePlayerProfile();
        return true;
    }

    public bool DeleteConfiguration(string id)
    {
        if (!JsonSaveFile.Delete(
                JsonSaveFile.ConfigurationPath(id),
                out string error))
        {
            return Fail(error);
        }

        PlayerProfile.configurations.RemoveAll(item => item.id == id);

        if (ActiveConfigurationId == id)
            ActiveConfigurationId = string.Empty;

        SavePlayerProfile();
        return true;
    }

    public IReadOnlyList<SavedConfigurationEntry> GetConfigurations() =>
        PlayerProfile.configurations;

    public void AddSessionRecord(SessionRecord session)
    {
        PlayerProfile.sessions.Add(session);
        PlayerProfile.statistics.totalPlaytimeSeconds += session.durationSeconds;
        PlayerProfile.statistics.longestSessionSeconds = Mathf.Max(
            PlayerProfile.statistics.longestSessionSeconds,
            session.durationSeconds);

        if (session.completed)
            PlayerProfile.statistics.sessionsCompleted++;

        PlayerProfile.statistics.duetActivations += session.duetActivations;
        SavePlayerProfile();
    }

    public void SavePlayerProfile()
    {
        PlayerProfile.modifiedUtc = DateTime.UtcNow.ToString("o");

        if (!JsonSaveFile.Save(
                JsonSaveFile.ProfilePath,
                PlayerProfile,
                out string error))
        {
            Debug.LogError($"Profile save failed: {error}");
        }
    }

    private void LoadOrCreatePlayerProfile()
    {
        if (JsonSaveFile.Load(
                JsonSaveFile.ProfilePath,
                out ResonancePlayerProfile loaded,
                out _))
        {
            PlayerProfile = loaded;
            return;
        }

        string now = DateTime.UtcNow.ToString("o");

        PlayerProfile = new ResonancePlayerProfile
        {
            playerId = Guid.NewGuid().ToString("N"),
            playerName = defaultPlayerName,
            createdUtc = now,
            modifiedUtc = now
        };

        SavePlayerProfile();
    }

    private void DestroyRuntimeProfile()
    {
        if (runtimeLoadedProfile != null)
            Destroy(runtimeLoadedProfile);

        runtimeLoadedProfile = null;
    }

    private bool Fail(string message)
    {
        Debug.LogError($"[ResonanceSaveManager] {message}");
        return false;
    }

    private void OnApplicationPause(bool paused)
    {
        if (paused)
            SavePlayerProfile();
    }

    private void OnApplicationQuit()
    {
        SavePlayerProfile();
        DestroyRuntimeProfile();
    }

    [ContextMenu("Print Save Folder")]
    private void PrintSaveFolder() =>
        Debug.Log(JsonSaveFile.RootPath);
}
