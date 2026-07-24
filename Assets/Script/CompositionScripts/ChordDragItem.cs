using FMODUnity;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

[RequireComponent(typeof(CanvasGroup))]
public class ChordDragItem : MonoBehaviour,
    IBeginDragHandler,
    IDragHandler,
    IEndDragHandler,
    IPointerClickHandler
{
    [Header("Chord")]
    [SerializeField] private ChordData chordData;

    [Header("UI")]
    [SerializeField] private TMP_Text chordNameText;

    private RectTransform rectTransform;
    private CanvasGroup canvasGroup;
    private Canvas rootCanvas;

    private Transform originalParent;
    private Vector2 originalPosition;

    public ChordData Data => chordData;

    private void Awake()
    {
        // Get the UI components.
        rectTransform = GetComponent<RectTransform>();
        canvasGroup = GetComponent<CanvasGroup>();
        rootCanvas = GetComponentInParent<Canvas>();

        if (chordData != null && chordNameText != null)
        {
            // Show the chord name.
            chordNameText.text = chordData.chordName;
        }
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        PlayPreview();
    }

    private void PlayPreview()
    {
        // Play a preview when the user clicks the chord.
        if (chordData == null)
        {
            Debug.LogError($"{name}: Chord Data is not assigned.");
            return;
        }
        // Check if the chord exists.
        if (chordData.chordEvent.IsNull)
        {
            Debug.LogError(
                $"{name}: No FMOD event assigned to {chordData.chordName}."
            );
            return;
        }

        Debug.Log($"Playing FMOD preview: {chordData.chordName}");
        // Play the FMOD event.
        RuntimeManager.PlayOneShot(chordData.chordEvent);
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        // Remember the original position.
        originalParent = transform.parent;
        // Move the chord above everything else.
        originalPosition = rectTransform.anchoredPosition;

        transform.SetParent(rootCanvas.transform);
        transform.SetAsLastSibling();

        // Make it slightly transparent while dragging.
        canvasGroup.alpha = 0.7f;

        canvasGroup.blocksRaycasts = false;
    }

    public void OnDrag(PointerEventData eventData)
    {
        // Follow the mouse.
        rectTransform.anchoredPosition +=
            eventData.delta / rootCanvas.scaleFactor;
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        // Put it back after dragging.
        canvasGroup.alpha = 1f;
        canvasGroup.blocksRaycasts = true;

        transform.SetParent(originalParent);
        rectTransform.anchoredPosition = originalPosition;
    }
}