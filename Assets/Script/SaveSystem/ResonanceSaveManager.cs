using System;
using System.IO;
using System.Collections.Generic;
using UnityEngine;

// Instead of allowing gameplay systems to directly read or write files,
// all save operations are routed through this manager.
//
// Main Responsibilities:
// Capture the player's current instrument configuration
// Save configurations as JSON files
// Load existing configurations
// Rename, duplicate and delete saves
// Keep track of the currently active configuration
// Notify the statistics system of successful save/load operations
//
// Design Principles:
// • Single Responsibility Principle
// • Separation of Concerns
// • Singleton Pattern
//
// This class DOES NOT:
// Perform JSON serialization itself
// Control audio playback
// Modify instrument moods directly
//
// Those responsibilities belong to JsonSaveFile,
// InstrumentMoodSaveConverter and MoodManager respectively.

public class ResonanceSaveManager : MonoBehaviour
{
    public static ResonanceSaveManager Instance { get; private set; }

    //SINGLETON
    // There should only ever be ONE SaveManager in the game. (only be one object responsible for persistent storage.
    // Multiple SaveManagers could overwrite each other's save files.)
    //
    // Every UI button or gameplay system accesses:ResonanceSaveManager.Instance instead of creating another SaveManager.
    //
    // Benefits:
    // Prevents duplicate save systems
    // Ensures all saves go through one controller
    // Persists across scene changes

    [Header("Current Resonance References")]
    [SerializeField]
    private InstrumentDatabase instrumentDatabase;

    //INSPECTOR REFERENCES
    // InstrumentDatabase stores every InstrumentData asset in the game.
    //
    // During loading, the save file only contains instrument names. This database is used to convert those names back into actual ScriptableObject references.
    //
    // Without this database, JSON would not know which InstrumentData to restore.

    [Tooltip(
        "Optional. Uses MoodManager.Instance if left empty.")]
    [SerializeField]
    private MoodManager moodManager;
    // MoodManager stores the CURRENT runtime mood of every instrument.
    //
    // SaveManager never changes moods directly.
    //
    // Instead, it asks MoodManager: "What is the player's current configuration?" when saving.
    //
    // During loading, it passes the restored moods back into MoodManager.
    // MoodManager owns runtime mood states.SaveManager only stores them permanently.

    [Tooltip(
        "Optional. Found automatically if left empty.")]
    [SerializeField]
    private ResonanceSessionTracker sessionTracker;

    // Statistics are intentionally separated.
    //
    // SaveManager performs save operations.
    //
    // SessionTracker records analytics.
    //
    // This avoids mixing persistence logic
    // with statistics tracking.

    public string ActiveConfigurationId { get; private set; }

    // Tracks which configuration is currently loaded.
    //
    // Why?
    //
    // Features like:
    // Overwrite
    // Rename
    // Delete Active Configuration
    //
    // need to know exactly
    // which save file is active.

    private void Awake()
    {

<<<<<<< HEAD
=======
        // Responsibilities:
        // 1. Create Singleton
        // 2. Detach from parent if necessary
        // 3. Persist across scenes
        // 4. Resolve missing references
        //
        // Why detach?
        //
        // If this object is parented under another object, destroying the parent would also destroy the SaveManager.
        //
        // By moving it to the root, DontDestroyOnLoad works correctly.
        // Why SetParent(null)? Because DontDestroyOnLoad() only preserves the root object.
        // If this GameObject stayed under another parent that gets destroyed, the SaveManager would also be destroyed.

>>>>>>> parent of f230da6 (Revert "code review notes")
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

        // AUTOMATIC DEPENDENCY RESOLUTION
        // Instead of forcing every reference to be manually assigned,
        // the SaveManager automatically searches for missing systems.
        //
        // Search order: MoodManager.Instance -> GetComponent() -> FindFirstObjectByType()
        //
        // This makes the system more robust and reduces Inspector setup errors.
        // This is called multiple times becausen some systems may not exist during Awake depending on scene loading.
        // Calling it before important operations ensures the latest references are always available.

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
        // Workflow:
        // 1. Verify required systems exist
        // 2. Generate a GUID
        // 3. Capture current runtime configuration
        // 4. Convert runtime data into serialisable data
        // 5. Save JSON file
        // 6. Update active configuration
        // 7. Record statistics
        //
        // Why GUID? (INSTEAD OF FILENAME)
        // Display names are not guaranteed to be unique.
        // GUID guarantees every save file has a permanent unique identifier.
        // Players can rename saves or create duplicate names. A GUID uniquely identifies the save regardless of its display name

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

        // Capture() converts runtime Unity objects
        // into JSON-safe data.
        //
        // Runtime:
        //
        // InstrumentData
        // Mood
        // References
        //
        // ↓
        //
        // SavedConfiguration
        //
        // Only simple values
        // such as strings and enums
        // are stored.
        //
        // This avoids serialising Unity references.

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

        // SaveManager delegates actual file writing to JsonSaveFile.
        //
        // SaveManager decides WHAT to save. JsonSaveFile decides HOW to save.
        //
        // This follows Separation of Concerns.

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

        // Unlike Save As New, Overwrite preserves:
        // Original GUID
        // Original creation date
        //
        // Only:
        // configuration data
        // display name
        // modified time
        // are updated.
        //
        // This keeps save history consistent.

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
        // Workflow:
        //
        // Read JSON -> Deserialize SavedConfiguration -> Restore InstrumentData references -> Restore moods -> Apply to game
        //
        // The SaveManager never directly changes gameplay.
        //
        // Instead, it rebuilds the saved configuration and lets the runtime systems apply it.

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
        // Only metadata changes.
        //
        // Instrument configuration remains untouched.
        //
        // This is much faster than recreating the save file.

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
        // Creates a completely new save.
        //
        // New:
        // GUID
        // Created Time
        // Modified Time
        //
        // Copied:
        // Instrument configuration
        // Mood configuration
        //
        // This behaves like "Save As..." in most software.

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
        // Deletes the JSON save file.
        //
        // If the deleted configuration is currently active,
        //
        // ActiveConfigurationId is cleared to prevent dangling references.

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
        // Rather than storing a separate save list,
        //
        // the SaveManager scans the Save Folder.
        //
        // Every readable JSON file becomes one entry.
        //
        // Advantages:
        // No duplicated metadata
        // Automatically detects new saves
        // Missing files disappear naturally

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
        // Defensive programming.
        //
        // Before saving, verify every required dependency exists.
        //
        // Prevents:
        // NullReferenceExceptions and provides meaningful error messages instead.

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
        // Centralised error handling.
        //
        // Instead of repeating: Debug.LogError(...) throughout the class, every failure passes through one method.
        //
        // Easier to maintain and produces consistent error messages.

        Debug.LogError(
            "[ResonanceSaveManager] " + message);

        return false;
    }

    [ContextMenu("Print Save Folder")]
    private void PrintSaveFolder()
    {
        Debug.Log(JsonSaveFile.RootPath);
    }

    /* ResonanceSaveManager is the central controller that coordinates every save-related operation within the Resonance project.Rather than directly handling
    every responsibility itself, it acts as an orchestrator that connects the different systems involved in persistence.

    Its primary role is to provide a simple interface for the rest of the project. When another script wants to save, load, rename, duplicate or delete a
    configuration, it only needs to call the corresponding public method in ResonanceSaveManager.The caller does not need to understand JSON
    serialization, file paths, or how runtime data is reconstructed.

    For example, when SaveActiveAsNew() is called, the manager first validates that the required systems exist, generates a unique configuration ID, captures
    the current runtime configuration, delegates the file writing to JsonSaveFile, updates the currently active configuration ID, and finally records the save
    through ResonanceSessionTracker.

    Likewise, when LoadConfiguration() is called, the manager retrieves the saved configuration from disk, restores it back into runtime objects through
    InstrumentMoodSaveConverter, updates the active configuration, and records the load operation.It coordinates the entire workflow without directly modifying
    the gameplay systems.

    This class also acts as a protective layer for the rest of the project.Instead of allowing every script to access the file system, all save
    operations are centralised here.This ensures that validation, error handling,logging and statistics recording are always performed consistently.

    Overall, the goal of ResonanceSaveManager is not to perform every save-related  task itself, but to coordinate the entire save workflow through specialised systems. 
    This reduces coupling between gameplay and persistence, improves code maintainability, and makes the save architecture easier to extend as the project grows.
    */

}