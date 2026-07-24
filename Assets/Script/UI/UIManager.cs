using System;
using FMODUnity;
using UnityEngine;
using UnityEngine.SceneManagement; // Required for SceneManager

public enum UIState { MainMenu, Tutorial, Playing, Paused }

public class UIManager : MonoBehaviour
{

    public static UIManager Instance { get; private set; }

    [Header("Panels")]
    private GameObject mainMenuPanel;
    private GameObject hudPanel;
    private GameObject tutorialPanel;
    private GameObject pausePanel;

    [Header("Sub-Systems")]
    private TutorialSystem tutorialSystem; // Link TutorialSystem script here in Inspector
    private UIState currentState;


    [Header("Mood Display UI")]
    [SerializeField] private Transform moodContainerContent; // Content transform of a ScrollView or Vertical Layout Group
    [SerializeField] private GameObject moodRowPrefab;       // Prefab with MoodDisplayRow attached
    [SerializeField] private ConfigurationProfile currentProfile; // Reference to the active configuration profile

    /// Refreshes the UI to display the currently chosen moods from MoodManager.
    public void RefreshSelectedMoodsDisplay()
    {
        if (moodContainerContent == null || moodRowPrefab == null) return;

        // Clear existing dynamic rows to prevent duplicates
        foreach (Transform child in moodContainerContent)
        {
            Destroy(child.gameObject);
        }

        if (currentProfile == null || currentProfile.instruments == null)
        {
            Debug.LogWarning("UIManager: No configuration profile assigned to display moods.");
            return;
        }

        // Loop through instruments dynamically from the profile
        foreach (var instrumentConfig in currentProfile.instruments)
        {
            if (instrumentConfig == null || string.IsNullOrEmpty(instrumentConfig.instrumentId)) continue;

            string instrumentId = instrumentConfig.instrumentId;

            Mood currentMood = Mood.Happy; // Fallback default

            GameObject rowInstance = Instantiate(moodRowPrefab, moodContainerContent);
            TextDisplay rowComponent = rowInstance.GetComponent<TextDisplay>();

            if (rowComponent != null)
            {
                rowComponent.SetupByName(instrumentId, currentMood);
            }
        }
    }
    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    // Event listener method for state changes
    private void HandleStateChange(UIState newState)
    {
        ChangeState(newState);
    }

    public void ChangeState(UIState newState)
    {
        currentState = newState;
        UpdateUIVisibility();

        // Architectural Trigger: If we enter the Tutorial state, initialize the tutorial logic
        if (currentState == UIState.Tutorial && tutorialSystem != null)
        {
            tutorialSystem.StartTutorial();
        }
    }

    private void UpdateUIVisibility()
    {
        if (mainMenuPanel != null) mainMenuPanel.SetActive(currentState == UIState.MainMenu);
        if (hudPanel != null) hudPanel.SetActive(currentState == UIState.Playing || currentState == UIState.Tutorial);
        if (tutorialPanel != null) tutorialPanel.SetActive(currentState == UIState.Tutorial);
        if (pausePanel != null) pausePanel.SetActive(currentState == UIState.Paused);
    }

    public void UpdateProximityDisplay(float value)
    {
        // hudPanel.GetComponent<ProximityUI>().UpdateMeter(value);
    }
    /// Loads the MoodSelection scene. Hook this up to the Start / Play button OnClick event.
    public void StartGame()
    {
        SceneManager.LoadScene("MoodSelection");
    }

    /// Quits the application. Hook this up to the Quit button OnClick event
    public void QuitGame()
    {
        Debug.Log("Quitting application...");

#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}