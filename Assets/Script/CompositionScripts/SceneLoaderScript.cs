using FMOD.Studio;
using FMODUnity;
using UnityEngine;

public class CompositionPanelToggle : MonoBehaviour
{
    [Header("Composition Panel")]
    [SerializeField] private GameObject compositionPanel;

    [Header("Player")]
    [SerializeField] private PlayerMovement playerMovement;
    [SerializeField] private MouseLook mouseLook;

    private Bus musicBus;
    private Bus chordsBus;

    private void Awake()
    {
        GetFmodBuses();

        // Panel must not appear when the scene starts.
        if (compositionPanel != null)
        {
            compositionPanel.SetActive(false);
        }
    }

    private void Start()
    {
        // Environment music starts playing.
        ResumeMusic();

        // Composition sounds start stopped.
        StopChordAudio();

        UpdatePlayerControls(false);
    }

    private void GetFmodBuses()
    {
        musicBus =
            RuntimeManager.GetBus("bus:/Music");

        chordsBus =
            RuntimeManager.GetBus(
                "bus:/ChordsComposition"
            );
    }

    // Connect the same button to this method.
    public void ToggleCompositionPanel()
    {
        if (compositionPanel == null)
        {
            Debug.LogError(
                "Composition Panel is not assigned."
            );

            return;
        }

        // Check the panel itself instead of relying on a bool.
        bool shouldOpen =
            !compositionPanel.activeSelf;

        if (shouldOpen)
        {
            OpenCompositionPanel();
        }
        else
        {
            CloseCompositionPanel();
        }
    }

    private void OpenCompositionPanel()
    {
        compositionPanel.SetActive(true);

        PauseMusic();

        if (chordsBus.isValid())
        {
            chordsBus.setPaused(false);
        }

        UpdatePlayerControls(true);
    }

    private void CloseCompositionPanel()
    {
        StopChordAudio();
        ResumeMusic();

        compositionPanel.SetActive(false);

        UpdatePlayerControls(false);
    }

    private void PauseMusic()
    {
        // Refresh the bus handle if necessary.
        if (!musicBus.isValid())
        {
            musicBus =
                RuntimeManager.GetBus("bus:/Music");
        }

        if (musicBus.isValid())
        {
            FMOD.RESULT result =
                musicBus.setPaused(true);

            if (result != FMOD.RESULT.OK)
            {
                Debug.LogWarning(
                    "Could not pause Music bus: " +
                    result
                );
            }
        }
        else
        {
            Debug.LogWarning(
                "Music bus was not found. " +
                "Check that the path is bus:/Music"
            );
        }
    }

    private void ResumeMusic()
    {
        if (!musicBus.isValid())
        {
            musicBus =
                RuntimeManager.GetBus("bus:/Music");
        }

        if (musicBus.isValid())
        {
            FMOD.RESULT result =
                musicBus.setPaused(false);

            if (result != FMOD.RESULT.OK)
            {
                Debug.LogWarning(
                    "Could not resume Music bus: " +
                    result
                );
            }
        }
    }

    private void StopChordAudio()
    {
        if (!chordsBus.isValid())
        {
            chordsBus =
                RuntimeManager.GetBus(
                    "bus:/ChordsComposition"
                );
        }

        if (chordsBus.isValid())
        {
            chordsBus.stopAllEvents(
                FMOD.Studio.STOP_MODE.IMMEDIATE
            );

            chordsBus.setPaused(false);
        }
    }

    private void UpdatePlayerControls(
        bool panelIsOpen
    )
    {
        bool canMove = !panelIsOpen;

        if (playerMovement != null)
        {
            playerMovement.enabled = canMove;
        }

        if (mouseLook != null)
        {
            mouseLook.enabled = canMove;
        }

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }
}