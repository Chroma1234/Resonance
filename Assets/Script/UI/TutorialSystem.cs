using FMODUnity;
using TMPro;
using UnityEngine;

public class TutorialSystem : MonoBehaviour
{
    [Header("UI References")]
    private TextMeshProUGUI tutorialTextComponent;

    [Header("Data Source (No Hardcoding)")]
    private TutorialData tutorialData; // Plug your ScriptableObject asset here!

    private int currentStepIndex = 0;

    public void StartTutorial()
    {
        currentStepIndex = 0;
        DisplayCurrentStep();
        gameObject.SetActive(true);
    }

    private void DisplayCurrentStep()
    {
        // Safe check to ensure data exists and index is valid
        if (tutorialData != null && tutorialData.tutorialSteps != null)
        {
            if (tutorialTextComponent != null && currentStepIndex < tutorialData.tutorialSteps.Length)
            {
                tutorialTextComponent.text = tutorialData.tutorialSteps[currentStepIndex];
            }
        }
    }

    public void AdvanceTutorialStep()
    {
        if (tutorialData != null && tutorialData.tutorialSteps != null)
        {
            if (currentStepIndex < tutorialData.tutorialSteps.Length - 1)
            {
                currentStepIndex++;
                DisplayCurrentStep();
            }
            else
            {
                CompleteTutorial();
            }
        }
    }

    private void HandleInstrumentUnlocked(string instrumentName)
    {
        // Automatically steps forward when the team's event fires
        AdvanceTutorialStep();
    }

    private void CompleteTutorial()
    {
        if (tutorialTextComponent != null)
        {
            tutorialTextComponent.text = "Exploration Tutorial Complete!";
        }

        if (UIManager.Instance != null)
        {
            UIManager.Instance.ChangeState(UIState.Playing);
        }

        gameObject.SetActive(false);
    }
}