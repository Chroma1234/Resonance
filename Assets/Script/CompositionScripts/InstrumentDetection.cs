using UnityEngine;

public class InstrumentDetection : MonoBehaviour
{
    [SerializeField] private CompositionUIManager uiManager;
    [SerializeField] private GameObject compositionPanel;

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player"))
            return;

        uiManager.EnterInstrument(compositionPanel);
    }

    private void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag("Player"))
            return;

        uiManager.ExitInstrument(compositionPanel);
    }
}