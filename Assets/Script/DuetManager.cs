using System.Collections.Generic;
using UnityEngine;

public class DuetManager : MonoBehaviour
{
    [SerializeField] private MusicLandmark[] landmarks;

    private void Start()
    {
        landmarks = FindObjectsByType<MusicLandmark>(FindObjectsSortMode.None);
    }

    private void Update()
    {
        List<MusicLandmark> nearby = new();

        foreach (MusicLandmark landmark in landmarks)
        {
            landmark.SetDuet(false);

            if (landmark.PlayerInDuetRange)
            {
                nearby.Add(landmark);
            }
            else
            {
                nearby.Remove(landmark);
            }
        }

        if (nearby.Count == 2)
        {
            nearby[0].SetDuet(true);
            nearby[1].SetDuet(true);
        }
    }
}