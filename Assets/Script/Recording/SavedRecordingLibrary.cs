using System;
using System.Collections;
using System.IO;
using FMOD;
using FMOD.Studio;
using FMODUnity;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

using Debug = UnityEngine.Debug;
using FmodSound = FMOD.Sound;
using FmodChannel = FMOD.Channel;
using StudioBus = FMOD.Studio.Bus;

public class SavedRecordingLibrary : MonoBehaviour
{
    [Header("Recording List")]
    [SerializeField]
    private Transform recordingListContent;

    [SerializeField]
    private Button recordingButtonPrefab;

    [Header("Selection Highlight")]
    [SerializeField]
    private Color normalButtonColor = Color.white;

    [SerializeField]
    private Color selectedButtonColor = Color.green;

    // The FMOD Studio Master Bus.
    private StudioBus studioMasterBus;

    // The currently loaded external WAV.
    private FmodSound loadedRecordingSound;

    // The FMOD Core channel playing the WAV.
    private FmodChannel recordingChannel;

    private Coroutine playbackCoroutine;

    // Stores the selected recording file.
    private string selectedRecordingPath;

    // Stores the selected button so it can be highlighted.
    private Button selectedRecordingButton;

    private void Start()
    {
        // This is the FMOD Studio Master Bus.
        studioMasterBus =
            RuntimeManager.GetBus("bus:/");

        RefreshRecordingList();
    }

    public void RefreshRecordingList()
    {
        if (recordingListContent == null)
        {
            Debug.LogError(
                "Recording List Content is not assigned."
            );

            return;
        }

        if (recordingButtonPrefab == null)
        {
            Debug.LogError(
                "Recording Button Prefab is not assigned."
            );

            return;
        }

        ClearRecordingButtons();

        string folderPath =
            Application.persistentDataPath;

        if (!Directory.Exists(folderPath))
        {
            Directory.CreateDirectory(folderPath);
        }

        string[] recordingFiles =
            Directory.GetFiles(
                folderPath,
                "*.wav"
            );

        // Show the newest recording first.
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

        foreach (string filePath in recordingFiles)
        {
            CreateRecordingButton(filePath);
        }

        Debug.Log(
            recordingFiles.Length +
            " saved recording(s) found."
        );
    }

    private void CreateRecordingButton(
        string filePath
    )
    {
        Button newButton =
            Instantiate(
                recordingButtonPrefab,
                recordingListContent
            );

        string recordingName =
            Path.GetFileNameWithoutExtension(
                filePath
            );

        TMP_Text tmpText =
            newButton.GetComponentInChildren<TMP_Text>();

        if (tmpText != null)
        {
            tmpText.text = recordingName;
        }
        else
        {
            Text normalText =
                newButton.GetComponentInChildren<Text>();

            if (normalText != null)
            {
                normalText.text = recordingName;
            }
        }

        // Set the normal button colour.
        SetButtonColor(
            newButton,
            normalButtonColor
        );

        string selectedFilePath = filePath;
        Button selectedButton = newButton;

        /*
         * Removes listeners added through code.
         * Keep the prefab's Inspector OnClick list empty.
         */
        newButton.onClick.RemoveAllListeners();

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

    private void ClearRecordingButtons()
    {
        foreach (Transform child in recordingListContent)
        {
            Destroy(child.gameObject);
        }

        selectedRecordingButton = null;
        selectedRecordingPath = null;
    }

    public void SelectRecording(
        string filePath,
        Button clickedButton
    )
    {
        // Return the previous button to its normal colour.
        if (selectedRecordingButton != null)
        {
            SetButtonColor(
                selectedRecordingButton,
                normalButtonColor
            );
        }

        // Remember the newly selected recording.
        selectedRecordingPath = filePath;
        selectedRecordingButton = clickedButton;

        // Highlight the selected recording button.
        SetButtonColor(
            selectedRecordingButton,
            selectedButtonColor
        );

        StopSelectedRecording();

        playbackCoroutine =
            StartCoroutine(
                PlayRecording(filePath)
            );
    }

    private void SetButtonColor(
        Button button,
        Color colour
    )
    {
        if (button == null)
        {
            return;
        }

        ColorBlock colours = button.colors;

        colours.normalColor = colour;
        colours.selectedColor = colour;
        colours.highlightedColor = colour;

        button.colors = colours;

        // Immediately update the visible button colour.
        if (button.targetGraphic != null)
        {
            button.targetGraphic.color = colour;
        }
    }

    private IEnumerator PlayRecording(
        string filePath
    )
    {
        if (!File.Exists(filePath))
        {
            Debug.LogError(
                "Recording file was not found:\n" +
                filePath
            );

            playbackCoroutine = null;
            yield break;
        }

        // Wait briefly if the recorder is still closing the WAV.
        float waitTime = 0f;

        while (IsFileLocked(filePath))
        {
            waitTime += 0.1f;

            if (waitTime >= 5f)
            {
                Debug.LogError(
                    "The recording file is still being written:\n" +
                    filePath
                );

                playbackCoroutine = null;
                yield break;
            }

            yield return new WaitForSecondsRealtime(0.1f);
        }

        /*
         * Load the saved WAV through FMOD Core.
         *
         * CREATESTREAM is suitable for playback directly
         * from a saved audio file.
         */
        RESULT result =
            RuntimeManager.CoreSystem.createSound(
                filePath,
                MODE.DEFAULT |
                MODE.CREATESTREAM |
                MODE._2D,
                out loadedRecordingSound
            );

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

        /*
         * Pause FMOD Studio events.
         *
         * The external WAV is played through FMOD Core,
         * not as an FMOD Studio Event.
         */
        if (studioMasterBus.isValid())
        {
            studioMasterBus.setPaused(true);
        }

        result =
            RuntimeManager.CoreSystem.playSound(
                loadedRecordingSound,
                default(ChannelGroup),
                false,
                out recordingChannel
            );

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

        bool isPlaying = true;

        while (isPlaying)
        {
            if (!recordingChannel.hasHandle())
            {
                break;
            }

            result =
                recordingChannel.isPlaying(
                    out isPlaying
                );

            if (result != RESULT.OK)
            {
                break;
            }

            yield return null;
        }

        recordingChannel.clearHandle();

        ReleaseRecordingSound();
        ResumeStudioAudio();

        playbackCoroutine = null;

        Debug.Log(
            "Recording finished. FMOD Studio audio resumed."
        );
    }

    private bool IsFileLocked(
        string filePath
    )
    {
        try
        {
            using (FileStream stream = new FileStream(
                filePath,
                FileMode.Open,
                FileAccess.Read,
                FileShare.Read))
            {
                return false;
            }
        }
        catch (IOException)
        {
            return true;
        }
    }

    public void StopSelectedRecording()
    {
        if (playbackCoroutine != null)
        {
            StopCoroutine(playbackCoroutine);
            playbackCoroutine = null;
        }

        if (recordingChannel.hasHandle())
        {
            recordingChannel.stop();
            recordingChannel.clearHandle();
        }

        ReleaseRecordingSound();
        ResumeStudioAudio();
    }

    public void DeleteSelectedRecording()
    {
        if (string.IsNullOrEmpty(selectedRecordingPath))
        {
            Debug.LogWarning(
                "Select a recording before deleting."
            );

            return;
        }

        // Stop playback before deleting the WAV.
        StopSelectedRecording();

        if (File.Exists(selectedRecordingPath))
        {
            File.Delete(selectedRecordingPath);

            Debug.Log(
                "Deleted recording:\n" +
                Path.GetFileName(selectedRecordingPath)
            );
        }
        else
        {
            Debug.LogWarning(
                "The selected recording could not be found."
            );
        }

        selectedRecordingPath = null;
        selectedRecordingButton = null;

        RefreshRecordingList();
    }

    private void ReleaseRecordingSound()
    {
        if (loadedRecordingSound.hasHandle())
        {
            loadedRecordingSound.release();
            loadedRecordingSound.clearHandle();
        }
    }

    private void ResumeStudioAudio()
    {
        if (studioMasterBus.isValid())
        {
            studioMasterBus.setPaused(false);
        }
    }

    private void OnDisable()
    {
        StopSelectedRecording();
    }

    private void OnDestroy()
    {
        StopSelectedRecording();
    }
}