using System.Collections.Generic;
using System.Linq;
using FMODUnity;
using UnityEngine;
using UnityEngine.SceneManagement;

public enum UIState { MainMenu, Tutorial, Playing, Paused }

public class UIManager : MonoBehaviour
{
    public static UIManager Instance { get; private set; }

    [Header("Panels")]
    [SerializeField] private GameObject mainMenuPanel;
    [SerializeField] private GameObject hudPanel;
    [SerializeField] private GameObject tutorialPanel;
    [SerializeField] private GameObject pausePanel;

    [Header("Sub-Systems")]
    [SerializeField] private TutorialSystem tutorialSystem;
    private UIState currentState;

    [Header("Mood Display UI")]
    [SerializeField] private Transform moodContainerContent;
    [SerializeField] private GameObject moodRowPrefab;
    [SerializeField] private ConfigurationProfile currentProfile;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    //public void RefreshSelectedMoodsDisplay()
    //{
    //    if (moodContainerContent == null || moodRowPrefab == null) return;

    //    // Clear existing dynamic rows to prevent duplicates
    //    foreach (Transform child in moodContainerContent)
    //    {
    //        Destroy(child.gameObject);
    //    }

    //    if (currentProfile == null || currentProfile.instruments == null)
    //    {
    //        Debug.LogWarning("UIManager: No configuration profile assigned to display moods.");
    //        return;
    //    }

    //    // Loop through instruments dynamically from the profile
    //    foreach (var instrumentConfig in currentProfile.instruments)
    //    {
    //        if (instrumentConfig == null || string.IsNullOrEmpty(instrumentConfig.instrumentId)) continue;

    //        string instrumentId = instrumentConfig.instrumentId;
    //        Mood currentMood = Mood.Happy; // Default fallback

    //        GameObject rowInstance = Instantiate(moodRowPrefab, moodContainerContent);
    //        TextDisplay rowComponent = rowInstance.GetComponent<TextDisplay>();

    //        if (rowComponent != null)
    //        {
    //            // Calls the method safely now that it exists in TextDisplay
    //            rowComponent.SetupByName(instrumentId, currentMood);
    //        }
    //    }
    //}
    public void RefreshSelectedMoodsDisplay()
    {
        if (moodContainerContent == null)
        {
            Debug.LogError("UIManager: moodContainerContent is NULL!");
            return;
        }
        if (moodRowPrefab == null)
        {
            Debug.LogError("UIManager: moodRowPrefab is NULL!");
            return;
        }
        if (currentProfile == null)
        {
            Debug.LogError("UIManager: currentProfile is NULL! Assign it in the Inspector.");
            return;
        }

        Debug.Log("Refreshing moods... Profile instruments count: "  + currentProfile.instruments.Count());

        foreach (Transform child in moodContainerContent)
        {
            Destroy(child.gameObject);
        }

        foreach (var instrumentConfig in currentProfile.instruments)
        {
            if (instrumentConfig == null) continue;

            Debug.Log("Spawning row for instrument: " + instrumentConfig.instrumentId);

            GameObject rowInstance = Instantiate(moodRowPrefab, moodContainerContent);
            TextDisplay rowComponent = rowInstance.GetComponent<TextDisplay>();

            if (rowComponent != null)
         
            {
                rowComponent.SetupByName(instrumentConfig.instrumentId, Mood.Happy);
            }
            else
            {
                Debug.LogError("The spawned row prefab is missing the TextDisplay script!");
            }
        }
    }
    private void HandleStateChange(UIState newState)
    {
        ChangeState(newState);
    }

    public void ChangeState(UIState newState)
    {
        currentState = newState;
        UpdateUIVisibility();

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

    public void StartGame()
    {
        SceneManager.LoadScene("MoodSelection");
        Debug.Log("UIManager Start called - forcing refresh!");
        RefreshSelectedMoodsDisplay();
    }

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