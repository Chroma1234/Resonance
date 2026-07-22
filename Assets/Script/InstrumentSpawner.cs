using NUnit.Framework;
using System.Collections.Generic;
using UnityEditor.IMGUI.Controls;
using UnityEngine;

public class InstrumentSpawner : MonoBehaviour
{
    [SerializeField] private List<InstrumentData> instrumentsToSpawn;
    [SerializeField] private GameObject defaultPrefab;

    [SerializeField] private float arcSpawnAngle;

    void Start()
    {
        if (instrumentsToSpawn == null || instrumentsToSpawn.Count == 0)
        {
            return;
        }

        SpawnBand();
    }

    private void SpawnBand()
    {
        Vector3 stageCenter = transform.position;

        float angleStep = arcSpawnAngle * Mathf.Deg2Rad / (instrumentsToSpawn.Count - 1);
        float duetRadius = instrumentsToSpawn[0].duetRadius;

        float desiredSpacing = duetRadius * 2f * 0.8f;

        float currentRadius = desiredSpacing / (2f * Mathf.Sin(angleStep * 0.5f));

        for (int i = 0; i < instrumentsToSpawn.Count; i++)
        {
            InstrumentData data = instrumentsToSpawn[i];
            if (data == null)
            {
                continue;
            }

            float startAngle = -arcSpawnAngle * 0.5f;
            float angle = startAngle + (i / (float)(instrumentsToSpawn.Count - 1)) * arcSpawnAngle;

            angle *= Mathf.Deg2Rad;

            float x = currentRadius * Mathf.Cos(angle);
            float z = currentRadius * Mathf.Sin(angle);
            Vector3 localSpawnPos = new Vector3(x, 0f, z);

            Vector3 spawnPos = localSpawnPos + stageCenter;
            Vector3 lookDir = (stageCenter - spawnPos).normalized;

            GameObject prefabToSpawn = defaultPrefab;

            GameObject spawnedObj = Instantiate(prefabToSpawn, spawnPos, Quaternion.LookRotation(lookDir), transform);

            MusicLandmark landmark = spawnedObj.GetComponent<MusicLandmark>();
            landmark.instrumentData = data;

            landmark.SetModel();

            spawnedObj.name = $"Landmark_{data.instrumentName}_{i}";
        }
    }
}