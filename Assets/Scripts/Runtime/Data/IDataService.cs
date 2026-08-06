using System;
using System.IO;
using UnityEngine;

public interface IDataService
{
    bool HasData(string relativePath);
    bool SaveData<T>(string relativePath, T data);
    T LoadData<T>(string relativePath);
}

public class JsonDataService : IDataService
{
    public bool HasData(string relativePath)
    {
        return File.Exists(GetFullPath(relativePath));
    }

    public bool SaveData<T>(string relativePath, T data)
    {
        string path = GetFullPath(relativePath);

        try
        {
            string directory = Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                Directory.CreateDirectory(directory);

            string json = JsonUtility.ToJson(data, true);
            File.WriteAllText(path, json);
            return true;
        }
        catch (Exception exception)
        {
            Debug.LogError("[JsonDataService] Unable to save data: " + exception.Message);
            return false;
        }
    }

    public T LoadData<T>(string relativePath)
    {
        string path = GetFullPath(relativePath);
        if (!File.Exists(path))
            throw new FileNotFoundException(path + " does not exist.");

        try
        {
            string json = File.ReadAllText(path);
            return JsonUtility.FromJson<T>(json);
        }
        catch (Exception exception)
        {
            Debug.LogError("[JsonDataService] Unable to load data: " + exception.Message);
            throw;
        }
    }

    private static string GetFullPath(string relativePath)
    {
        string normalizedPath = string.IsNullOrWhiteSpace(relativePath)
            ? "player_save.json"
            : relativePath.TrimStart('/', '\\');

        return Path.Combine(Application.persistentDataPath, normalizedPath);
    }
}
