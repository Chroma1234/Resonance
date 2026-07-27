using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// This script represents one composition slot.
// It allows a chord to be dropped into the slot
// and lets the user clear the slot with a right-click.
//
// Interfaces:
// IDropHandler         -> Called when a draggable UI object is dropped.
// IPointerClickHandler -> Called when the slot is clicked.
public class ChordSlot : MonoBehaviour, IDropHandler, IPointerClickHandler
{
    [Header("Display")]

    // Text shown inside the slot.
    // It displays either the chord name or "Drop Chord".
    [SerializeField] private TMP_Text chordNameText;

    [Header("Colours")]

    // Colours for empty and occupied slots.
    // (Currently reserved for future UI improvements.)
    [SerializeField] private Color emptyColour = new Color(1f, 1f, 1f, 0.25f);
    [SerializeField] private Color occupiedColour = Color.white;

    // Stores the chord currently assigned to this slot.
    private ChordData assignedChord;

    // Read-only property.
    // Other scripts (such as CompositionPlayer)
    // can read the assigned chord but cannot modify it directly.
    public ChordData AssignedChord => assignedChord;

    // Called automatically when a draggable UI object
    // is dropped onto this slot.
    public void OnDrop(PointerEventData eventData)
    {
        // Make sure something was actually dragged.
        if (eventData.pointerDrag == null)
            return;

        // Try to get the ChordDragItem component
        // from the object that was dropped.
        ChordDragItem draggedChord =
            eventData.pointerDrag.GetComponent<ChordDragItem>();

        // Stop if the dropped object is not a chord.
        if (draggedChord == null || draggedChord.Data == null)
            return;

        // Store the dropped chord in this slot.
        AssignChord(draggedChord.Data);
    }

    // Assign a chord to this slot.
    public void AssignChord(ChordData chord)
    {
        // Save the selected chord.
        assignedChord = chord;

        // Update the UI to display the new chord.
        RefreshDisplay();
    }

    // Remove the assigned chord from this slot.
    public void ClearSlot()
    {
        assignedChord = null;

        // Refresh the UI after removing the chord.
        RefreshDisplay();
    }

    // Called whenever the slot is clicked.
    public void OnPointerClick(PointerEventData eventData)
    {
        // Right-click clears the current chord.
        if (eventData.button == PointerEventData.InputButton.Right)
        {
            ClearSlot();
        }
    }

    // Updates the text displayed inside the slot.
    private void RefreshDisplay()
    {
        // Check if this slot currently contains a chord.
        bool hasChord = assignedChord != null;

        if (chordNameText != null)
        {
            // If a chord exists, show its name.
            // Otherwise display "Drop Chord".
            chordNameText.text =
                hasChord ? assignedChord.chordName : "Drop Chord";
        }

        // Future improvement:
        // The slot colour can also be updated here
        // using emptyColour and occupiedColour.
    }
}