using UnityEngine;
using UnityEngine.UI;

public class WeaponPickUp : Interactable
{
    [SerializeField] private WeaponItem weapon;
    [SerializeField] private string pickUpAnimation = "Pick_Up_Item";
    [SerializeField] private float pickUpTime = 1f;

    public override void OnInteract(PlayerManager playerManager)
    {
        base.OnInteract(playerManager);

        PickUpItem(playerManager);
    }

    private void PickUpItem(PlayerManager playerManager)
    {
        PlayerInventory playerInventory = playerManager.GetComponent<PlayerInventory>();
        PlayerLocomotion playerLocomotion = playerManager.GetComponent<PlayerLocomotion>();
        PlayerAnimator animatorHandler = playerManager.GetComponentInChildren<PlayerAnimator>();

        playerLocomotion.rigidbody.velocity = Vector3.zero;
        animatorHandler.PlayTargetAnimation(pickUpAnimation, true);
        playerInventory.weaponsInventory.Add(weapon);

        Invoke(nameof(DestroyGameObject), pickUpTime);
    }

    private void DestroyGameObject()
    {
        Destroy(gameObject);
    }

    public override string GetItemName()
    {
        return weapon.name;
    }

    public override Sprite GetItemIcon()
    {
        return weapon.itemIcon;
    }
}
