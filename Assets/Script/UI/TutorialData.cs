using UnityEngine;

[CreateAssetMenu(fileName = "NewTutorialData", menuName = "Game/Tutorial Data")]
public class TutorialData : ScriptableObject
{
    [Header("Exploration Steps")]
    [TextArea(2, 5)]
    public string[] tutorialSteps;
}