using System.Collections;
using System.Collections.Generic;
using FMOD.Studio;
using FMODUnity;
using UnityEngine;
using UnityEngine.UI;

// This script controls the full chord composition playback.
//
// Main responsibilities:
// 1. Reads every chord slot from left to right.
// 2. Plays the FMOD event stored inside each occupied slot.
// 3. Moves the playhead across both occupied and empty slots.
// 4. Supports Play, Pause, Stop and Clear.
// 5. Releases FMOD event instances after playback.
public class CompositionPlayer : MonoBehaviour
{
    [Header("Composition")]

    // Stores all chord slots in playback order.
    // The script checks the slots from the first slot to the last slot.
    [SerializeField]
    private List<ChordSlot> chordSlots = new List<ChordSlot>();

    [Header("Playhead")]

    // The moving line that shows the current playback position.
    [SerializeField]
    private RectTransform playhead;

    // The UI area that contains the playhead and chord slots.
    // Slot positions are converted relative to this container.
    [SerializeField]
    private RectTransform playheadContainer;

    // Stores the running playback coroutine.
    // A Coroutine allows the sequence to run over multiple frames
    // without freezing Unity.
    private Coroutine playbackCoroutine;

    // Stores the FMOD event instance that is currently playing.
    private EventInstance currentChordInstance;

    // Remembers whether playback is currently paused.
    private bool isPaused;

    private void Start()
    {
        isPaused = false;

        // Hide the playhead until the user presses Play.
        HidePlayhead();

        // Attempts to load chord audio data early
        // to reduce possible delay during playback.
        PreloadChordEvents();
    }

    // Connect this function to the Play button.
    public void PlayComposition()
    {
        // If the composition is paused and there is a valid
        // FMOD chord instance, resume that same chord.
        if (isPaused && currentChordInstance.isValid())
        {
            FMOD.RESULT resumeResult =
                currentChordInstance.setPaused(false);

            // Check whether FMOD resumed successfully.
            if (resumeResult != FMOD.RESULT.OK)
            {
                Debug.LogError(
                    "Could not resume composition: " +
                    resumeResult
                );

                return;
            }

            isPaused = false;

            Debug.Log("Composition resumed.");
            return;
        }

        // Do not start another coroutine if the composition
        // is already playing.
        if (playbackCoroutine != null)
        {
            Debug.Log("Composition is already playing.");
            return;
        }

        // Make sure the slot list exists and contains slots.
        if (chordSlots == null || chordSlots.Count == 0)
        {
            Debug.LogWarning(
                "No chord slots are assigned."
            );

            return;
        }

        // Make sure the playhead UI references are assigned.
        if (playhead == null ||
            playheadContainer == null)
        {
            Debug.LogError(
                "Playhead or Playhead Container is not assigned."
            );

            return;
        }

        isPaused = false;

        // Force Unity to update the UI layout before reading
        // the slot positions.
        //
        // Without this, Unity may still be using old UI coordinates,
        // causing the playhead to begin in the wrong position.
        Canvas.ForceUpdateCanvases();

        LayoutRebuilder.ForceRebuildLayoutImmediate(
            playheadContainer
        );

        // Start the chord sequence.
        playbackCoroutine =
            StartCoroutine(PlayChordSequence());
    }

    // Connect this function to the Pause button.
    public void PauseComposition()
    {
        // A valid FMOD event must exist before it can be paused.
        if (!currentChordInstance.isValid())
        {
            Debug.LogWarning(
                "There is no composition currently playing."
            );

            return;
        }

        // Prevent pausing twice.
        if (isPaused)
        {
            Debug.Log("Composition is already paused.");
            return;
        }

        // Ask FMOD to pause the current chord event.
        FMOD.RESULT pauseResult =
            currentChordInstance.setPaused(true);

        // Check whether FMOD paused successfully.
        if (pauseResult != FMOD.RESULT.OK)
        {
            Debug.LogError(
                "Could not pause composition: " +
                pauseResult
            );

            return;
        }

        isPaused = true;

        Debug.Log("Composition paused.");
    }

    // This coroutine plays each slot in order.
    //
    // A coroutine is used because playback happens over time.
    // It allows Unity to continue running normally while
    // the playhead and audio update every frame.
    private IEnumerator PlayChordSequence()
    {
        // Default duration used for empty slots.
        // This may be replaced by the duration of the first
        // valid chord found in the composition.
        float emptySlotDuration = 2f;

        // Search for the first valid chord.
        // Its FMOD event length is used as the duration
        // for empty slots so every slot takes similar time.
        foreach (ChordSlot slot in chordSlots)
        {
            if (slot == null ||
                slot.AssignedChord == null ||
                slot.AssignedChord.chordEvent.IsNull)
            {
                continue;
            }

            // Get the FMOD event description.
            // The description contains information about the event,
            // such as its timeline length.
            EventDescription description =
                RuntimeManager.GetEventDescription(
                    slot.AssignedChord.chordEvent
                );

            if (description.isValid())
            {
                FMOD.RESULT lengthResult =
                    description.getLength(
                        out int lengthMilliseconds
                    );

                if (lengthResult == FMOD.RESULT.OK &&
                    lengthMilliseconds > 0)
                {
                    // Convert milliseconds into seconds.
                    emptySlotDuration =
                        lengthMilliseconds / 1000f;

                    break;
                }
            }
        }

        // Place the playhead at the beginning of slot 1.
        if (chordSlots.Count > 0 &&
            chordSlots[0] != null)
        {
            SetPlayheadX(
                GetSlotStartX(chordSlots[0])
            );
        }

        // Go through every chord slot from left to right.
        for (int i = 0; i < chordSlots.Count; i++)
        {
            ChordSlot slot = chordSlots[i];

            // Skip missing slot references.
            if (slot == null)
            {
                continue;
            }

            // Find the left and right positions of this slot.
            float startX = GetSlotStartX(slot);
            float endX = GetSlotEndX(slot);

            // Move the playhead to the beginning of the slot.
            SetPlayheadX(startX);

            // Read the chord stored inside this slot.
            ChordData chord = slot.AssignedChord;

            // If the slot is empty, move through it silently.
            if (chord == null ||
                chord.chordEvent.IsNull)
            {
                float elapsedTime = 0f;

                // Keep moving until the empty slot duration is complete.
                while (elapsedTime < emptySlotDuration)
                {
                    // Only increase time when playback is not paused.
                    if (!isPaused)
                    {
                        elapsedTime +=
                            Time.unscaledDeltaTime;
                    }

                    // Convert elapsed time into a value from 0 to 1.
                    //
                    // 0 means the beginning of the slot.
                    // 0.5 means halfway through the slot.
                    // 1 means the end of the slot.
                    float progress =
                        Mathf.Clamp01(
                            elapsedTime /
                            emptySlotDuration
                        );

                    // Lerp smoothly calculates a position
                    // between the left and right edges of the slot.
                    SetPlayheadX(
                        Mathf.Lerp(
                            startX,
                            endX,
                            progress
                        )
                    );

                    // Wait until the next frame.
                    yield return null;
                }

                // Make sure the playhead reaches the end exactly.
                SetPlayheadX(endX);
                continue;
            }

            // Create a new playable FMOD instance
            // from the chord event stored in the slot.
            currentChordInstance =
                RuntimeManager.CreateInstance(
                    chord.chordEvent
                );

            // Get the event description from the active instance.
            FMOD.RESULT descriptionResult =
                currentChordInstance.getDescription(
                    out EventDescription eventDescription
                );

            // Stop if the FMOD description could not be retrieved.
            if (descriptionResult != FMOD.RESULT.OK)
            {
                Debug.LogError(
                    $"Could not get description for " +
                    $"{chord.chordName}: " +
                    descriptionResult
                );

                ReleaseCurrentChord();
                continue;
            }

            // Read the real FMOD event timeline length.
            FMOD.RESULT lengthResult =
                eventDescription.getLength(
                    out int eventLengthMilliseconds
                );

            // Stop if the event has no valid duration.
            if (lengthResult != FMOD.RESULT.OK ||
                eventLengthMilliseconds <= 0)
            {
                Debug.LogError(
                    $"{chord.chordName} has no valid timeline length."
                );

                ReleaseCurrentChord();
                continue;
            }

            // Start playing the FMOD event.
            FMOD.RESULT startResult =
                currentChordInstance.start();

            // Stop if the event failed to start.
            if (startResult != FMOD.RESULT.OK)
            {
                Debug.LogError(
                    $"Could not play {chord.chordName}: " +
                    startResult
                );

                ReleaseCurrentChord();
                continue;
            }

            isPaused = false;

            Debug.Log(
                $"Playing {chord.chordName} " +
                $"from slot {i + 1}."
            );

            // Continue updating while the FMOD instance is valid.
            while (currentChordInstance.isValid())
            {
                // Ask FMOD for the current timeline position.
                FMOD.RESULT positionResult =
                    currentChordInstance.getTimelinePosition(
                        out int timelineMilliseconds
                    );

                if (positionResult != FMOD.RESULT.OK)
                {
                    break;
                }

                // Calculate the playback percentage.
                //
                // Example:
                // Current position = 1000 ms
                // Total length = 2000 ms
                // Progress = 0.5, meaning halfway.
                float progress =
                    Mathf.Clamp01(
                        (float)timelineMilliseconds /
                        eventLengthMilliseconds
                    );

                // Synchronise the playhead with FMOD's
                // actual playback timeline.
                SetPlayheadX(
                    Mathf.Lerp(
                        startX,
                        endX,
                        progress
                    )
                );

                // Stop when the audio is close to the end.
                // The 20 milliseconds acts as a small tolerance.
                if (timelineMilliseconds >=
                    eventLengthMilliseconds - 20)
                {
                    break;
                }

                // Check whether FMOD has already stopped the event.
                FMOD.RESULT stateResult =
                    currentChordInstance.getPlaybackState(
                        out PLAYBACK_STATE playbackState
                    );

                if (stateResult != FMOD.RESULT.OK ||
                    playbackState ==
                    PLAYBACK_STATE.STOPPED)
                {
                    break;
                }

                // Wait until the next frame before checking again.
                yield return null;
            }

            // Place the playhead exactly at the end of the slot.
            SetPlayheadX(endX);

            // Release the finished FMOD event from memory.
            ReleaseCurrentChord();

            isPaused = false;
        }

        // Hide the playhead after all slots finish.
        HidePlayhead();

        // Reset the playback states.
        isPaused = false;
        playbackCoroutine = null;
    }

    // Attempts to preload currently assigned chord audio data.
    // This can help reduce delays when the event first plays.
    private void PreloadChordEvents()
    {
        foreach (ChordSlot slot in chordSlots)
        {
            if (slot == null ||
                slot.AssignedChord == null)
            {
                continue;
            }

            ChordData chord = slot.AssignedChord;

            if (chord.chordEvent.IsNull)
            {
                continue;
            }

            EventDescription eventDescription =
                RuntimeManager.GetEventDescription(
                    chord.chordEvent
                );

            if (eventDescription.isValid())
            {
                eventDescription.loadSampleData();
            }
        }
    }

    // Finds the horizontal starting position
    // of one chord slot.
    private float GetSlotStartX(ChordSlot slot)
    {
        // Get the RectTransform of this individual slot.
        RectTransform slotRect =
            slot.GetComponent<RectTransform>();

        // Every UI rectangle has four corners.
        // It represents the four corners of one rectangular UI slot. ( Vector3[4])
        // Unity stores the corners in this order:
        //
        // corners[0] = bottom-left
        // corners[1] = top-left
        // corners[2] = top-right
        // corners[3] = bottom-right
        //
        // Vector3 is used because each corner has
        // an X, Y and Z world position.
        Vector3[] corners = new Vector3[4];

        // Fill the array with the four world-space
        // corner positions of this slot.
        slotRect.GetWorldCorners(corners);

        // corners[0] is the bottom-left corner.
        //
        // The playhead container uses local coordinates,
        // but GetWorldCorners gives world coordinates.
        // InverseTransformPoint converts the world position
        // into the playhead container's local position.
        Vector3 localPosition =
            playheadContainer.InverseTransformPoint(
                corners[0]
            );

        // Only the X position is needed because
        // the playhead moves horizontally.
        return localPosition.x;
    }

    // Finds the horizontal ending position
    // of one chord slot.
    private float GetSlotEndX(ChordSlot slot)
    {
        RectTransform slotRect =
            slot.GetComponent<RectTransform>();

        // Again, this array stores the four corners
        // of one rectangular UI slot.
        //
        // It does not mean there are four slots.
        Vector3[] corners = new Vector3[4];

        // Get all four corners in world-space coordinates.
        slotRect.GetWorldCorners(corners);

        // corners[3] is the bottom-right corner,
        // which represents the horizontal end of the slot.
        Vector3 localPosition =
            playheadContainer.InverseTransformPoint(
                corners[3]
            );

        // Return only the horizontal X coordinate.
        return localPosition.x;
    }

    // Moves the playhead to a specific horizontal position.
    private void SetPlayheadX(float targetX)
    {
        if (playhead == null)
        {
            return;
        }

        // Read the current local position.
        Vector3 position =
            playhead.localPosition;

        // Change only the X position.
        // The Y and Z positions remain unchanged.
        position.x = targetX;

        // Apply the new position.
        playhead.localPosition = position;

        // Make sure the playhead is visible.
        playhead.gameObject.SetActive(true);
    }

    // Used by ClearComposition and OnDestroy.
    public void StopComposition()
    {
        // Stop the sequence coroutine if it is running.
        if (playbackCoroutine != null)
        {
            StopCoroutine(playbackCoroutine);
            playbackCoroutine = null;
        }

        // Stop the active FMOD chord.
        StopCurrentChord();

        isPaused = false;

        // Hide the playhead after stopping.
        HidePlayhead();
    }

    // Stops the currently playing FMOD chord immediately.
    private void StopCurrentChord()
    {
        if (!currentChordInstance.isValid())
        {
            return;
        }

        currentChordInstance.stop(
            FMOD.Studio.STOP_MODE.IMMEDIATE
        );

        ReleaseCurrentChord();
    }

    // Releases the current FMOD instance.
    //
    // Releasing it prevents unused event instances
    // from remaining in memory.
    private void ReleaseCurrentChord()
    {
        if (!currentChordInstance.isValid())
        {
            return;
        }

        currentChordInstance.release();
        currentChordInstance.clearHandle();
    }

    // Hides the playhead UI.
    private void HidePlayhead()
    {
        if (playhead != null)
        {
            playhead.gameObject.SetActive(false);
        }
    }

    // Connect this function to the Clear button.
    public void ClearComposition()
    {
        // Stop playback before clearing the slots.
        StopComposition();

        // Remove the assigned chord from every slot.
        foreach (ChordSlot slot in chordSlots)
        {
            if (slot != null)
            {
                slot.ClearSlot();
            }
        }
    }

    // Unity calls this when the object or scene is destroyed.
    private void OnDestroy()
    {
        // Stop and release all active playback safely.
        StopComposition();
    }
}