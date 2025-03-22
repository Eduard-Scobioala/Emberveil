using UnityEngine;

public class WeaponPickUp : Interactable
{
    [SerializeField] private WeaponItem weapon;
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
        AnimatorHandler animatorHandler = playerManager.GetComponentInChildren<AnimatorHandler>();

        playerLocomotion.rigidbody.velocity = Vector3.zero;
        animatorHandler.PlayTargetAnimation("Pick_Up_Item", true);
        playerInventory.weaponsInventary.Add(weapon);

        Invoke(nameof(DestroyGameObject), pickUpTime);
    }

    void DestroyGameObject()
    {
        Destroy(gameObject);
    }
}
