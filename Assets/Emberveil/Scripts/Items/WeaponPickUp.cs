using UnityEngine;
using UnityEngine.UI;

public class WeaponPickUp : Interactable
{
    [SerializeField] private WeaponItem weaponToPickUp;
    [SerializeField] private string pickUpAnimation = "Pick_Up_Item";

    public override void OnInteract(PlayerManager playerManager)
    {
        if (weaponToPickUp == null)
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

        playerManager.playerInventory.AddWeaponToInventory(weaponToPickUp);

        // Disable interactable immediately to prevent re-interaction
        GetComponent<Collider>().enabled = false;
        // Visually disable or hide the pickup model
        foreach (Renderer r in GetComponentsInChildren<Renderer>()) r.enabled = false;
        foreach (Light l in GetComponentsInChildren<Light>()) l.enabled = false;


        // The pickup animation should have an AnimEvent_FinishAction which sets IsInMidAction false.
        // Destroy this GameObject after a delay (or after animation via another event from PlayerManager)
        // For simplicity now, destroy after a slight delay. A better way is an event from PlayerManager
        // when the pickup animation is truly finished.
        Destroy(gameObject, 2f); // Adjust delay or use animation event system
    }

    public override string GetItemName()
    {
        return weaponToPickUp != null ? weaponToPickUp.name : "Unknown Weapon";
    }

    public override Sprite GetItemIcon()
    {
        return weaponToPickUp != null ? weaponToPickUp.itemIcon : null;
    }
}
