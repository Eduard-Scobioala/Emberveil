using System.Collections;
using UnityEngine;

[RequireComponent(typeof(SavableEntity))]
public class ItemPickUp : Interactable, ISavable
{
    [SerializeField] private Item itemToPickUp;
    [SerializeField] private string pickUpAnimation = "Pick_Up_Item";

    [Header("Save/Load Settings")]
    [SerializeField] private bool isOneTimePickup = false;

    public override bool IsInteractablePickUp => true;

    private bool hasBeenCollected = false;
    private SavableEntity savableEntity;

    private void Awake()
    {
        savableEntity = GetComponent<SavableEntity>();
    }

    // Setting the item dynamically (for when dropping from inventory)
    public void SetItem(Item item)
    {
        interactableInfoText = item.itemName;
        itemToPickUp = item;
    }

    public override void OnInteract(PlayerManager playerManager)
    {
        if (itemToPickUp == null)
        {
            Debug.LogError("WeaponPickUp: weaponToPickUp is not assigned!", this);
            return;
        }
        PickUpItem(playerManager);
    }

    private void PickUpItem(PlayerManager playerManager)
    {
        if (playerManager.playerAnimator.IsInMidAction) return; // Don't pickup if busy

        playerManager.playerAnimator.IsInMidAction = true;
        playerManager.playerLocomotion.rigidbody.velocity = Vector3.zero;
        playerManager.playerAnimator.PlayTargetAnimation(pickUpAnimation, true);

        playerManager.playerInventory.AddItem(itemToPickUp);
        hasBeenCollected = true;

        // Update UI prompt
        var interactable = playerManager.interactableUI;
        interactable.itemInfoText.text = GetItemName();
        interactable.itemImage.sprite = GetItemIcon();
        interactable.EnableInteractionPopUpGameObject(false);
        interactable.EnableItemPopUpGameObject(true);

        // Disable interactable immediately
        GetComponent<Collider>().enabled = false;

        // Visually disable or hide the pickup model
        foreach (Renderer r in GetComponentsInChildren<Renderer>()) r.enabled = false;

        StartCoroutine(DisableAfterDelay(2f));
    }

    IEnumerator DisableAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        gameObject.SetActive(false);
    }

    public override string GetItemName()
    {
        return itemToPickUp != null ? itemToPickUp.itemName : "Unknown Weapon";
    }

    public override Sprite GetItemIcon()
    {
        return itemToPickUp != null ? itemToPickUp.itemIcon : null;
    }

    #region Saving and Loading
    public string GetUniqueIdentifier()
    {
        return savableEntity.GetUniqueIdentifier();
    }

    public object CaptureState()
    {
        // If it's a one-time pickup, we save whether it has been collected.
        if (isOneTimePickup)
        {
            return hasBeenCollected;
        }
        return null; // Don't save state for respawning items.
    }

    public void RestoreState(object state)
    {
        if (!isOneTimePickup) return;

        if (state is bool collected)
        {
            hasBeenCollected = collected;
            if (hasBeenCollected)
            {
                // If the item was already collected, destroy this pickup immediately on load.
                Debug.Log($"One-time pickup {itemToPickUp.name} ({GetUniqueIdentifier()}) was already collected. Destroying.");
                gameObject.SetActive(false);
            }
        }
    }
    #endregion
}
