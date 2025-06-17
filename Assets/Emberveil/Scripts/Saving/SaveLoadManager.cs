using UnityEngine;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using System.Collections.Generic;

public class SaveLoadManager : MonoBehaviour
{
    public static SaveLoadManager Instance { get; private set; }

    [SerializeField] private string saveFileName = "emberveil.json";

    private JsonSerializerSettings serializerSettings;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        serializerSettings = new JsonSerializerSettings
        {
            TypeNameHandling = TypeNameHandling.All,
            Converters = new List<JsonConverter> { new Vector3Converter(), new QuaternionConverter() }
        };
    }

    private string GetSavePath()
    {
        return Path.Combine(Application.persistentDataPath, saveFileName);
    }

    public void SaveGame()
    {
        SaveData saveData = new ();

        // Find all ISavable objects in the current scene and capture their state.
        var allSavables = FindObjectsOfType<MonoBehaviour>(true).OfType<ISavable>().ToList();
        foreach (var savable in allSavables)
        {
            saveData.savableEntitiesData[savable.GetUniqueIdentifier()] = savable.CaptureState();
        }

        saveData.lastUpdated = System.DateTime.Now.ToBinary();

        string json = JsonConvert.SerializeObject(saveData, Formatting.Indented, serializerSettings);

        File.WriteAllText(GetSavePath(), json);
        Debug.Log($"Game saved to: {GetSavePath()}");
    }

    public void LoadGame()
    {
        string path = GetSavePath();
        if (!File.Exists(path))
        {
            Debug.Log("No save file found. Starting a new game.");
            return; // TODO: create a new game state here
        }

        string json = File.ReadAllText(path);
        SaveData saveData = JsonConvert.DeserializeObject<SaveData>(json, serializerSettings);

        // Find all ISavable objects and restore their state.
        foreach (var savable in FindObjectsOfType<MonoBehaviour>().OfType<ISavable>())
        {
            string id = savable.GetUniqueIdentifier();
            if (saveData.savableEntitiesData.TryGetValue(id, out object state))
            {
                savable.RestoreState(state);
            }
        }

        Debug.Log("Game loaded.");
    }

    public bool HasSaveFile()
    {
        return File.Exists(GetSavePath());
    }

    public void DeleteSaveFile()
    {
        string path = GetSavePath();
        if (File.Exists(path))
        {
            File.Delete(path);
            Debug.Log("Save file deleted.");
        }
    }
}
