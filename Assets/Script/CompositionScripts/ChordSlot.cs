using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class ChordSlot : MonoBehaviour, IDropHandler, IPointerClickHandler
{
    [Header("Display")]
    [SerializeField] private TMP_Text chordNameText;

    [Header("Colours")]
    [SerializeField] private Color emptyColour = new Color(1f, 1f, 1f, 0.25f);
    [SerializeField] private Color occupiedColour = Color.white;

    private ChordData assignedChord;

    public ChordData AssignedChord => assignedChord;


    public void OnDrop(PointerEventData eventData)
    {
        // Check if something was dropped.
        if (eventData.pointerDrag == null)
            return;

        ChordDragItem draggedChord =
            eventData.pointerDrag.GetComponent<ChordDragItem>();

        if (draggedChord == null || draggedChord.Data == null)
            return;

        AssignChord(draggedChord.Data);
    }

    public void AssignChord(ChordData chord)
    {
        // Save the chord into this slot.
        assignedChord = chord;
        RefreshDisplay();
    }

    public void ClearSlot()
    {
        // Remove the chord.
        assignedChord = null;
        RefreshDisplay();
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        // Right click to clear the slot.
        if (eventData.button == PointerEventData.InputButton.Right)
        {
            ClearSlot();
        }
    }

    private void RefreshDisplay()
    {
        // Update the text shown in the slot.
        bool hasChord = assignedChord != null;

        if (chordNameText != null)
        {
            chordNameText.text =
                hasChord ? assignedChord.chordName : "Drop Chord";
        }

    }
}