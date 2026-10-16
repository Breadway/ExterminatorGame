using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

[Serializable]
public class SaveData
{
    public int version = 1;
    public int scrap;
    public int lifetimeScrap;
    public int bestRoom;
    public int bestLevel;
    public int totalRuns;
    public int totalKills;
    public int victories;
    public bool endlessUnlocked;
    public string selectedCharacter = "firebug";
    public float masterVolume = 0.8f;
    public float sfxVolume = 1f;
    public float musicVolume = 0.6f;
    public bool screenShake = true;
    public bool damageNumbers = true;
    public List<string> unlockedCharacters = new List<string> { "firebug" };
    public List<MetaLevel> metaLevels = new List<MetaLevel>();
    public List<string> unlockedDocs = new List<string>();
    public List<BestiaryCount> bestiary = new List<BestiaryCount>();
}

[Serializable]
public class BestiaryCount
{
    public string id;
    public int kills;
}

[Serializable]
public class MetaLevel
{
    public string id;
    public int level;
}

/// <summary>
/// Persistent save file in Application.persistentDataPath as JSON. Written atomically
/// (temp file then replace) and tolerant of a missing or corrupt file, which just starts fresh.
/// </summary>
public static class SaveSystem
{
    private static SaveData cached;
    private static string pathOverride;

    /// <summary>Tests point this at a temp file so they never touch a real save.</summary>
    public static void UsePath(string path)
    {
        pathOverride = path;
        cached = null;
    }

    private static string FilePath => pathOverride ?? Path.Combine(Application.persistentDataPath, "exterminator_save.json");

    public static SaveData Data
    {
        get
        {
            if (cached == null) cached = Load();
            return cached;
        }
    }

    public static SaveData Load()
    {
        try
        {
            if (File.Exists(FilePath))
            {
                var loaded = JsonUtility.FromJson<SaveData>(File.ReadAllText(FilePath));
                if (loaded != null)
                {
                    if (loaded.unlockedCharacters == null || loaded.unlockedCharacters.Count == 0)
                        loaded.unlockedCharacters = new List<string> { "firebug" };
                    if (loaded.metaLevels == null) loaded.metaLevels = new List<MetaLevel>();
                    if (loaded.unlockedDocs == null) loaded.unlockedDocs = new List<string>();
                    if (loaded.bestiary == null) loaded.bestiary = new List<BestiaryCount>();
                    cached = loaded;
                    return loaded;
                }
            }
        }
        catch (Exception e)
        {
            Debug.LogWarning($"Save file unreadable, starting fresh: {e.Message}");
        }
        cached = new SaveData();
        return cached;
    }

    public static void Save()
    {
        try
        {
            string path = FilePath;
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            string temp = path + ".tmp";
            File.WriteAllText(temp, JsonUtility.ToJson(Data, true));
            if (File.Exists(path)) File.Delete(path);
            File.Move(temp, path);
        }
        catch (Exception e)
        {
            Debug.LogWarning($"Could not write save: {e.Message}");
        }
    }

    public static void Reset()
    {
        cached = new SaveData();
        Save();
    }

    public static int GetMetaLevel(string id)
    {
        foreach (var m in Data.metaLevels) if (m.id == id) return m.level;
        return 0;
    }

    public static void SetMetaLevel(string id, int level)
    {
        foreach (var m in Data.metaLevels)
        {
            if (m.id == id) { m.level = level; return; }
        }
        Data.metaLevels.Add(new MetaLevel { id = id, level = level });
    }
}
