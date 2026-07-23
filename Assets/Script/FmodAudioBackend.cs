using System.Collections.Generic;
using UnityEngine;
using FMODUnity;
using FMOD.Studio;

public class FmodAudioBackend : MonoBehaviour, IAudioBackend
{
    [System.Serializable]
    public class LandmarkFmodConfig
    {
        public int landmarkId;
        public EventReference musicEvent;
    }

    [SerializeField]
    private List<LandmarkFmodConfig> landmarkConfigs = new List<LandmarkFmodConfig>();

    [SerializeField, Range(0.1f, 5f)]
    private float musicGain = 2.0f;

    private class LandmarkInstance
    {
        public EventInstance instance;
        public LoopType currentLoop = LoopType.Normal;
    }

    private Dictionary<int, LandmarkInstance> _instances = new Dictionary<int, LandmarkInstance>();

    private void Awake()
    {
        foreach (var config in landmarkConfigs)
        {
            if (config.musicEvent.IsNull)
            {
                Debug.LogWarning($"FmodAudioBackend: Landmark {config.landmarkId} has no musicEvent assigned.");
                continue;
            }

            var inst = new LandmarkInstance
            {
                instance = RuntimeManager.CreateInstance(config.musicEvent)
            };

            if (inst.instance.isValid())
            {
                inst.instance.setVolume(musicGain);
                inst.instance.start();
                inst.instance.setParameterByName("LoopType", (float)LoopType.Normal);
            }

            _instances[config.landmarkId] = inst;
        }
    }

    private void OnDestroy()
    {
        foreach (var kvp in _instances)
        {
            var inst = kvp.Value.instance;
            if (inst.isValid())
            {
                inst.stop(FMOD.Studio.STOP_MODE.ALLOWFADEOUT);
                inst.release();
            }
        }
    }

    public void CrossfadeLoop(
        int landmarkId,
        LoopType fromLoop,
        LoopType toLoop,
        float durationSeconds)
    {
        if (!_instances.TryGetValue(landmarkId, out var data))
            return;

        if (!data.instance.isValid())
            return;

        float loopValue = (float)toLoop;
        data.instance.setParameterByName("LoopType", loopValue);
        data.currentLoop = toLoop;
    }

    public void SetMixParameters(
        int landmarkId,
        float presence,
        float clarity,
        float reverb)
    {
        if (!_instances.TryGetValue(landmarkId, out var data))
            return;

        if (!data.instance.isValid())
            return;

        data.instance.setParameterByName("Presence", presence);
        data.instance.setParameterByName("Clarity", clarity);
        data.instance.setParameterByName("ReverbSend", reverb);
    }
}
