using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;
using static TutorialStep;

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

    [Header("Tutorial Status")]
    [field: SerializeField] public bool TutorialEnded { get; private set; } = false;

    [Header("One-Time Settings")]
    [SerializeField] private string tutorialSaveKey = "HasSeenTutorial_Scene";

    [Header("Tutorial Difficulty Settings")]
    [SerializeField] private float tutorialApproachRadiusMultiplier = 0.5f;
    [SerializeField] private float tutorialDuetRadiusMultiplier = 0.7f;

    [Header("References for Custom Triggers")]
    [SerializeField] private Transform player;
    private MusicLandmark[] landmarks;

    [Header("UI Button References")]
    [SerializeField] private UnityEngine.UI.Button recordButton;
    [SerializeField] private UnityEngine.UI.Button saveButton;
    [SerializeField] private UnityEngine.UI.Button libraryButton;
    [SerializeField] private UnityEngine.UI.Button composeButton;

    private int currentStepIndex = 0;
    private float timer = 0f;
    private bool isWaitingForDelay = false;
    private float lastClickTime = 0f;
    private float clickCooldown = 0.3f; // Prevents spam clicking within 300ms

    void Start()
    {
        //// REMOVE OR COMMENT OUT THIS LINE ONCE YOU BUILD YOUR .EXE:
        //PlayerPrefs.DeleteKey(tutorialSaveKey);

        if (PlayerPrefs.GetInt(tutorialSaveKey, 0) == 1)
        {
            if (tutorialPanel != null) tutorialPanel.SetActive(false);
            TutorialEnded = true;
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
        if (TutorialEnded || isWaitingForDelay || tutorialSteps == null || currentStepIndex >= tutorialSteps.Count) return;

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
                float distance = Vector3.Distance(player.position, landmark.transform.position);
                if (distance <= landmark.instrumentData.intenseDistance * tutorialApproachRadiusMultiplier * 0.5f)
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
                float distance = Vector3.Distance(player.position, landmark.transform.position);
                if (distance <= landmark.instrumentData.duetRadius * tutorialDuetRadiusMultiplier)
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
            FinishTutorial();
        }
    }

    private void DisplayStepUI(TutorialStep step)
    {
        if (tutorialPanel != null) tutorialPanel.SetActive(true);

        if (instructionLabel != null)
            instructionLabel.text = step.instructionText;

        if (tutorialArrow != null)
            tutorialArrow.SetActive(step.showArrowIndicator);

        // 1. Hide all glows first
        if (recordButtonGlow != null) recordButtonGlow.SetActive(false);
        if (libraryButtonGlow != null) libraryButtonGlow.SetActive(false);
        if (saveButtonGlow != null) saveButtonGlow.SetActive(false);
        if (composeButtonGlow != null) composeButtonGlow.SetActive(false);

        // 2. Control button interactivity and glows based on the current step's required button
        SetButtonInteractive(recordButton, recordButtonGlow, step.requiredButton == TutorialButtonType.Record);
        SetButtonInteractive(saveButton, saveButtonGlow, step.requiredButton == TutorialButtonType.Save);
        SetButtonInteractive(libraryButton, libraryButtonGlow, step.requiredButton == TutorialButtonType.Library);
        SetButtonInteractive(composeButton, composeButtonGlow, step.requiredButton == TutorialButtonType.Compose);
    }
    // Helper method to handle enabling/disabling cleanly
    private void SetButtonInteractive(UnityEngine.UI.Button btn, GameObject glow, bool isEnabled)
    {
        if (btn != null)
        {
            // If it's a button click tutorial step, lock/unlock it. 
            // If the step doesn't require ANY buttons, you might want all buttons interactable. 
            // Change 'true' below to '!isEnabled' if you want non-target buttons disabled during button steps.
            btn.interactable = isEnabled;
        }

        if (glow != null)
        {
            glow.SetActive(isEnabled);
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

    private bool isProcessingClick = false;
   // Universal button click method that works for ANY button!
    // Just pass a string or enum from your UI button: "Record", "Save", "Library", "Compose"
    public void OnSpecificButtonClicked(string buttonName)
    {
        if (isProcessingClick) return;
        isProcessingClick = true;

        if (isWaitingForDelay || tutorialSteps == null || currentStepIndex >= tutorialSteps.Count)
        {
            isProcessingClick = false;
            return;
        }

        TutorialStep currentStep = tutorialSteps[currentStepIndex];
      
        // SAFETY CHECK: If this step requires a button, block any other button immediately!
        if (currentStep.triggerType == TutorialTriggerType.ButtonClick)
        {
            if (currentStep.requiredButton.ToString() != buttonName)
            {
                Debug.LogWarning($"Blocked! You clicked '{buttonName}', but this tutorial step only allows '{currentStep.requiredButton}'.");
                isProcessingClick = false;
                return; // Stop right here, do not advance!
            }
        }

        Debug.Log($"Button '{buttonName}' clicked. Current step expects: {currentStep.requiredButton}");
       
        // Check if the current step requires a button click and matches this button
        if (currentStep.triggerType == TutorialTriggerType.ButtonClick)
        {
            // Match the button name/enum to the step's required button
            if (currentStep.requiredButton.ToString() == buttonName)
            {
                Debug.Log("Success! Correct button clicked.");
                NextStep();
            }
            else
            {
                Debug.LogWarning($"Wrong button! You clicked {buttonName}, but the tutorial expects {currentStep.requiredButton}.");
            }
           
        }
        StartCoroutine(ResetClickLock());
    }

    private System.Collections.IEnumerator ResetClickLock()
    {
        yield return null;
        isProcessingClick = false;
    }


    public void TryAdvanceStep(TutorialTriggerType requiredType)
    {
        if (isWaitingForDelay || tutorialSteps == null || currentStepIndex >= tutorialSteps.Count) return;

        if (tutorialSteps[currentStepIndex].triggerType == requiredType)
        {
            NextStep();
        }
    }

    private void FinishTutorial()
    {
        TutorialEnded = true; // Sets your tracking bool to true!

        PlayerPrefs.SetInt(tutorialSaveKey, 1);
        PlayerPrefs.Save();

        if (tutorialPanel != null)
            tutorialPanel.SetActive(false);

        if (tutorialArrow != null)
            tutorialArrow.SetActive(false);

        if (recordButtonGlow != null)
            recordButtonGlow.SetActive(false);

        if (libraryButtonGlow != null)
            libraryButtonGlow.SetActive(false);

        if (saveButtonGlow != null)
            saveButtonGlow.SetActive(false);

        if (composeButtonGlow != null)
            composeButtonGlow.SetActive(false);
        if (recordButton != null) recordButton.interactable = true;
        if (saveButton != null) saveButton.interactable = true;
        if (libraryButton != null) libraryButton.interactable = true;
        if (composeButton != null) composeButton.interactable = true;

        // RESUME GAME TIME 
        Time.timeScale = 1f;
        enabled = false;

        Debug.Log("Tutorial completed! game resume");
    }
}