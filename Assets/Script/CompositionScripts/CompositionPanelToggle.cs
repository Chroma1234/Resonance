using FMOD.Studio;
using FMODUnity;
using UnityEngine;

// This script manages entering and leaving Composition Mode.
//
// Responsibilities:
// - Open and close the composition panel.
// - Pause and resume the environment music.
// - Control the composition audio bus.
// - Enable or disable player movement.
// - Stop any recording playback.
// - Reset the composition when closing.
public class CompositionPanelToggle : MonoBehaviour
{
    [Header("Composition Panel")]

    // The UI panel used for composing music.
    [SerializeField] private GameObject compositionPanel;

    [Header("Composition Player")]

    // Controls playback and clearing of the composition.
    [SerializeField] private CompositionPlayer compositionPlayer;

    [Header("Player")]

    // References to the player's movement and camera controls.
    // These are disabled while composing.
    [SerializeField] private PlayerMovement playerMovement;
    [SerializeField] private MouseLook mouseLook;

    // Controls playback of previously saved WAV recordings.
    [SerializeField]
    private SavedRecordingLibrary savedRecordingLibrary;

    // FMOD buses.
    //
    // Music Bus
    // - Controls all environment/background music.
    //
    // Chords Bus
    // - Controls all composition chord audio.
    //
    // Separating them allows each group of sounds
    // to be controlled independently.
    private Bus musicBus;
    private Bus chordsBus;

    private void Awake()
    {
        // Get references to both FMOD buses.
        musicBus = RuntimeManager.GetBus("bus:/Music");
        chordsBus = RuntimeManager.GetBus("bus:/ChordsComposition");

        // Start with the composition panel hidden.
        if (compositionPanel != null)
        {
            compositionPanel.SetActive(false);
        }
    }

    private void Start()
    {
        // Restore the normal gameplay state when the scene starts.
        ResumeMusic();
        StopChordAudio();
        EnablePlayerControls();

        // Keep the cursor available for UI interaction.
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    // Connecting this function to the Open button.
    //
    // Flow:
    // Open Button
    //      ↓
    // Stop saved recording
    //      ↓
    // Show composition panel
    //      ↓
    // Pause environment music
    //      ↓
    // Resume composition audio
    //      ↓
    // Disable player controls
    //      ↓
    // Show cursor
    public void OpenCompositionPanel()
    {
        if (compositionPanel == null)
        {
            Debug.LogError("Composition Panel is not assigned.");
            return;
        }

        // Prevent overlapping audio by stopping
        // any saved WAV recording currently playing.
        if (savedRecordingLibrary != null)
        {
            savedRecordingLibrary.StopSelectedRecording();
        }

        // Display the composition UI.
        compositionPanel.SetActive(true);

        // Pause the background music.
        PauseMusic();

        // Make sure the composition bus is active.
        ResumeChordBus();

        // Prevent the player from moving
        // while interacting with the UI.
        DisablePlayerControls();

        // Unlock and display the cursor
        // for UI interaction.
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    // Connecting this function to the Close button.
    //
    // Flow:
    // Close Button
    //      ↓
    // Stop composition playback
    //      ↓
    // Clear all chord slots
    //      ↓
    // Stop remaining FMOD events
    //      ↓
    // Resume background music
    //      ↓
    // Hide composition panel
    //      ↓
    // Enable player controls
    public void CloseCompositionPanel()
    {
        if (compositionPanel == null)
        {
            Debug.LogError("Composition Panel is not assigned.");
            return;
        }

        // Stop playback and clear the current composition.
        if (compositionPlayer != null)
        {
            compositionPlayer.ClearComposition();
        }
        else
        {
            Debug.LogWarning("Composition Player is not assigned.");
        }

        // Extra safety.
        // Stop any remaining FMOD chord events.
        StopChordAudio();

        // Resume the environment music.
        ResumeMusic();

        // Hide the composition UI.
        compositionPanel.SetActive(false);

        // Restore normal gameplay controls.
        EnablePlayerControls();

        // Keep the cursor visible.
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    // Pause every event routed through the Music bus.
    private void PauseMusic()
    {
        RefreshMusicBus();

        if (musicBus.isValid())
        {
            musicBus.setPaused(true);
        }
    }

    // Resume every event routed through the Music bus.
    private void ResumeMusic()
    {
        RefreshMusicBus();

        if (musicBus.isValid())
        {
            musicBus.setPaused(false);
        }
    }

    // Resume playback for the composition bus.
    private void ResumeChordBus()
    {
        RefreshChordBus();

        if (chordsBus.isValid())
        {
            chordsBus.setPaused(false);
        }
    }

    // Immediately stop all chord events.
    //
    // This prevents any composition audio
    // from continuing after the panel closes.
    private void StopChordAudio()
    {
        RefreshChordBus();

        if (chordsBus.isValid())
        {
            chordsBus.stopAllEvents(
                FMOD.Studio.STOP_MODE.IMMEDIATE
            );

            // Ensure the bus is ready
            // the next time composition starts.
            chordsBus.setPaused(false);
        }
    }

    // Disable player movement and camera control.
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

    // Restore player movement and camera control.
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

    // Check whether the Music bus reference
    // is still valid before using it.
    //
    // If the reference becomes invalid,
    // retrieve it again from FMOD.
    private void RefreshMusicBus()
    {
        if (!musicBus.isValid())
        {
            musicBus = RuntimeManager.GetBus("bus:/Music");
        }
    }

    // Check whether the ChordsComposition bus
    // is still valid before using it.
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