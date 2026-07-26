using NUnit.Framework;
using System.Collections.Generic;
using UnityEditor.IMGUI.Controls;
using UnityEngine;

public class InstrumentSpawner : MonoBehaviour
{
    [SerializeField] private InstrumentDatabase database;

    private List<InstrumentData> instrumentsToSpawn = new List<InstrumentData>();
    [SerializeField] private GameObject defaultPrefab;

    [SerializeField] private float arcSpawnAngle;

    private void Awake()
    {
        foreach(InstrumentData instr in database.instruments)
        {
            instrumentsToSpawn.Add(instr);
        }
    }

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

        if (instrumentsToSpawn.Count == 1)
        {
            InstrumentData data = instrumentsToSpawn[0];

            GameObject spawnedObj = Instantiate(defaultPrefab, stageCenter, Quaternion.identity, transform);

            MusicLandmark landmark = spawnedObj.GetComponent<MusicLandmark>();
            landmark.instrumentData = data;
            landmark.SetModel();

            spawnedObj.name = $"Landmark_{data.instrumentName}_0";
            return;
        }

        float angleStep = arcSpawnAngle * Mathf.Deg2Rad / (instrumentsToSpawn.Count - 1);
        float duetRadius = instrumentsToSpawn[0].duetRadius;

        float desiredSpacing = duetRadius * 2f * 0.8f;
        float currentRadius = desiredSpacing / (2f * Mathf.Sin(angleStep * 0.5f));

        for (int i = 0; i < instrumentsToSpawn.Count; i++)
        {
            InstrumentData data = instrumentsToSpawn[i];
            if (data == null)
                continue;

            float startAngle = -arcSpawnAngle * 0.5f;
            float angle = startAngle + (i / (float)(instrumentsToSpawn.Count - 1)) * arcSpawnAngle;
            angle *= Mathf.Deg2Rad;

            Vector3 localSpawnPos = new Vector3(
                currentRadius * Mathf.Cos(angle),
                0f,
                currentRadius * Mathf.Sin(angle));

            Vector3 spawnPos = stageCenter + localSpawnPos;
            Vector3 lookDir = (stageCenter - spawnPos).normalized;

            GameObject spawnedObj = Instantiate(defaultPrefab, spawnPos, Quaternion.LookRotation(lookDir), transform);

            MusicLandmark landmark = spawnedObj.GetComponent<MusicLandmark>();
            landmark.instrumentData = data;
            landmark.SetModel();

            spawnedObj.name = $"Landmark_{data.instrumentName}_{i}";
        }
    }
}