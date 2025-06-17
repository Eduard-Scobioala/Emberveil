using System.Collections.Generic;

[System.Serializable]
public class SaveData
{
    public long lastUpdated;

    // A dictionary where the key is the unique ID from ISavable
    // and the value is the state object captured by that ISavable component.
    public Dictionary<string, object> savableEntitiesData = new ();

    public SaveData()
    {
        savableEntitiesData = new Dictionary<string, object>();
    }
}