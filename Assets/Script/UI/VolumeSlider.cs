using UnityEngine;
using UnityEngine.UI;

public class VolumeSlider : MonoBehaviour
{
    [SerializeField] private Slider slider;

    private void Start()
    {
        slider.value = VolumeManager.Instance.GetMusicVolume();
        slider.onValueChanged.AddListener(OnVolumeChanged);
    }

    private void OnVolumeChanged(float value)
    {
        VolumeManager.Instance.SetMusicVolume(value);
    }

    private void OnDestroy()
    {
        slider.onValueChanged.RemoveListener(OnVolumeChanged);
    }
}