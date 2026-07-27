using FMODUnity;
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
// IBeginDragHandler    -> Called once when dragging starts.
// IDragHandler         -> Called every frame while dragging.
// IEndDragHandler      -> Called once when dragging ends.
// IPointerClickHandler -> Called when the player clicks the chord.
public class ChordDragItem : MonoBehaviour,
    IBeginDragHandler,
    IDragHandler,
    IEndDragHandler,
    IPointerClickHandler
{
    [Header("Chord")]

    // Stores the chord information such as
    // its name and FMOD event.
    [SerializeField] private ChordData chordData;

    [Header("UI")]

    // Text displayed on the draggable chord.
    [SerializeField] private TMP_Text chordNameText;

    // References to UI components used for dragging.
    private RectTransform rectTransform;   // Controls UI position.
    private CanvasGroup canvasGroup;       // Controls transparency and raycasts.
    private Canvas rootCanvas;             // Main canvas containing the UI.

    // Remember where the chord originally belongs.
    // After dropping, the draggable chord returns here.
    private Transform originalParent;
    private Vector2 originalPosition;

    // Read-only property.
    // Other scripts can read the chord data but cannot overwrite it.
    public ChordData Data => chordData;

    private void Awake()
    {
        // Get references to the UI components attached to this object.
        rectTransform = GetComponent<RectTransform>();
        canvasGroup = GetComponent<CanvasGroup>();

        // Find the parent canvas.
        // UI dragging should happen relative to the main canvas.
        rootCanvas = GetComponentInParent<Canvas>();

        // Display the chord name on the UI.
        if (chordData != null && chordNameText != null)
        {
            chordNameText.text = chordData.chordName;
        }
    }

    // Called automatically when the player clicks this chord.
    public void OnPointerClick(PointerEventData eventData)
    {
        PlayPreview();
    }

    private void PlayPreview()
    {
        // Make sure chord data exists.
        if (chordData == null)
        {
            Debug.LogError($"{name}: Chord Data is not assigned.");
            return;
        }

        // Make sure an FMOD event has been assigned.
        if (chordData.chordEvent.IsNull)
        {
            Debug.LogError(
                $"{name}: No FMOD event assigned to {chordData.chordName}."
            );
            return;
        }

        // Play a short preview of the chord.
        Debug.Log($"Playing FMOD preview: {chordData.chordName}");

        RuntimeManager.PlayOneShot(chordData.chordEvent);
    }

    // Called once when dragging begins.
    public void OnBeginDrag(PointerEventData eventData)
    {
        // Save the original location so we can restore it later.
        originalParent = transform.parent;
        originalPosition = rectTransform.anchoredPosition;

        // Move the chord to the root canvas.
        // This keeps it visible above all other UI while dragging.
        transform.SetParent(rootCanvas.transform);

        // Render this object above other UI elements.
        transform.SetAsLastSibling();

        // Make the chord slightly transparent to indicate it is being dragged.
        canvasGroup.alpha = 0.7f;

        // Disable raycasts so the slot underneath
        // can detect the drop event.
        canvasGroup.blocksRaycasts = false;
    }

    // Called every frame while dragging.
    public void OnDrag(PointerEventData eventData)
    {
        // Move together with the mouse.
        // Dividing by the canvas scale factor keeps
        // dragging accurate on different resolutions.
        rectTransform.anchoredPosition +=
            eventData.delta / rootCanvas.scaleFactor;
    }

    // Called once when the player releases the mouse button.
    public void OnEndDrag(PointerEventData eventData)
    {
        // Restore full visibility.
        canvasGroup.alpha = 1f;

        // Enable raycasts again so the chord
        // can be clicked and dragged in the future.
        canvasGroup.blocksRaycasts = true;

        // Return the draggable chord to the chord library.
        // The ChordSlot only copies the chord data,
        // so the original draggable object always stays here.
        transform.SetParent(originalParent);
        rectTransform.anchoredPosition = originalPosition;
    }
}