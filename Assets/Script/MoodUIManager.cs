using UnityEngine;
using System.Collections.Generic;
using UnityEngine.SceneManagement;

public class MoodUIManager : MonoBehaviour
{
    [SerializeField] private InstrumentDatabase database;

    [SerializeField] private InstrumentPanelUI panelPrefab;

    [SerializeField] private Transform panelParent;

    private void Start()
    {
        foreach (InstrumentData instrument in database.instruments)
        {
            InstrumentPanelUI panel = Instantiate(panelPrefab, panelParent);

            panel.Initialise(instrument);
        }
    }
}
