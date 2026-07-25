using UnityEngine;
using TMPro; // Use UnityEngine.UI if using standard UI Text

public class TextDisplay : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI instrumentLabel;
    [SerializeField] private TextMeshProUGUI moodLabel;

    public void Setup(InstrumentData instrumentData, Mood mood)
    {
        if (instrumentLabel != null)
        {
            instrumentLabel.text = instrumentData.instrumentName;
        }

        if (moodLabel != null)
        {
            moodLabel.text = mood.ToString();
        }
    }

    // To handle string-based IDs/names directly:
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