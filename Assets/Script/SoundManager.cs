using System.Collections.Generic;
using UnityEngine;

public class SoundManager : MonoBehaviour
{
    public static SoundManager Instance { get; private set; }

    private readonly List<MusicLandmark> landmarks = new();

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public void Register(MusicLandmark landmark)
    {
        if (landmark == null)
        {
            return;
        }

        if (!landmarks.Contains(landmark))
        {
            landmarks.Add(landmark);
        }
    }

    public void Unregister(MusicLandmark landmark)
    {
        landmarks.Remove(landmark);
    }

    private void Update()
    {
        foreach (var landmark in landmarks)
        {
            if (landmark != null)
            {
                landmark.SetDuet(false);
            }
        }

        List<MusicLandmark> nearby = new();

        foreach (var landmark in landmarks)
        {
            if (landmark == null)
            {
                continue;
            }

            if (landmark.PlayerInDuetRange)
            {
                nearby.Add(landmark);
            }
        }

        if (nearby.Count == 2)
        {
            nearby[0].SetDuet(true);
            nearby[1].SetDuet(true);
        }
    }
}