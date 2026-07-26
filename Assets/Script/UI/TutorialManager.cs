using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;

public class TutorialManager : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private GameObject tutorialPanel;
    [SerializeField] private TMP_Text instructionLabel;
    [SerializeField] private GameObject tutorialArrow;

    [Header("Button Glows")]
    [SerializeField] private GameObject recordButtonGlow;
    [SerializeField] private GameObject libraryButtonGlow;
    [SerializeField] private GameObject saveButtonGlow;
    [SerializeField] private GameObject composeButtonGlow;

    [Header("Tutorial Steps Sequence")]
    [SerializeField] private List<TutorialStep> tutorialSteps;

    [Header("One-Time Settings")]
    [SerializeField] private string tutorialSaveKey = "HasSeenTutorial_Scene";

    [Header("Tutorial Difficulty Settings")]
    [SerializeField] private float tutorialApproachRadiusMultiplier = 0.5f;
    [SerializeField] private float tutorialDuetRadiusMultiplier = 0.7f;

    [Header("References for Custom Triggers")]
    [SerializeField] private Transform player;
    private MusicLandmark[] landmarks;

    private int currentStepIndex = 0;
    private float timer = 0f;
    private bool isWaitingForDelay = false;

    void Start()
    {
        // REMOVE OR COMMENT OUT THIS LINE ONCE YOU BUILD YOUR .EXE:
        PlayerPrefs.DeleteKey(tutorialSaveKey);

        if (PlayerPrefs.GetInt(tutorialSaveKey, 0) == 1)
        {
            if (tutorialPanel != null) tutorialPanel.SetActive(false);
           
            enabled = false;
            return;
        }

        CacheReferences();

        if (tutorialSteps == null || tutorialSteps.Count == 0)
        {
            if (tutorialPanel != null) tutorialPanel.SetActive(false);
            return;
        }

        ShowStep(0);
    }

    void Update()
    {
        if (isWaitingForDelay || tutorialSteps == null || currentStepIndex >= tutorialSteps.Count) return;

        TutorialStep currentStep = tutorialSteps[currentStepIndex];

        if (currentStep.triggerType == TutorialTriggerType.ApproachLandmark ||
            currentStep.triggerType == TutorialTriggerType.TriggerDuet)
        {
            if (player == null || landmarks == null || landmarks.Length == 0)
            {
                CacheReferences();
                if (player == null) return;
            }
        }

        switch (currentStep.triggerType)
        {
            case TutorialTriggerType.Timer:
                timer += Time.unscaledDeltaTime;
                if (timer >= currentStep.displayDuration) NextStep();
                break;

            case TutorialTriggerType.Spacebar:
                if (Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame) NextStep();
                break;

            case TutorialTriggerType.MouseClick:
                if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame) NextStep();
                break;

            case TutorialTriggerType.ApproachLandmark:
                if (CheckPlayerApproachedLandmark()) NextStep();
                break;

            case TutorialTriggerType.TriggerDuet:
                if (CheckPlayerTriggeredDuet()) NextStep();
                break;

                // ButtonClick is handled via the OnClick event below, so we don't need code in Update for it!
        }
    }


    private void CacheReferences()
    {
        if (player == null)
        {
            GameObject playerObj = GameObject.FindWithTag("Player");
            if (playerObj != null)
            {
                player = playerObj.transform;
            }
        }

        landmarks = FindObjectsByType<MusicLandmark>(FindObjectsSortMode.None);
    }

    private bool CheckPlayerApproachedLandmark()
    {
        if (player == null || landmarks == null) return false;

        foreach (var landmark in landmarks)
        {
            if (landmark != null && landmark.instrumentData != null)
            {
                float distance = Vector3.Distance(player.position, landmark.Position);
                if (distance <= landmark.InfluenceRadius * tutorialApproachRadiusMultiplier * 0.5f)
                {
                    return true;
                }
            }
        }
        return false;
    }

    private bool CheckPlayerTriggeredDuet()
    {
        if (player == null || landmarks == null) return false;

        int nearbyCount = 0;
        foreach (var landmark in landmarks)
        {
            if (landmark != null && landmark.instrumentData != null)
            {
                float distance = Vector3.Distance(player.position, landmark.Position);
                if (distance <= landmark.DuetRadius * tutorialDuetRadiusMultiplier)
                {
                    nearbyCount++;
                }
            }
        }
        return nearbyCount >= 2;
    }

    private void ShowStep(int index)
    {
        currentStepIndex = index;
        timer = 0f;

        if (currentStepIndex < tutorialSteps.Count)
        {
            TutorialStep step = tutorialSteps[currentStepIndex];

            if (step.delayBeforeShowing > 0f)
            {
                StartCoroutine(ShowStepWithDelay(step));
            }
            else
            {
                DisplayStepUI(step);
            }
        }
        else
        {
            PlayerPrefs.SetInt(tutorialSaveKey, 1);
            PlayerPrefs.Save();
            if (tutorialPanel != null) tutorialPanel.SetActive(false);
            if (tutorialArrow != null) tutorialArrow.SetActive(false);
            enabled = false;
        }
    }

    private void DisplayStepUI(TutorialStep step)
    {
        if (tutorialPanel != null) tutorialPanel.SetActive(true);

        if (instructionLabel != null)
            instructionLabel.text = step.instructionText;

        if (tutorialArrow != null)
            tutorialArrow.SetActive(step.showArrowIndicator);

        // Hide all glows first
        if (recordButtonGlow != null) recordButtonGlow.SetActive(false);
        if (libraryButtonGlow != null) libraryButtonGlow.SetActive(false);
        if (saveButtonGlow != null) saveButtonGlow.SetActive(false);
        if (composeButtonGlow != null) composeButtonGlow.SetActive(false);

        // Turn on the specific glow based on the current step index
        // (Replace 3 and 5 with whatever your actual element index numbers are)
        if (currentStepIndex == 3 && recordButtonGlow != null)
        {
            recordButtonGlow.SetActive(true);
        }
        else if (currentStepIndex == 4 && saveButtonGlow != null)
        {
            saveButtonGlow.SetActive(true);
        }
        else if (currentStepIndex == 5 && libraryButtonGlow != null)
        {
            libraryButtonGlow.SetActive(true);
        }
        else if (currentStepIndex == 6 && composeButtonGlow != null)
        {
            composeButtonGlow.SetActive(true);
        }
    }
    private IEnumerator ShowStepWithDelay(TutorialStep step)
    {
        isWaitingForDelay = true;

        // Disappear/hide the panel during the wait time
        if (tutorialPanel != null) tutorialPanel.SetActive(false);
       

        yield return new WaitForSecondsRealtime(step.delayBeforeShowing);

        isWaitingForDelay = false;
        DisplayStepUI(step);
    }



    public void NextStep()
    {
        StopAllCoroutines();// Cancel any pending delayed steps if we move forward manually
        ShowStep(currentStepIndex + 1);
    }


    public void OnTargetButtonClicked()
    {
        Debug.Log("Button was clicked! Current step type is: " + tutorialSteps[currentStepIndex].triggerType);
        if (isWaitingForDelay || tutorialSteps == null || currentStepIndex >= tutorialSteps.Count) return;

        // Check if the current tutorial step is actually waiting for a button click
        if (tutorialSteps[currentStepIndex].triggerType == TutorialTriggerType.ButtonClick)
        {
            Debug.Log($"[Time: {Time.time}] Button clicked! Advancing from step {currentStepIndex}");
            NextStep();
        }
        else
        {
            Debug.LogWarning("Button clicked, but the current tutorial step is NOT set to ButtonClick!");
        }
        //if (currentStepIndex == 7)
        //{
        //    // Advance to the next step
        //    NextStep();
        //}
    }

    public void TryAdvanceStep(TutorialTriggerType requiredType)
     {
        if (isWaitingForDelay || tutorialSteps == null || currentStepIndex >= tutorialSteps.Count) return;

        if (tutorialSteps[currentStepIndex].triggerType == requiredType)
        {
            Debug.Log($"[TutorialManager] Successfully triggered step advance for: {requiredType}");
            NextStep();
        }
     }
}