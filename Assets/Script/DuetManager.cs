using System.Collections.Generic;
using UnityEngine;

public class DuetManager : MonoBehaviour
{
    private MusicLandmark[] landmarks;

    [SerializeField] private Transform player;

    private void Start()
    {
        landmarks = FindObjectsByType<MusicLandmark>(FindObjectsSortMode.None);
    }

    private void Update()
    {
        if (SoundManager.Instance == null || player == null) return;

        List<MusicLandmark> nearbyForDuet = new();
        List<LandmarkMixInput> mixInputs = new();

        foreach (MusicLandmark landmark in landmarks)
        {
            int landmarkId = landmark.LandmarkId;
            if (landmarkId < 0) continue;

            float distance = landmark.DistanceToPlayer;

            bool inDuetRange = distance <= landmark.DuetRadius;
            landmark.SetDuet(inDuetRange);

            if (inDuetRange)
            {
                nearbyForDuet.Add(landmark);
            }

            float influenceRadius = landmark.InfluenceRadius;

            int priority;
            if (distance > influenceRadius)
                priority = 0; 
            else if (distance > influenceRadius * 0.5f)
                priority = 1; 
            else
                priority = 2;

            SoundManager.Instance.UpdateLandmarkPriority(landmarkId, priority);

            mixInputs.Add(new LandmarkMixInput
            {
                LandmarkId = landmarkId,
                Distance = distance,
                InfluenceRadius = influenceRadius
            });
        }

        if (nearbyForDuet.Count == 2)
        {
            int duetAId = nearbyForDuet[0].LandmarkId;
            int duetBId = nearbyForDuet[1].LandmarkId;
            SoundManager.Instance.SetDuetPair(duetAId, duetBId);
        }
        else
        {
            SoundManager.Instance.ClearDuet();
        }

        SoundManager.Instance.UpdateMixing(mixInputs.ToArray());
    }
}