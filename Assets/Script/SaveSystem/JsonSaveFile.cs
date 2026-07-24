using System;
using System.IO;
using UnityEngine;

public static class JsonSaveFile
{
    public static string RootPath =>
        Path.Combine(Application.persistentDataPath, "ResonanceSaveData");

    public static string ProfilePath =>
        Path.Combine(RootPath, "profile.json");

    public static string ConfigurationPath(string id) =>
        Path.Combine(RootPath, "configurations", $"config_{id}.json");

    public static bool Save<T>(string path, T data, out string error)
    {
        try
        {
            string directory = Path.GetDirectoryName(path);

            if (!string.IsNullOrEmpty(directory))
                Directory.CreateDirectory(directory);

            string temporaryPath = path + ".tmp";
            string backupPath = path + ".bak";
            string json = JsonUtility.ToJson(data, true);

            File.WriteAllText(temporaryPath, json);

            if (File.Exists(path))
                File.Copy(path, backupPath, true);

            if (File.Exists(path))
                File.Delete(path);

            File.Move(temporaryPath, path);

            error = string.Empty;
            return true;
        }
        catch (Exception exception)
        {
            error = exception.Message;
            return false;
        }
    }

    public static bool Load<T>(string path, out T data, out string error)
        where T : class
    {
        data = null;

        if (TryRead(path, out data))
        {
            error = string.Empty;
            return true;
        }

        if (TryRead(path + ".bak", out data))
        {
            error = "Main file failed. Loaded backup instead.";
            return true;
        }

        error = $"Could not load '{path}'.";
        return false;
    }

    public static bool Delete(string path, out string error)
    {
        try
        {
            DeleteIfPresent(path);
            DeleteIfPresent(path + ".bak");
            DeleteIfPresent(path + ".tmp");

            error = string.Empty;
            return true;
        }
        catch (Exception exception)
        {
            error = exception.Message;
            return false;
        }
    }

    private static bool TryRead<T>(string path, out T data)
        where T : class
    {
        data = null;

        if (!File.Exists(path))
            return false;

        try
        {
            data = JsonUtility.FromJson<T>(File.ReadAllText(path));
            return data != null;
        }
        catch
        {
            return false;
        }
    }

    private static void DeleteIfPresent(string path)
    {
        if (File.Exists(path))
            File.Delete(path);
    }
}
