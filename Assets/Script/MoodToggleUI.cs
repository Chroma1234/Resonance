using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class MoodToggleUI : MonoBehaviour
{
    [SerializeField] private Toggle toggle;

    [SerializeField] private TMP_Text label;

    private InstrumentData instrument;

    private Mood mood;

    public void Initialise(InstrumentData instrument, Mood mood, ToggleGroup group)
    {
        this.instrument = instrument;
        this.mood = mood;

        label.text = mood.ToString();

        toggle.group = group;
        toggle.isOn = MoodManager.Instance.GetMood(instrument) == mood;
        toggle.onValueChanged.AddListener(OnToggleChanged);
    }

    private void OnToggleChanged(bool isOn)
    {
        if (!isOn)
        {
            return;
        }

        Debug.Log($"Selected {mood} for {instrument.instrumentName}");

        MoodManager.Instance.SetMood(instrument, mood);
    }
}
