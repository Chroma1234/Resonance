using System.Collections;
using System.Collections.Generic;
using FMOD.Studio;
using FMODUnity;
using UnityEngine;
using UnityEngine.UI;

public class CompositionPlayer : MonoBehaviour
{
    [Header("Composition")]
    [SerializeField]
    private List<ChordSlot> chordSlots = new List<ChordSlot>();

    [Header("Playhead")]
    [SerializeField]
    private RectTransform playhead;

    [SerializeField]
    private RectTransform playheadContainer;

    private Coroutine playbackCoroutine;
    private EventInstance currentChordInstance;

    private bool isPaused;

    private void Start()
    {
        isPaused = false;

        HidePlayhead();
        PreloadChordEvents();
    }

    // Connect this to the Play button.
    public void PlayComposition()
    {
        // Resume from the paused position.
        if (isPaused && currentChordInstance.isValid())
        {
            FMOD.RESULT resumeResult =
                currentChordInstance.setPaused(false);

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

        // Do not restart if it is already playing.
        if (playbackCoroutine != null)
        {
            Debug.Log("Composition is already playing.");
            return;
        }

        if (chordSlots == null || chordSlots.Count == 0)
        {
            Debug.LogWarning(
                "No chord slots are assigned."
            );

            return;
        }

        if (playhead == null ||
            playheadContainer == null)
        {
            Debug.LogError(
                "Playhead or Playhead Container is not assigned."
            );

            return;
        }

        isPaused = false;

        Canvas.ForceUpdateCanvases();

        LayoutRebuilder.ForceRebuildLayoutImmediate(
            playheadContainer
        );

        playbackCoroutine =
            StartCoroutine(PlayChordSequence());
    }

    // Connect this to the Pause button.
    public void PauseComposition()
    {
        if (!currentChordInstance.isValid())
        {
            Debug.LogWarning(
                "There is no composition currently playing."
            );

            return;
        }

        if (isPaused)
        {
            Debug.Log("Composition is already paused.");
            return;
        }

        FMOD.RESULT pauseResult =
            currentChordInstance.setPaused(true);

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

    private IEnumerator PlayChordSequence()
    {
        // Find a normal chord duration to use for empty slots.
        float emptySlotDuration = 2f;

        foreach (ChordSlot slot in chordSlots)
        {
            if (slot == null ||
                slot.AssignedChord == null ||
                slot.AssignedChord.chordEvent.IsNull)
            {
                continue;
            }

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
                    emptySlotDuration =
                        lengthMilliseconds / 1000f;

                    break;
                }
            }
        }

        // Start from the very beginning of slot 1.
        if (chordSlots.Count > 0 &&
            chordSlots[0] != null)
        {
            SetPlayheadX(
                GetSlotStartX(chordSlots[0])
            );
        }

        for (int i = 0; i < chordSlots.Count; i++)
        {
            ChordSlot slot = chordSlots[i];

            if (slot == null)
            {
                continue;
            }

            float startX = GetSlotStartX(slot);
            float endX = GetSlotEndX(slot);

            SetPlayheadX(startX);

            ChordData chord = slot.AssignedChord;

            // Empty slot: move the playhead across silently.
            if (chord == null ||
                chord.chordEvent.IsNull)
            {
                float elapsedTime = 0f;

                while (elapsedTime < emptySlotDuration)
                {
                    if (!isPaused)
                    {
                        elapsedTime +=
                            Time.unscaledDeltaTime;
                    }

                    float progress =
                        Mathf.Clamp01(
                            elapsedTime /
                            emptySlotDuration
                        );

                    SetPlayheadX(
                        Mathf.Lerp(
                            startX,
                            endX,
                            progress
                        )
                    );

                    yield return null;
                }

                SetPlayheadX(endX);
                continue;
            }

            currentChordInstance =
                RuntimeManager.CreateInstance(
                    chord.chordEvent
                );

            FMOD.RESULT descriptionResult =
                currentChordInstance.getDescription(
                    out EventDescription eventDescription
                );

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

            FMOD.RESULT lengthResult =
                eventDescription.getLength(
                    out int eventLengthMilliseconds
                );

            if (lengthResult != FMOD.RESULT.OK ||
                eventLengthMilliseconds <= 0)
            {
                Debug.LogError(
                    $"{chord.chordName} has no valid timeline length."
                );

                ReleaseCurrentChord();
                continue;
            }

            FMOD.RESULT startResult =
                currentChordInstance.start();

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

            while (currentChordInstance.isValid())
            {
                FMOD.RESULT positionResult =
                    currentChordInstance.getTimelinePosition(
                        out int timelineMilliseconds
                    );

                if (positionResult != FMOD.RESULT.OK)
                {
                    break;
                }

                float progress =
                    Mathf.Clamp01(
                        (float)timelineMilliseconds /
                        eventLengthMilliseconds
                    );

                SetPlayheadX(
                    Mathf.Lerp(
                        startX,
                        endX,
                        progress
                    )
                );

                if (timelineMilliseconds >=
                    eventLengthMilliseconds - 20)
                {
                    break;
                }

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

                yield return null;
            }

            SetPlayheadX(endX);

            ReleaseCurrentChord();
            isPaused = false;
        }

        HidePlayhead();

        isPaused = false;
        playbackCoroutine = null;
    }

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

    private float GetSlotStartX(ChordSlot slot)
    {
        RectTransform slotRect =
            slot.GetComponent<RectTransform>();

        Vector3[] corners = new Vector3[4];
        slotRect.GetWorldCorners(corners);

        Vector3 localPosition =
            playheadContainer.InverseTransformPoint(
                corners[0]
            );

        return localPosition.x;
    }

    private float GetSlotEndX(ChordSlot slot)
    {
        RectTransform slotRect =
            slot.GetComponent<RectTransform>();

        Vector3[] corners = new Vector3[4];
        slotRect.GetWorldCorners(corners);

        Vector3 localPosition =
            playheadContainer.InverseTransformPoint(
                corners[3]
            );

        return localPosition.x;
    }

    private void SetPlayheadX(float targetX)
    {
        if (playhead == null)
        {
            return;
        }

        Vector3 position =
            playhead.localPosition;

        position.x = targetX;

        playhead.localPosition = position;
        playhead.gameObject.SetActive(true);
    }

    // Used internally by Clear and OnDestroy.
    public void StopComposition()
    {
        if (playbackCoroutine != null)
        {
            StopCoroutine(playbackCoroutine);
            playbackCoroutine = null;
        }

        StopCurrentChord();

        isPaused = false;

        HidePlayhead();
    }

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

    private void ReleaseCurrentChord()
    {
        if (!currentChordInstance.isValid())
        {
            return;
        }

        currentChordInstance.release();
        currentChordInstance.clearHandle();
    }

    private void HidePlayhead()
    {
        if (playhead != null)
        {
            playhead.gameObject.SetActive(false);
        }
    }

    public void ClearComposition()
    {
        StopComposition();

        foreach (ChordSlot slot in chordSlots)
        {
            if (slot != null)
            {
                slot.ClearSlot();
            }
        }
    }

    private void OnDestroy()
    {
        StopComposition();
    }
}