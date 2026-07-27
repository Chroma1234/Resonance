using System;
using System.Collections;
using System.IO;
using FMOD;
using FMOD.Studio;
using FMODUnity;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// These aliases prevent naming conflicts
// between Unity and FMOD classes.
using Debug = UnityEngine.Debug;
using FmodSound = FMOD.Sound;
using FmodChannel = FMOD.Channel;
using StudioBus = FMOD.Studio.Bus;

// This script manages the saved recording library.
//
// Responsibilities:
// - Find all saved WAV recordings.
// - Create a button for each recording.
// - Play selected recordings.
// - Highlight the selected recording.
// - Delete saved recordings.
// - Pause and resume FMOD Studio audio.
public class SavedRecordingLibrary : MonoBehaviour
{
    [Header("Recording List")]

    // Parent object that holds
    // all dynamically created recording buttons.
    [SerializeField]
    private Transform recordingListContent;

    // Button prefab used to create
    // one button for every saved recording.
    [SerializeField]
    private UnityEngine.UI.Button recordingButtonPrefab;

    [Header("Selection Highlight")]

    // Colour used when a recording
    // button is not selected.
    [SerializeField]
    private Color normalButtonColor = Color.white;

    // Colour used to clearly show
    // which recording is currently selected.
    [SerializeField]
    private Color selectedButtonColor = Color.green;

    [Header("Folder Path")]

    // Text that displays the full folder path
    // where WAV recordings are saved.
    [SerializeField]
    private TMP_Text folderPathText;

    // Reference to FMOD Studio's Master Bus.
    //
    // This is paused when a saved recording
    // is being played so both audio sources
    // do not play at the same time.
    private StudioBus studioMasterBus;

    // Stores the WAV sound
    // currently loaded into FMOD.
    private FmodSound loadedRecordingSound;

    // FMOD Core channel used
    // to play the loaded WAV file.
    private FmodChannel recordingChannel;

    // Stores the active playback coroutine
    // so it can be stopped when required.
    private Coroutine playbackCoroutine;

    // Stores the full file path
    // of the selected recording.
    private string selectedRecordingPath;

    // Stores the selected recording button
    // so its colour can be updated.
    private UnityEngine.UI.Button selectedRecordingButton;

    private void Start()
    {
        // Get the FMOD Studio Master Bus.
        //
        // The path "bus:/" represents
        // the main Master Bus.
        studioMasterBus =
            RuntimeManager.GetBus("bus:/");

        // Search the save folder
        // and create the recording buttons.
        RefreshRecordingList();
    }

    // Reloads all saved WAV recordings
    // and displays them as buttons.
    public void RefreshRecordingList()
    {
        // Ensure the list container
        // has been assigned in the Inspector.
        if (recordingListContent == null)
        {
            Debug.LogError(
                "Recording List Content is not assigned."
            );

            return;
        }

        // Ensure the recording button prefab
        // has been assigned in the Inspector.
        if (recordingButtonPrefab == null)
        {
            Debug.LogError(
                "Recording Button Prefab is not assigned."
            );

            return;
        }

        // Remove all old recording buttons
        // before rebuilding the list.
        ClearRecordingButtons();

        // Get Unity's persistent storage folder.
        //
        // This is the same folder used
        // by the recording script
        // when saving WAV files.
        string folderPath =
            Application.persistentDataPath;

        // Display the complete folder path
        // so the user knows where
        // recordings are stored.
        if (folderPathText != null)
        {
            folderPathText.text =
                "Save Location:\n" +
                folderPath;

            folderPathText.gameObject.SetActive(true);
        }
        else
        {
            Debug.LogWarning(
                "Folder Path Text is not assigned."
            );
        }

        // Create the storage folder
        // if it does not already exist.
        if (!Directory.Exists(folderPath))
        {
            Directory.CreateDirectory(folderPath);
        }

        // Find all files with the .wav extension
        // inside the recording folder.
        string[] recordingFiles =
            Directory.GetFiles(
                folderPath,
                "*.wav"
            );

        // Sort the files by their modified time
        // so the newest recording appears first.
        Array.Sort(
            recordingFiles,
            delegate (
                string firstFile,
                string secondFile
            )
            {
                return File
                    .GetLastWriteTime(secondFile)
                    .CompareTo(
                        File.GetLastWriteTime(firstFile)
                    );
            }
        );

        // Create one UI button
        // for every saved recording.
        foreach (string filePath in recordingFiles)
        {
            CreateRecordingButton(filePath);
        }

        // Print useful information
        // inside the Unity Console.
        Debug.Log(
            recordingFiles.Length +
            " saved recording(s) found.\n" +
            "Folder location:\n" +
            folderPath
        );
    }
    // Creates one UI button
    // for a saved recording.
    private void CreateRecordingButton(
        string filePath
    )
    {
        // Create a new button
        // inside the recording list container.
        UnityEngine.UI.Button newButton =
            Instantiate(
                recordingButtonPrefab,
                recordingListContent
            );

        // Remove the file extension
        // so only the recording name
        // is shown on the button.
        string recordingName =
            Path.GetFileNameWithoutExtension(
                filePath
            );

        // Try to find a TextMeshPro text component
        // inside the button prefab.
        TMP_Text tmpText =
            newButton.GetComponentInChildren<TMP_Text>();

        if (tmpText != null)
        {
            // Display the recording name.
            tmpText.text = recordingName;
        }
        else
        {
            // Use the older Unity Text component
            // if TextMeshPro is not available.
            Text normalText =
                newButton.GetComponentInChildren<Text>();

            if (normalText != null)
            {
                normalText.text = recordingName;
            }
        }

        // Give the new button
        // its normal unselected colour.
        SetButtonColor(
            newButton,
            normalButtonColor
        );

        /*
         * Store separate local copies
         * of the file path and button.
         *
         * These are used inside
         * the button's click listener.
         */
        string selectedFilePath = filePath;

        UnityEngine.UI.Button selectedButton =
            newButton;

        // Remove any listeners
        // already attached to the prefab.
        newButton.onClick.RemoveAllListeners();

        // Add a new listener.
        //
        // When clicked, this button
        // selects and plays its recording.
        newButton.onClick.AddListener(
            delegate
            {
                SelectRecording(
                    selectedFilePath,
                    selectedButton
                );
            }
        );
    }

    // Removes all recording buttons
    // currently displayed in the list.
    private void ClearRecordingButtons()
    {
        // Loop through every child object
        // inside the list container.
        foreach (
            Transform child
            in recordingListContent
        )
        {
            // Remove the old button.
            Destroy(child.gameObject);
        }

        // Clear the previous selection
        // because the list is being rebuilt.
        selectedRecordingButton = null;
        selectedRecordingPath = null;
    }

    // Selects and begins playing
    // a saved recording.
    public void SelectRecording(
        string filePath,
        UnityEngine.UI.Button clickedButton
    )
    {
        // Return the previously selected button
        // to its normal colour.
        if (selectedRecordingButton != null)
        {
            SetButtonColor(
                selectedRecordingButton,
                normalButtonColor
            );
        }

        // Store the newly selected
        // recording path and button.
        selectedRecordingPath = filePath;
        selectedRecordingButton = clickedButton;

        // Highlight the selected button
        // so the user can see
        // which recording is active.
        SetButtonColor(
            selectedRecordingButton,
            selectedButtonColor
        );

        // Stop any recording
        // that may already be playing.
        StopSelectedRecording();

        // Start loading and playing
        // the newly selected WAV file.
        playbackCoroutine =
            StartCoroutine(
                PlayRecording(filePath)
            );
    }

    // Changes a button's visual colour.
    private void SetButtonColor(
        UnityEngine.UI.Button button,
        Color colour
    )
    {
        // Prevent errors if
        // the button reference is missing.
        if (button == null)
        {
            return;
        }

        // Copy the button's
        // current colour settings.
        ColorBlock colours =
            button.colors;

        /*
         * Apply the same colour
         * to the main button states.
         *
         * This keeps the selected button
         * visibly highlighted even when
         * the mouse is hovering over it.
         */
        colours.normalColor = colour;
        colours.selectedColor = colour;
        colours.highlightedColor = colour;

        // Apply the updated colours
        // back to the button.
        button.colors = colours;

        // Directly update the target image
        // so the colour changes immediately.
        if (button.targetGraphic != null)
        {
            button.targetGraphic.color =
                colour;
        }
    }
    // Loads and plays
    // the selected WAV recording.
    private IEnumerator PlayRecording(
        string filePath
    )
    {
        // Make sure the recording
        // file still exists.
        if (!File.Exists(filePath))
        {
            Debug.LogError(
                "Recording file was not found:\n" +
                filePath
            );

            playbackCoroutine = null;
            yield break;
        }

        // Wait until the recording file
        // is no longer being written.
        float waitTime = 0f;

        while (IsFileLocked(filePath))
        {
            waitTime += 0.1f;

            // Stop waiting after 5 seconds.
            if (waitTime >= 5f)
            {
                Debug.LogError(
                    "The recording file is still being written:\n" +
                    filePath
                );

                playbackCoroutine = null;
                yield break;
            }

            // Wait briefly before checking again.
            yield return new WaitForSecondsRealtime(0.1f);
        }

        // Load the WAV file into
        // FMOD Core as a streaming sound.
        RESULT result =
            RuntimeManager.CoreSystem.createSound(
                filePath,
                MODE.DEFAULT |
                MODE.CREATESTREAM |
                MODE._2D,
                out loadedRecordingSound
            );

        // Stop if loading failed.
        if (result != RESULT.OK)
        {
            Debug.LogError(
                "FMOD could not load the recording:\n" +
                result +
                "\n" +
                filePath
            );

            loadedRecordingSound.clearHandle();
            playbackCoroutine = null;

            yield break;
        }

        // Pause all FMOD Studio audio
        // so only the recording plays.
        if (studioMasterBus.isValid())
        {
            studioMasterBus.setPaused(true);
        }

        // Play the recording
        // through FMOD Core.
        result =
            RuntimeManager.CoreSystem.playSound(
                loadedRecordingSound,
                default(ChannelGroup),
                false,
                out recordingChannel
            );

        // Stop if playback failed.
        if (result != RESULT.OK)
        {
            Debug.LogError(
                "FMOD could not play the recording:\n" +
                result
            );

            ResumeStudioAudio();
            ReleaseRecordingSound();

            playbackCoroutine = null;

            yield break;
        }

        Debug.Log(
            "Playing saved recording:\n" +
            Path.GetFileName(filePath)
        );

        // Track whether the recording
        // is still playing.
        bool isPlaying = true;

        // Wait until playback finishes.
        while (isPlaying)
        {
            // Stop if the channel
            // is no longer valid.
            if (!recordingChannel.hasHandle())
            {
                break;
            }

            result =
                recordingChannel.isPlaying(
                    out isPlaying
                );

            // Exit if FMOD reports an error.
            if (result != RESULT.OK)
            {
                break;
            }

            // Wait until the next frame.
            yield return null;
        }

        // Clear the playback channel.
        recordingChannel.clearHandle();

        // Release the WAV file
        // from FMOD memory.
        ReleaseRecordingSound();

        // Resume the game's
        // original FMOD Studio audio.
        ResumeStudioAudio();

        // Mark playback as finished.
        playbackCoroutine = null;

        Debug.Log(
            "Recording finished. FMOD Studio audio resumed."
        );
    }

    // Checks whether the WAV file
    // is still being used by another process.
    //
    // This prevents the script from trying
    // to play a file that is still being saved.
    private bool IsFileLocked(
        string filePath
    )
    {
        try
        {
            // Try to open the file for reading.
            //
            // FileShare.Read allows other systems
            // to read the file at the same time.
            using (FileStream stream = new FileStream(
                filePath,
                FileMode.Open,
                FileAccess.Read,
                FileShare.Read))
            {
                // If opening succeeds,
                // the file is ready to use.
                return false;
            }
        }
        catch (IOException)
        {
            // An IOException usually means
            // the file is still being written
            // or is currently locked.
            return true;
        }
    }

    // Stops the recording
    // that is currently playing.
    public void StopSelectedRecording()
    {
        // Stop the playback coroutine
        // if it is currently running.
        if (playbackCoroutine != null)
        {
            StopCoroutine(playbackCoroutine);
            playbackCoroutine = null;
        }

        // Stop the FMOD playback channel.
        if (recordingChannel.hasHandle())
        {
            recordingChannel.stop();
            recordingChannel.clearHandle();
        }

        // Release the loaded WAV file
        // from FMOD memory.
        ReleaseRecordingSound();

        // Resume the game's original
        // FMOD Studio audio.
        ResumeStudioAudio();
    }

    // Deletes the currently selected
    // recording from the computer.
    public void DeleteSelectedRecording()
    {
        // Prevent deletion if the user
        // has not selected a recording.
        if (string.IsNullOrEmpty(
            selectedRecordingPath))
        {
            Debug.LogWarning(
                "Select a recording before deleting."
            );

            return;
        }

        // Stop playback before deleting
        // so the file is no longer in use.
        StopSelectedRecording();

        // Check that the file still exists.
        if (File.Exists(selectedRecordingPath))
        {
            // Delete the WAV file
            // from persistent storage.
            File.Delete(selectedRecordingPath);

            Debug.Log(
                "Deleted recording:\n" +
                Path.GetFileName(
                    selectedRecordingPath
                )
            );
        }
        else
        {
            Debug.LogWarning(
                "The selected recording could not be found."
            );
        }

        // Clear the current selection.
        selectedRecordingPath = null;
        selectedRecordingButton = null;

        // Rebuild the recording list
        // so the deleted button disappears.
        RefreshRecordingList();
    }
    // Releases the currently loaded
    // WAV recording from FMOD memory.
    //
    // This frees memory after playback
    // has finished or been stopped.
    private void ReleaseRecordingSound()
    {
        // Check that the sound
        // is still valid.
        if (loadedRecordingSound.hasHandle())
        {
            // Release the sound resource.
            loadedRecordingSound.release();

            // Clear the handle so it
            // cannot be used again.
            loadedRecordingSound.clearHandle();
        }
    }

    // Resumes the original
    // FMOD Studio audio.
    //
    // This is called after a recording
    // finishes playing or is stopped.
    private void ResumeStudioAudio()
    {
        // Make sure the Master Bus
        // is still valid.
        if (studioMasterBus.isValid())
        {
            // Resume all FMOD Studio events.
            studioMasterBus.setPaused(false);
        }
    }

    // Automatically called when
    // this GameObject is disabled.
    //
    // Stops any recording playback
    // to prevent audio continuing
    // in the background.
    private void OnDisable()
    {
        StopSelectedRecording();
    }

    // Automatically called when
    // this GameObject is destroyed.
    //
    // Performs final cleanup
    // before the object is removed.
    private void OnDestroy()
    {
        StopSelectedRecording();
    }
}