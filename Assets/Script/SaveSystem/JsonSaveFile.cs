using System;
using System.IO;
using UnityEngine;

public static class JsonSaveFile
{
    public static string RootPath
    {
        get { return Path.Combine(Application.persistentDataPath, "ResonanceSaveData"); }
    }

    public static string StatisticsPath
    {
        get { return Path.Combine(RootPath, "statistics.json"); }
    }

    public static string ConfigurationDirectory
    {
        get { return Path.Combine(RootPath, "configurations"); }
    }

    public static string ConfigurationPath(string id)
    {
        return Path.Combine(ConfigurationDirectory, "config_" + id + ".json");
    }

    public static string[] ListConfigurationFiles()
    {
        if (!Directory.Exists(ConfigurationDirectory))
        {
            return new string[0];
        }

        return Directory.GetFiles(ConfigurationDirectory, "config_*.json");
    }

    public static bool Save<T>(string path, T data, out string error)
    {
        if (data == null)
        {
            error = "Cannot save null data to '" + path + "'.";
            return false;
        }

        try
        {
            string directory = Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(directory))
            {
                Directory.CreateDirectory(directory);
            }

            string temporaryPath = path + ".tmp";
            string backupPath = path + ".bak";

            string json = JsonUtility.ToJson(data, true);
            File.WriteAllText(temporaryPath, json);

            if (File.Exists(path))
            {
                File.Copy(path, backupPath, true);
                File.Delete(path);
            }

            File.Move(temporaryPath, path);

            error = string.Empty;
            return true;
        }
        catch (Exception exception)
        {
            error = "Save failed for '" + path + "': " + exception.Message;
            return false;
        }
    }

    public static bool Load<T>(string path, out T data, out string error) where T : class
    {
        if (TryRead(path, out data))
        {
            error = string.Empty;
            return true;
        }

        if (TryRead(path + ".bak", out data))
        {
            error = "Main file was missing or unreadable. Loaded the .bak backup for '" + path + "'.";
            return true;
        }

        error = "Could not load '" + path + "'. The file does not exist or is not valid JSON.";
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
            error = "Delete failed for '" + path + "': " + exception.Message;
            return false;
        }
    }

    private static bool TryRead<T>(string path, out T data) where T : class
    {
        data = null;

        if (!File.Exists(path))
        {
            return false;
        }

        try
        {
            data = JsonUtility.FromJson<T>(File.ReadAllText(path));
            return data != null;
        }
        catch (Exception exception)
        {
            Debug.LogWarning("JsonSaveFile: Failed to parse '" + path + "': " + exception.Message);
            return false;
        }
    }

    private static void DeleteIfPresent(string path)
    {
        if (File.Exists(path))
        {
            File.Delete(path);
        }
    }
}
