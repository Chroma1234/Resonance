using System.Collections;
using System.Collections.Generic;
using FMOD.Studio;
using FMODUnity;
using UnityEngine;

public class CompositionPlayer : MonoBehaviour
{
    [Header("Composition")]
    [SerializeField]
    private List<ChordSlot> chordSlots = new List<ChordSlot>();

    // The composition always plays at 98 BPM.
    private const float BPM = 98f;

    [Header("Timing")]

    // Each chord slot lasts for 4 beats.
    [SerializeField] private int beatsPerSlot = 4;

    [Header("Playhead")]

    // The white line that moves across the chord slots.
    [SerializeField] private RectTransform playhead;

    // The panel containing the playhead and chord slots.
    // It is used to calculate the playhead position.
    [SerializeField] private RectTransform playheadContainer;

    // Stores the playback coroutine so it can be stopped.
    private Coroutine playbackCoroutine;

    // Stores the FMOD chord that is currently playing.
    private EventInstance currentChordInstance;

    private void Start()
    {
        // Hide the playhead until Play is pressed.
        HidePlayhead();
    }

    public void PlayComposition()
    {
        // Stop the previous playback before starting again.
        StopComposition();

        if (chordSlots == null || chordSlots.Count == 0)
        {
            Debug.LogWarning("No chord slots are assigned.");
            return;
        }

        playbackCoroutine =
            StartCoroutine(PlayChordSequence());
    }

    private IEnumerator PlayChordSequence()
    {
        // Convert 98 BPM into the duration of each slot.
        float secondsPerBeat = 60f / BPM;
        float secondsPerSlot =
            secondsPerBeat * beatsPerSlot;

        bool foundChord = false;

        ChordSlot firstSlot = GetFirstValidSlot();

        if (firstSlot == null)
        {
            Debug.LogWarning(
                "The chord slot list contains no valid slots."
            );

            playbackCoroutine = null;
            yield break;
        }

        // Place the playhead at the start of slot 1.
        MovePlayheadImmediately(firstSlot);

        // Play each slot from left to right.
        for (int i = 0; i < chordSlots.Count; i++)
        {
            ChordSlot currentSlot = chordSlots[i];

            // Skip missing slot references.
            if (currentSlot == null)
            {
                continue;
            }

            ChordData chord = currentSlot.AssignedChord;

            if (chord == null)
            {
                // An empty slot works as a rest.
                Debug.Log($"Slot {i + 1} is empty.");

                StopCurrentChord();
            }
            else if (chord.chordEvent.IsNull)
            {
                Debug.LogError(
                    $"{chord.chordName} has no FMOD event assigned."
                );

                StopCurrentChord();
            }
            else
            {
                foundChord = true;

                // Keep the previous chord temporarily.
                EventInstance previousChord =
                    currentChordInstance;

                // Create the new chord.
                currentChordInstance =
                    RuntimeManager.CreateInstance(
                        chord.chordEvent
                    );

                // Start the new chord first.
                // This helps the chords change without a gap.
                currentChordInstance.start();

                // Stop the previous chord after the new one starts.
                if (previousChord.isValid())
                {
                    previousChord.stop(
                        FMOD.Studio.STOP_MODE.IMMEDIATE
                    );

                    previousChord.release();
                    previousChord.clearHandle();
                }

                Debug.Log(
                    $"Playing {chord.chordName} from slot {i + 1}."
                );
            }

            ChordSlot nextSlot =
                GetNextValidSlot(i + 1);

            if (nextSlot != null)
            {
                // Move to the next slot while the chord plays.
                yield return StartCoroutine(
                    SlidePlayheadToSlot(
                        nextSlot,
                        secondsPerSlot
                    )
                );
            }
            else
            {
                // Wait for the final slot to finish.
                yield return new WaitForSecondsRealtime(secondsPerSlot);
            }
        }

        if (!foundChord)
        {
            Debug.LogWarning(
                "No chords have been placed into the composition."
            );
        }

        StopCurrentChord();
        HidePlayhead();
        playbackCoroutine = null;
    }

    private ChordSlot GetFirstValidSlot()
    {
        // Find the first slot that is assigned.
        for (int i = 0; i < chordSlots.Count; i++)
        {
            if (chordSlots[i] != null)
            {
                return chordSlots[i];
            }
        }

        return null;
    }

    private ChordSlot GetNextValidSlot(int startIndex)
    {
        // Find the next slot after the current one.
        for (int i = startIndex;
             i < chordSlots.Count;
             i++)
        {
            if (chordSlots[i] != null)
            {
                return chordSlots[i];
            }
        }

        return null;
    }

    private float GetSlotStartX(ChordSlot slot)
    {
        RectTransform slotRect =
            slot.GetComponent<RectTransform>();

        Vector3[] corners = new Vector3[4];
        slotRect.GetWorldCorners(corners);

        // Corner 0 is the bottom-left of the slot.
        // This makes the line touch the start of the box.
        Vector3 leftSide = corners[0];

        // Convert the position into the playhead panel.
        Vector3 localPosition =
            playheadContainer.InverseTransformPoint(
                leftSide
            );

        return localPosition.x;
    }

    private void MovePlayheadImmediately(ChordSlot slot)
    {
        if (playhead == null ||
            playheadContainer == null ||
            slot == null)
        {
            return;
        }

        float targetX = GetSlotStartX(slot);

        playhead.anchoredPosition = new Vector2(
            targetX,
            playhead.anchoredPosition.y
        );

        playhead.gameObject.SetActive(true);
    }

    private IEnumerator SlidePlayheadToSlot(
        ChordSlot slot,
        float duration
    )
    {
        if (playhead == null ||
            playheadContainer == null ||
            slot == null)
        {
            yield break;
        }

        // Save the start and target positions.
        float startX =
            playhead.anchoredPosition.x;

        float targetX =
            GetSlotStartX(slot);

        float elapsed = 0f;

        playhead.gameObject.SetActive(true);

        // Move the playhead smoothly every frame.
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;

            float progress = Mathf.Clamp01(
                elapsed / duration
            );

            float newX = Mathf.Lerp(
                startX,
                targetX,
                progress
            );

            playhead.anchoredPosition =
                new Vector2(
                    newX,
                    playhead.anchoredPosition.y
                );

            yield return null;
        }

        // Make sure the playhead ends exactly
        // at the beginning of the next slot.
        playhead.anchoredPosition =
            new Vector2(
                targetX,
                playhead.anchoredPosition.y
            );
    }

    public void StopComposition()
    {
        if (playbackCoroutine != null)
        {
            StopCoroutine(playbackCoroutine);
            playbackCoroutine = null;
        }

        StopCurrentChord();
        HidePlayhead();
    }

    private void StopCurrentChord()
    {
        // Do nothing if there is no chord playing.
        if (!currentChordInstance.isValid())
        {
            return;
        }

        currentChordInstance.stop(
            FMOD.Studio.STOP_MODE.IMMEDIATE
        );

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

        // Remove every chord from the composition.
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
        // Stop the audio when the scene closes.
        StopComposition();
    }
}