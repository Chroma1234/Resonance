using UnityEngine;
using TMPro;
using System.Collections.Generic;

public class TextDisplay : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI instrumentLabel;
    [SerializeField] private TextMeshProUGUI moodLabel;

    [Header("List of all instruments to display/cycle through")]
    [SerializeField] private List<InstrumentData> instruments;

    private void OnEnable()
    {
        DisplayFirstInstrument();
    }

    public void DisplayFirstInstrument()
    {
        if (instruments == null || instruments.Count == 0) return;

        // Grab the first instrument (or you can expand this to loop/select active ones)
        InstrumentData currentInstrument = instruments[0];

        if (currentInstrument != null)
        {
            string instName = currentInstrument.instrumentName;

            // Get its mood from MoodManager
            Mood currentMood = Mood.Happy; // Fallback
            if (MoodManager.Instance != null)
            {
                currentMood = MoodManager.Instance.GetMood(currentInstrument);
            }

            SetupByName(instName, currentMood);
        }
    }

    public void SetupByName(string instrumentName, Mood mood)
    {
        if (instrumentLabel != null)
        {
            instrumentLabel.text = instrumentName;
        }

        if (moodLabel != null)
        {
            moodLabel.text = mood.ToString();
        }
    }
}