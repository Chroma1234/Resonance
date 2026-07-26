using System.Collections.Generic;
using UnityEngine;

public class DuetManager : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform player;

    private MusicLandmark[] landmarks;
    private DuetSessionReporter duetReporter;

    private bool duetActive;

    private void Start()
    {
        if (player == null)
        {
            GameObject playerObject =
                GameObject.FindGameObjectWithTag("Player");

            if (playerObject != null)
            {
                player = playerObject.transform;
            }
        }

        landmarks =
            FindObjectsByType<MusicLandmark>(
                FindObjectsSortMode.None);

        duetReporter =
            FindFirstObjectByType<DuetSessionReporter>();
    }

    private void Update()
    {
        if (player == null)
        {
            return;
        }

        if (landmarks == null || landmarks.Length == 0)
        {
            landmarks =
                FindObjectsByType<MusicLandmark>(
                    FindObjectsSortMode.None);
        }

        List<MusicLandmark> nearbyForDuet =
            new List<MusicLandmark>();

        foreach (MusicLandmark landmark in landmarks)
        {
            if (landmark == null ||
                landmark.instrumentData == null)
            {
                continue;
            }

            float distance =
                Vector3.Distance(
                    player.position,
                    landmark.transform.position);

            bool inDuetRange =
                distance <=
                landmark.instrumentData.duetRadius;

            if (inDuetRange &&
                landmark.instrumentData.mixable)
            {
                nearbyForDuet.Add(landmark);
            }
        }

        bool shouldActivateDuet =
            nearbyForDuet.Count == 2;

        foreach (MusicLandmark landmark in landmarks)
        {
            if (landmark != null)
            {
                landmark.SetDuet(false);
            }
        }

        if (shouldActivateDuet)
        {
            nearbyForDuet[0].SetDuet(true);
            nearbyForDuet[1].SetDuet(true);

            if (!duetActive)
            {
                TutorialManager tutorial =
                    FindFirstObjectByType<TutorialManager>();

                if (tutorial != null)
                {
                    tutorial.TryAdvanceStep(
                        TutorialTriggerType.TriggerDuet);
                }
            }
        }

        if (duetReporter != null)
        {
            duetReporter.ReportDuetState(
                shouldActivateDuet);
        }

        duetActive = shouldActivateDuet;
    }
}