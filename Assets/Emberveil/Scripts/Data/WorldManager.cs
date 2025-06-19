using UnityEngine;

public class WorldManager : MonoBehaviour
{
    public static WorldManager Instance { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
        }
        else
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
    }

    public void ResetWorldState()
    {
        Debug.Log("Resetting world state...");
        RespawnAllEnemies();
        RespawnAllItems();
    }

    private void RespawnAllEnemies()
    {
        var allEnemies = FindObjectsOfType<EnemyManager>(true); // Include inactive
        foreach (var enemy in allEnemies)
        {
            enemy.RespawnEnemy();
        }
        Debug.Log("All enemies have been respawned.");
    }

    private void RespawnAllItems()
    {
        var allItems = FindObjectsOfType<ItemPickUp>(true); // Include inactive
        foreach (var item in allItems)
        {
            item.RespawnItem();
        }
        Debug.Log("All items have been respawned.");
    }
}