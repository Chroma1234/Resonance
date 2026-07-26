using FMOD.Studio;
using FMODUnity;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneLoaderScript : MonoBehaviour
{
    private Bus musicBus;
    private Bus chordsBus;

    private void Awake()
    {
        musicBus = RuntimeManager.GetBus("bus:/Music");
        chordsBus = RuntimeManager.GetBus("bus:/ChordsComposition");
    }

    public void LoadEnvironment()
    {
        // Stop all chord-composition sounds.
        if (chordsBus.isValid())
        {
            chordsBus.stopAllEvents(
                FMOD.Studio.STOP_MODE.IMMEDIATE
            );
        }

        // Resume the environment music.
        if (musicBus.isValid())
        {
            musicBus.setPaused(false);
        }

        SceneManager.LoadScene("Environment");
    }

    public void LoadComposition()
    {
        // Pause the environment music.
        if (musicBus.isValid())
        {
            musicBus.setPaused(true);
        }

        // Make sure the chord bus is ready to play.
        if (chordsBus.isValid())
        {
            chordsBus.setPaused(false);
        }

        SceneManager.LoadScene("SceneComposition");
    }
}