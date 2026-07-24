using UnityEngine;
using TMPro;

public class TextDisplay : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI instrumentLabel;
    [SerializeField] private TextMeshProUGUI moodLabel;

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