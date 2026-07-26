using UnityEngine;

public enum TutorialTriggerType
{
    Timer,
    Spacebar,
    MouseClick,
    AnyKey,
    ApproachLandmark,
    TriggerDuet,
    ButtonClick
}

[CreateAssetMenu(fileName = "NewTutorialStep", menuName = "Tutorial/Tutorial Step")]
public class TutorialStep : ScriptableObject
{
    [TextArea(3, 5)]
    [Header("Instruction Display")]
    public string instructionText;

    [Header("Completion Rule")]
    public TutorialTriggerType triggerType = TutorialTriggerType.Spacebar;

    [Tooltip("Only used if triggerType is set to Timer")]
    public float displayDuration = 3f;

    [Header("Timing")]
    public float delayBeforeShowing = 0f;

    [Header("Specific Step Target Settings")]
    public GameObject targetButton;
    public GameObject stepGlowObject;

    [Header("Visual Aids")]
    [Tooltip("Check if this step should show a directional arrow and button glow")]
    public bool showArrowIndicator = false;
}