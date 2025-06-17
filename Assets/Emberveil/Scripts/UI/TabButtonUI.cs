using UnityEngine;

public class TabButtonUI : MonoBehaviour
{
    [SerializeField] private GameObject activeSprite;
    [SerializeField] private GameObject inactiveSprite;

    private void Awake()
    {
        if (activeSprite == null) Debug.LogWarning($"Active Sprite for Inventory Tab [{gameObject.name}]");
        if (inactiveSprite == null) Debug.LogWarning($"Inactive Sprite for Inventory Tab [{gameObject.name}]");
    }

    public void SetActiveState(bool isActive)
    {
        activeSprite.SetActive(isActive);
        inactiveSprite.SetActive(!isActive);
    }
}