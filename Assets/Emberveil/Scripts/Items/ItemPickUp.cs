using UnityEngine;
using UnityEngine.UI;

public class ItemPickUp : Interactable
{
    [SerializeField] private Item itemToPickUp;
    [SerializeField] private string pickUpAnimation = "Pick_Up_Item";

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

        Destroy(gameObject, 2f);
    }

    public override string GetItemName()
    {
        return itemToPickUp != null ? itemToPickUp.itemName : "Unknown Weapon";
    }

    public override Sprite GetItemIcon()
    {
        return itemToPickUp != null ? itemToPickUp.itemIcon : null;
    }
}
