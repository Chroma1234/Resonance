using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

// Require a CanvasGroup component.
// This is used to change transparency and allow the chord
// to be dropped onto UI slots while dragging.
[RequireComponent(typeof(CanvasGroup))]

// This script controls one draggable chord in the chord library.
//
// Interfaces:
// IBeginDragHandler -> Called once when dragging starts.
// IDragHandler      -> Called every frame while dragging.
// IEndDragHandler   -> Called once when dragging ends.
public class ChordDragItem : MonoBehaviour,
    IBeginDragHandler,
    IDragHandler,
    IEndDragHandler
{
    [Header("Chord")]

    // Stores the chord information,
    // such as its name and FMOD event.
    [SerializeField] private ChordData chordData;

    [Header("UI")]

    // Text displayed on the draggable chord.
    [SerializeField] private TMP_Text chordNameText;

    // Controls the position of this UI object.
    private RectTransform rectTransform;

    // Controls transparency and whether
    // this object blocks UI raycasts.
    private CanvasGroup canvasGroup;

    // The main Canvas containing this draggable UI.
    private Canvas rootCanvas;

    // Stores the original parent and position.
    // This allows the chord to return to the library
    // after the player finishes dragging it.
    private Transform originalParent;
    private Vector2 originalPosition;

    // Read-only property.
    // Other scripts can read the chord data,
    // but they cannot overwrite it directly.
    public ChordData Data => chordData;

    private void Awake()
    {
        // Get the UI components attached to this object.
        rectTransform = GetComponent<RectTransform>();
        canvasGroup = GetComponent<CanvasGroup>();

        // Find the main parent Canvas.
        // Dragging is calculated relative to this Canvas.
        rootCanvas = GetComponentInParent<Canvas>();

        // Display the chord name on the UI.
        if (chordData != null && chordNameText != null)
        {
            chordNameText.text = chordData.chordName;
        }
    }

    // Called automatically once
    // when the player starts dragging the chord.
    public void OnBeginDrag(PointerEventData eventData)
    {
        // Save the original parent and position
        // so the chord can return after dragging.
        originalParent = transform.parent;
        originalPosition = rectTransform.anchoredPosition;

        // Move the chord under the main Canvas.
        // This prevents it from being hidden behind other UI.
        transform.SetParent(rootCanvas.transform);

        // Place it above the other UI elements.
        transform.SetAsLastSibling();

        // Make the chord slightly transparent
        // to show that it is being dragged.
        canvasGroup.alpha = 0.7f;

        // Disable raycasts on the dragged chord.
        // This allows the ChordSlot underneath
        // to receive the drop event.
        canvasGroup.blocksRaycasts = false;
    }

    // Called automatically every frame
    // while the player is dragging the chord.
    public void OnDrag(PointerEventData eventData)
    {
        // Move the chord by the same amount
        // that the mouse moved.
        //
        // Dividing by the Canvas scale factor
        // keeps the movement accurate on different resolutions.
        rectTransform.anchoredPosition +=
            eventData.delta / rootCanvas.scaleFactor;
    }

    // Called automatically once
    // when the player releases the mouse button.
    public void OnEndDrag(PointerEventData eventData)
    {
        // Restore full visibility.
        canvasGroup.alpha = 1f;

        // Enable raycasts again so the chord
        // can be dragged in the future.
        canvasGroup.blocksRaycasts = true;

        // Return the draggable chord to the library.
        //
        // The ChordSlot only copies the ChordData.
        // It does not move the original UI object into the slot.
        // This means the same chord can be reused many times.
        transform.SetParent(originalParent);
        rectTransform.anchoredPosition = originalPosition;
    }
}