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
        public Transform landmarkTransform;
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

            if (config.landmarkTransform == null)
            {
                Debug.LogWarning($"FmodAudioBackend: Landmark {config.landmarkId} has no transform assigned.");
                continue;
            }

            var inst = new LandmarkInstance
            {
                instance = RuntimeManager.CreateInstance(config.musicEvent)
            };

            if (!inst.instance.isValid())
            {
                Debug.LogWarning($"FmodAudioBackend: Instance for landmark {config.landmarkId} is not valid.");
                continue;
            }

            GameObject go = config.landmarkTransform.gameObject;
            Rigidbody rb = go.GetComponent<Rigidbody>();

            RuntimeManager.AttachInstanceToGameObject(inst.instance, go, rb);

            inst.instance.setVolume(musicGain);

            FMOD.RESULT startResult = inst.instance.start();
            if (startResult != FMOD.RESULT.OK)
            {
                Debug.LogWarning($"FmodAudioBackend: Failed to start event for landmark {config.landmarkId}: {startResult}");
            }

            // Force a simple, known state to test audibility
            inst.instance.setParameterByName("LoopType", (float)LoopType.Normal);
            inst.instance.setParameterByName("Presence", 1.0f);
            inst.instance.setParameterByName("Clarity", 1.0f);
            inst.instance.setParameterByName("ReverbSend", 0.0f);

            Debug.Log($"FmodAudioBackend: Started music event for landmark {config.landmarkId}.");
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
