using UnityEngine;

public enum TutorialTriggerType
{
    Timer,
    Spacebar,
    MouseClick,
    AnyKey,
    ApproachLandmark, // New trigger for step 1
    TriggerDuet       // New trigger for step 2
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

    [Header("Visual Aids")]
    [Tooltip("Check if this step should show a directional arrow or highlight")]
    public bool showArrowIndicator = false;
}