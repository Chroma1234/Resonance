using FMODUnity;
using FMOD.Studio;
using UnityEngine;

public class VolumeManager : MonoBehaviour
{
    public static VolumeManager Instance;

    [SerializeField] private string musicVCAPath = "vca:/Music";

    private VCA musicVCA;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        musicVCA = RuntimeManager.GetVCA(musicVCAPath);
    }

    public void SetMusicVolume(float volume)
    {
        musicVCA.setVolume(volume);
    }

    public float GetMusicVolume()
    {
        musicVCA.getVolume(out float volume);
        return volume;
    }
}