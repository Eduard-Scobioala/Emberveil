using UnityEngine;

public class GameLoader : MonoBehaviour
{
    [Header("Audio")]
    [SerializeField] private SoundSO gameMusic;

    void Start()
    {
        if (SaveLoadManager.Instance == null)
        {
            Debug.LogError("GameLoader: SaveLoadManager instance not found! Make sure it's in your initial scene.");
            return;
        }

        if (SaveLoadManager.Instance.ShouldSaveOnStart)
        {
            Debug.Log("GameLoader: Loading saved game as requested by main menu.");
            SaveLoadManager.Instance.LoadGame();

            // Reset the flag so it doesn't try to load again on subsequent scene loads (if any).
            SaveLoadManager.Instance.ShouldSaveOnStart = false;
        }
        else
        {
            Debug.Log("GameLoader: Starting a new game (no load requested).");
            // No action needed, the game will just start with default values.
        }

        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayMusic(gameMusic);
        }
    }
}