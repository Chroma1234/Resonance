using System.Collections.Generic;
using UnityEngine;

public class EnvironmentAudioInitializer : MonoBehaviour
{
    [SerializeField] private FmodAudioBackend backend;
    [SerializeField] private List<FmodAudioBackend.LandmarkFmodConfig> landmarkConfigs;

    private void Start()
    {
        Debug.Log("EnvironmentAudioInitializer: Start called");

        if (backend == null)
        {
            backend = Object.FindFirstObjectByType<FmodAudioBackend>();
        }

        if (backend == null)
        {
            Debug.LogWarning("EnvironmentAudioInitializer: No FmodAudioBackend found.");
            return;
        }

        Debug.Log($"EnvironmentAudioInitializer: Initializing {landmarkConfigs.Count} configs");
        backend.InitializeInstances(landmarkConfigs);
    }
}
