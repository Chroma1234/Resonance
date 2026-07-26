using System.Diagnostics;
using System.IO;
using UnityEngine;
using Debug = UnityEngine.Debug;

public class SavePathTest : MonoBehaviour
{
    private void Start()
    {
        string folder = Path.GetFullPath(Application.persistentDataPath);
        string filePath = Path.Combine(folder, "SAVE_SYSTEM_TEST.json");

        Directory.CreateDirectory(folder);

        File.WriteAllText(
            filePath,
            "{\n  \"test\": true,\n  \"message\": \"Unity successfully created this file\"\n}"
        );

        Debug.Log("EXACT FOLDER: " + folder);
        Debug.Log("EXACT FILE: " + filePath);
        Debug.Log("FILE EXISTS: " + File.Exists(filePath));
        Debug.Log("FILE SIZE: " + new FileInfo(filePath).Length + " bytes");

        string[] files = Directory.GetFiles(folder);

        Debug.Log("FILES FOUND IN DIRECTORY: " + files.Length);

        foreach (string file in files)
        {
            Debug.Log("FOUND FILE: " + file);
        }

        Process.Start(new ProcessStartInfo
        {
            FileName = "explorer.exe",
            Arguments = "/select,\"" + filePath + "\"",
            UseShellExecute = true
        });
    }
}