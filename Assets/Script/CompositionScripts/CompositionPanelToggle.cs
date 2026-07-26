using FMOD.Studio;
using FMODUnity;
using UnityEngine;

public class CompositionPanelToggle : MonoBehaviour
{
    [Header("Composition Panel")]
    [SerializeField] private GameObject compositionPanel;

    [Header("Composition Player")]
    [SerializeField] private CompositionPlayer compositionPlayer;

    [Header("Player")]
    [SerializeField] private PlayerMovement playerMovement;
    [SerializeField] private MouseLook mouseLook;

    [SerializeField]
    private SavedRecordingLibrary savedRecordingLibrary;

    private Bus musicBus;
    private Bus chordsBus;

    private void Awake()
    {
        musicBus = RuntimeManager.GetBus("bus:/Music");
        chordsBus = RuntimeManager.GetBus("bus:/ChordsComposition");

        if (compositionPanel != null)
        {
            compositionPanel.SetActive(false);
        }
    }

    private void Start()
    {
        ResumeMusic();
        StopChordAudio();
        EnablePlayerControls();

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    // Connect this to the Open button.
    public void OpenCompositionPanel()
    {
        if (compositionPanel == null)
        {
            Debug.LogError("Composition Panel is not assigned.");
            return;
        }

        // Stop any WAV that is currently playing.
        if (savedRecordingLibrary != null)
        {
            savedRecordingLibrary.StopSelectedRecording();
        }

        compositionPanel.SetActive(true);

        PauseMusic();
        ResumeChordBus();
        DisablePlayerControls();

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    // Connect this to the Close button.
    public void CloseCompositionPanel()
    {
        if (compositionPanel == null)
        {
            Debug.LogError("Composition Panel is not assigned.");
            return;
        }

        // Stop playback and clear every chord slot.
        if (compositionPlayer != null)
        {
            compositionPlayer.ClearComposition();
        }
        else
        {
            Debug.LogWarning("Composition Player is not assigned.");
        }

        // Extra safety for any remaining FMOD chord event.
        StopChordAudio();

        ResumeMusic();

        compositionPanel.SetActive(false);

        EnablePlayerControls();

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    private void PauseMusic()
    {
        RefreshMusicBus();

        if (musicBus.isValid())
        {
            musicBus.setPaused(true);
        }
    }

    private void ResumeMusic()
    {
        RefreshMusicBus();

        if (musicBus.isValid())
        {
            musicBus.setPaused(false);
        }
    }

    private void ResumeChordBus()
    {
        RefreshChordBus();

        if (chordsBus.isValid())
        {
            chordsBus.setPaused(false);
        }
    }

    private void StopChordAudio()
    {
        RefreshChordBus();

        if (chordsBus.isValid())
        {
            chordsBus.stopAllEvents(
                FMOD.Studio.STOP_MODE.IMMEDIATE
            );

            chordsBus.setPaused(false);
        }
    }

    private void DisablePlayerControls()
    {
        if (playerMovement != null)
        {
            playerMovement.enabled = false;
        }

        if (mouseLook != null)
        {
            mouseLook.enabled = false;
        }
    }

    private void EnablePlayerControls()
    {
        if (playerMovement != null)
        {
            playerMovement.enabled = true;
        }

        if (mouseLook != null)
        {
            mouseLook.enabled = true;
        }
    }

    private void RefreshMusicBus()
    {
        if (!musicBus.isValid())
        {
            musicBus = RuntimeManager.GetBus("bus:/Music");
        }
    }

    private void RefreshChordBus()
    {
        if (!chordsBus.isValid())
        {
            chordsBus = RuntimeManager.GetBus(
                "bus:/ChordsComposition"
            );
        }
    }
}