using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class InstrumentPanelUI : MonoBehaviour
{
    [SerializeField] private TMP_Text instrumentName;

    [SerializeField] private MoodToggleUI moodTogglePrefab;

    [SerializeField] private Transform moodParent;

    private InstrumentData instrument;

    public void Initialise(InstrumentData data)
    {
        instrument = data;

        instrumentName.text = data.name;
        ToggleGroup toggleGrp = moodParent.gameObject.GetComponent<ToggleGroup>();

        foreach (MoodEvent moodEvent in instrument.moodEvents)
        {
            MoodToggleUI toggle = Instantiate(moodTogglePrefab, moodParent);
            toggle.Initialise(instrument, moodEvent.mood, toggleGrp);
        }
    }
}
