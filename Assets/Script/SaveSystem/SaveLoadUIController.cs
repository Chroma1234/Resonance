using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;



public class SaveLoadUIController : MonoBehaviour
{

    [Header("System")]
    [SerializeField]
    private ResonanceSaveManager saveManager;

    [Header("Inputs")]
    [SerializeField]
    private TMP_InputField nameInput;

    [SerializeField]
    private TMP_Text feedbackText;

    [Header("Save List")]
    [SerializeField]
    private Transform listContent;

    [SerializeField]
    private UnityEngine.UI.Button listButtonPrefab;

    [Header("Panels")]
    [SerializeField]
    private GameObject mainMenuPanel;

    [SerializeField]
    private GameObject savePanel;

    private string selectedId = string.Empty;


    private void Start()
    {
        ResolveSaveManager();
        RefreshList();
    }

    private void ResolveSaveManager()
    {
        if (saveManager == null)
        {
            saveManager = ResonanceSaveManager.Instance;
        }
    }

    public void SaveAsNew()
    {
        ResolveSaveManager();

        if (!HasSaveManager())
        {
            return;
        }

        if (saveManager.SaveActiveAsNew(GetTypedName()))
        {
            selectedId = saveManager.ActiveConfigurationId;
            Show("Configuration saved.");
            RefreshList();
        }
        else
        {
            Show("Save failed. Check the Console.");
        }
    }

    public void OverwriteSelected()
    {
        ResolveSaveManager();

        if (!HasSaveManager() || !RequireSelection())
        {
            return;
        }

        if (saveManager.OverwriteConfiguration(
                selectedId,
                GetTypedName()))
        {
            Show("Configuration overwritten.");
            RefreshList();
        }
        else
        {
            Show("Overwrite failed. Check the Console.");
        }
    }

    public void LoadSelected()
    {
        ResolveSaveManager();

        if (!HasSaveManager() || !RequireSelection())
        {
            return;
        }

        if (saveManager.LoadConfiguration(selectedId))
        {
            Show(
                "Configuration loaded. It will be used " +
                "when the instruments are spawned.");
        }
        else
        {
            Show("Load failed. Check the Console.");
        }
    }

    public void RenameSelected()
    {
        ResolveSaveManager();

        if (!HasSaveManager() || !RequireSelection())
        {
            return;
        }

        if (saveManager.RenameConfiguration(
                selectedId,
                GetTypedName()))
        {
            Show("Configuration renamed.");
            RefreshList();
        }
        else
        {
            Show("Rename failed.");
        }
    }

    public void DuplicateSelected()
    {
        ResolveSaveManager();

        if (!HasSaveManager() || !RequireSelection())
        {
            return;
        }

        if (saveManager.DuplicateConfiguration(
                selectedId,
                GetTypedName()))
        {
            Show("Configuration duplicated.");
            RefreshList();
        }
        else
        {
            Show("Duplicate failed.");
        }
    }

    public void DeleteSelected()
    {
        ResolveSaveManager();

        if (!HasSaveManager() || !RequireSelection())
        {
            return;
        }

        if (saveManager.DeleteConfiguration(selectedId))
        {
            selectedId = string.Empty;

            if (nameInput != null)
            {
                nameInput.text = string.Empty;
            }

            Show("Configuration deleted.");
            RefreshList();
        }
        else
        {
            Show("Delete failed.");
        }
    }

    public void RefreshList()
    {
        ResolveSaveManager();

        if (listContent == null)
        {
            Debug.LogWarning(
                "SaveLoadUIController: List Content is not assigned.");

            return;
        }

        foreach (Transform child in listContent)
        {
            Destroy(child.gameObject);
        }

        if (saveManager == null)
        {
            Show("Save system is unavailable.");
            return;
        }

        if (listButtonPrefab == null)
        {
            Debug.LogWarning(
                "SaveLoadUIController: " +
                "List Button Prefab is not assigned.");

            return;
        }

        List<SavedConfigurationEntry> entries =
            saveManager.GetConfigurations();

        foreach (SavedConfigurationEntry entry in entries)
        {
            UnityEngine.UI.Button button =
                Instantiate(listButtonPrefab, listContent);

            TMP_Text label =
                button.GetComponentInChildren<TMP_Text>();

            if (label != null)
            {
                label.text = entry.displayName;
            }

            string capturedId = entry.id;
            string capturedName = entry.displayName;

            button.onClick.AddListener(() =>
            {
                selectedId = capturedId;

                if (nameInput != null)
                {
                    nameInput.text = capturedName;
                }

                Show($"Selected '{capturedName}'.");
            });
        }
    }

    public void OpenSaveMenu()
    {
        if (mainMenuPanel != null)
        {
            mainMenuPanel.SetActive(false);
        }

        if (savePanel != null)
        {
            savePanel.SetActive(true);
        }

        RefreshList();
    }

    public void CloseSaveMenu()
    {
        selectedId = string.Empty;

        if (savePanel != null)
        {
            savePanel.SetActive(false);
        }

        if (mainMenuPanel != null)
        {
            mainMenuPanel.SetActive(true);
        }

        if (feedbackText != null)
        {
            feedbackText.text = string.Empty;
        }
    }

    private bool HasSaveManager()
    {
        if (saveManager != null)
        {
            return true;
        }

        Show("Save system is unavailable.");
        return false;
    }

    private string GetTypedName()
    {
        return nameInput != null
            ? nameInput.text
            : string.Empty;
    }

    private bool RequireSelection()
    {
        if (!string.IsNullOrWhiteSpace(selectedId))
        {
            return true;
        }

        Show("Select a saved configuration first.");
        return false;
    }

    private void Show(string message)
    {
        if (feedbackText != null)
        {
            feedbackText.text = message;
        }

        Debug.Log("[SaveLoadUI] " + message);
    }
}