using UnityEngine;
using TMPro;
using System.Collections.Generic;
using UnityEngine.InputSystem;

public class TutorialManager : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private GameObject tutorialPanel;
    [SerializeField] private TMP_Text instructionLabel;
    [SerializeField] private GameObject tutorialArrow;

    [Header("Tutorial Steps Sequence")]
    [SerializeField] private List<TutorialStep> tutorialSteps;

    [Header("References for Custom Triggers")]
    [SerializeField] private Transform player;
    private MusicLandmark[] landmarks;


    [Header("Tutorial Difficulty Settings")]
    [SerializeField] private float tutorialApproachRadiusMultiplier = 0.5f; // Makes approach range 50% smaller
    [SerializeField] private float tutorialDuetRadiusMultiplier = 0.7f;     // Makes duet range tighter

    private int currentStepIndex = 0;
    private float timer = 0f;

    void Start()
    {
        CacheReferences();

        if (tutorialSteps == null || tutorialSteps.Count == 0)
        {
            if (tutorialPanel != null) tutorialPanel.SetActive(false);
            return;
        }

        ShowStep(0);
    }


    //void Update()
    //{
    //    if (tutorialSteps == null || currentStepIndex >= tutorialSteps.Count) return;

    //    // Auto-cache if missing during runtime
    //    if (player == null || landmarks == null || landmarks.Length == 0)
    //    {
    //        CacheReferences();
    //        if (player == null) return;
    //    }

    //    TutorialStep currentStep = tutorialSteps[currentStepIndex];

    //    switch (currentStep.triggerType)
    //    {
    //        case TutorialTriggerType.Timer:
    //            timer += Time.unscaledDeltaTime;
    //            if (timer >= currentStep.displayDuration) NextStep();
    //            break;

    //        case TutorialTriggerType.Spacebar:
    //            if (Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame) NextStep();
    //            break;

    //        case TutorialTriggerType.MouseClick:
    //            if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame) NextStep();
    //            break;

    //        case TutorialTriggerType.ApproachLandmark:
    //            if (CheckPlayerApproachedLandmark()) NextStep();
    //            break;

    //        case TutorialTriggerType.TriggerDuet:
    //            if (CheckPlayerTriggeredDuet()) NextStep();
    //            break;
    //    }
    //}

    void Update()
    {
        if (tutorialSteps == null || currentStepIndex >= tutorialSteps.Count) return;

        // Only try to cache the player if the current step actually needs it!
        TutorialStep currentStep = tutorialSteps[currentStepIndex];

        if (currentStep.triggerType == TutorialTriggerType.ApproachLandmark ||
            currentStep.triggerType == TutorialTriggerType.TriggerDuet)
        {
            if (player == null || landmarks == null || landmarks.Length == 0)
            {
                CacheReferences();
                if (player == null) return; // Skip if no player exists in this menu scene
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

                // Tightened radius specifically for the tutorial
                float adjustedRadius = landmark.InfluenceRadius * tutorialApproachRadiusMultiplier;

                if (distance <= adjustedRadius)
                {
                    Debug.Log($"[TutorialManager] Approached landmark: {landmark.name} at distance {distance:F2} (Required: < {adjustedRadius})");
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

                // Tightened duet radius specifically for the tutorial so you have to stand closer
                float adjustedDuetRadius = landmark.DuetRadius * tutorialDuetRadiusMultiplier;
                bool inRange = distance <= adjustedDuetRadius;

                if (inRange)
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
            if (tutorialPanel != null) tutorialPanel.SetActive(true);

            TutorialStep step = tutorialSteps[currentStepIndex];

            if (instructionLabel != null)
                instructionLabel.text = step.instructionText;

            if (tutorialArrow != null)
                tutorialArrow.SetActive(step.showArrowIndicator);
        }
        else
        {
            if (tutorialPanel != null) tutorialPanel.SetActive(false);
            if (tutorialArrow != null) tutorialArrow.SetActive(false);
        }
    }
    public void TryAdvanceStep(TutorialTriggerType requiredType)
    {
        if (tutorialSteps == null || currentStepIndex >= tutorialSteps.Count) return;

        if (tutorialSteps[currentStepIndex].triggerType == requiredType)
        {
            Debug.Log($"[TutorialManager] Successfully triggered step advance for: {requiredType}");
            NextStep();
        }
    }
    public void NextStep()
    {
        ShowStep(currentStepIndex + 1);
    }
}