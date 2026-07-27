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
    private UnityEngine.UI.Button recordingButtonPrefab;

    [Header("Selection Highlight")]
    [SerializeField]
    private Color normalButtonColor = Color.white;

    [SerializeField]
    private Color selectedButtonColor = Color.green;

    [Header("Folder Path")]
    [SerializeField]
    private TMP_Text folderPathText;

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
    private UnityEngine.UI.Button selectedRecordingButton;

    private void Start()
    {
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

        // Display the complete recording folder location.
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
            " saved recording(s) found.\n" +
            "Folder location:\n" +
            folderPath
        );
    }

    private void CreateRecordingButton(
        string filePath
    )
    {
        UnityEngine.UI.Button newButton =
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

        SetButtonColor(
            newButton,
            normalButtonColor
        );

        string selectedFilePath = filePath;
        UnityEngine.UI.Button selectedButton = newButton;

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
        UnityEngine.UI.Button clickedButton
    )
    {
        if (selectedRecordingButton != null)
        {
            SetButtonColor(
                selectedRecordingButton,
                normalButtonColor
            );
        }

        selectedRecordingPath = filePath;
        selectedRecordingButton = clickedButton;

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
        UnityEngine.UI.Button button,
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