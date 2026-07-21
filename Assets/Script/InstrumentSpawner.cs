using NUnit.Framework;
using System.Collections.Generic;
using UnityEngine;

public class InstrumentSpawner : MonoBehaviour
{
    [SerializeField] private List<InstrumentData> instrumentsToSpawn;
    [SerializeField] private GameObject defaultPrefab;

    [SerializeField] private float innerRadius = 2.0f;
    [SerializeField] private float spiralGrowthRate = 1.5f;
    [SerializeField] private float separationDistance = 3.0f;

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

        float duetRadius = instrumentsToSpawn[0].duetRadius;
        float currentRadius = duetRadius / Mathf.Sin(Mathf.PI / instrumentsToSpawn.Count) * 0.8f;

        for (int i = 0; i < instrumentsToSpawn.Count; i++)
        {
            InstrumentData data = instrumentsToSpawn[i];
            if (data == null)
            {
                continue;
            }

            float angle = i * Mathf.PI * 2 / instrumentsToSpawn.Count;

            float x = currentRadius * Mathf.Cos(angle);
            float z = currentRadius * Mathf.Sin(angle);
            Vector3 localSpawnPos = new Vector3(x, 0f, z);

            Vector3 spawnPos = localSpawnPos + stageCenter;
            Vector3 lookDir = (stageCenter - spawnPos).normalized;

            GameObject prefabToSpawn = defaultPrefab;

            GameObject spawnedObj = Instantiate(prefabToSpawn, spawnPos, Quaternion.LookRotation(lookDir), transform);

            MusicLandmark landmark = spawnedObj.GetComponent<MusicLandmark>();
            landmark.instrumentData = data;

            spawnedObj.name = $"Landmark_{data.instrumentName}_{i}";
        }
    }
}
