using UnityEngine;
using UnityEngine.UI;

public class VolumeSettings : MonoBehaviour
{
    [SerializeField] private Slider volumeSlider;

    void Start()
    {
        // Set the slider value to match the current global volume when the game starts
        if (volumeSlider != null)
        {
            volumeSlider.value = AudioListener.volume;

            // Listen for any changes made to the slider
            volumeSlider.onValueChanged.AddListener(SetVolume);
        }
    }

    public void SetVolume(float sliderValue)
    {
        // AudioListener.volume controls the global volume of the entire game (0.0 to 1.0)
        AudioListener.volume = sliderValue;

        // Optional: Save the volume preference so it remembers next time
        PlayerPrefs.SetFloat("MasterVolume", sliderValue);
    }
}