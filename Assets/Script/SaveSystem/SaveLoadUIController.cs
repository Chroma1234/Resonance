using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class SaveLoadUIController : MonoBehaviour
{
    [Header("System")]
    [SerializeField] private ResonanceSaveManager saveManager;

    [Header("Inputs")]
    [SerializeField] private TMP_InputField nameInput;
    [SerializeField] private TMP_Text feedbackText;

    [Header("Save List")]
    [SerializeField] private Transform listContent;
    [SerializeField] private Button listButtonPrefab;

    private string selectedId;

    private void Start()
    {
        RefreshList();
    }

    public void SaveAsNew()
    {
        if (saveManager.SaveActiveAsNew(nameInput.text))
        {
            selectedId = saveManager.ActiveConfigurationId;
            Show("Configuration saved.");
            RefreshList();
        }
    }

    public void OverwriteSelected()
    {
        if (!RequireSelection())
            return;

        if (saveManager.ActiveConfigurationId != selectedId &&
            !saveManager.LoadConfiguration(selectedId))
        {
            Show("Could not prepare the selected save.");
            return;
        }

        if (saveManager.OverwriteActive(nameInput.text))
        {
            Show("Configuration overwritten.");
            RefreshList();
        }
    }

    public void LoadSelected()
    {
        if (!RequireSelection())
            return;

        if (saveManager.LoadConfiguration(selectedId))
            Show("Configuration loaded.");
    }

    public void RenameSelected()
    {
        if (!RequireSelection())
            return;

        if (saveManager.RenameConfiguration(selectedId, nameInput.text))
        {
            Show("Configuration renamed.");
            RefreshList();
        }
    }

    public void DuplicateSelected()
    {
        if (!RequireSelection())
            return;

        if (saveManager.DuplicateConfiguration(selectedId, nameInput.text))
        {
            Show("Configuration duplicated.");
            RefreshList();
        }
    }

    public void DeleteSelected()
    {
        if (!RequireSelection())
            return;

        if (saveManager.DeleteConfiguration(selectedId))
        {
            selectedId = string.Empty;
            Show("Configuration deleted.");
            RefreshList();
        }
    }

    public void RefreshList()
    {
        foreach (Transform child in listContent)
            Destroy(child.gameObject);

        foreach (SavedConfigurationEntry item in
                 saveManager.GetConfigurations())
        {
            Button button =
                Instantiate(listButtonPrefab, listContent);

            TMP_Text label =
                button.GetComponentInChildren<TMP_Text>();

            if (label != null)
                label.text = item.displayName;

            string capturedId = item.id;
            string capturedName = item.displayName;

            button.onClick.AddListener(() =>
            {
                selectedId = capturedId;
                nameInput.text = capturedName;
                Show($"Selected {capturedName}.");
            });
        }
    }

    private bool RequireSelection()
    {
        if (!string.IsNullOrWhiteSpace(selectedId))
            return true;

        Show("Select a saved configuration first.");
        return false;
    }

    private void Show(string message)
    {
        if (feedbackText != null)
            feedbackText.text = message;

        Debug.Log($"[SaveLoadUI] {message}");
    }
}
