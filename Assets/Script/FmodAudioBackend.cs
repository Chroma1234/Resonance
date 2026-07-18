using UnityEngine;
// using FMODUnity;

public class FmodAudioBackend : MonoBehaviour, IAudioBackend
{
    public void CrossfadeLoop(
        int landmarkId,
        LoopType fromLoop,
        LoopType toLoop,
        float durationSeconds)
    {
        // implement FMOD crossfade
        Debug.Log($"CrossfadeLoop: landmark {landmarkId}, {fromLoop} -> {toLoop}");
    }

    public void SetMixParameters(
        int landmarkId,
        float presence,
        float clarity,
        float reverb)
    {
        // set FMOD parameters
        Debug.Log($"SetMixParameters: landmark {landmarkId}, presence={presence}, clarity={clarity}, reverb={reverb}");
    }
}
