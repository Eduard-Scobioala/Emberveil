using UnityEngine;

public class Interactable : MonoBehaviour
{
    public string interactableInfoText;
    public virtual bool IsInteractablePickUp => false;

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            PlayerManager player = other.GetComponent<PlayerManager>();
            if (player != null)
            {
                player.AddInteractable(this);
            }
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            PlayerManager player = other.GetComponent<PlayerManager>();
            if (player != null)
            {
                player.RemoveInteractable(this);
            }
        }
    }

    public virtual void OnInteract(PlayerManager playerManager)
    {
        Debug.Log("You picked up an Item.");
    }

    public virtual string GetItemName()
    {
        return "Item";
    }

    public virtual Sprite GetItemIcon()
    {
        return null;
    }
}
