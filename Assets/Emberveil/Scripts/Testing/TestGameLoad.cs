using UnityEngine;

public class TestGameLoad : MonoBehaviour
{
    private void Start()
    {
        if (SaveLoadManager.Instance.HasSaveFile())
        {
            SaveLoadManager.Instance.LoadGame();
        }
    }
}
