using UnityEngine;

public class Interactable : MonoBehaviour
{
    public float radius = 0.4f;
    public string interactableText;

    public void OnDrawGizmos()
    {
        Gizmos.color = Color.blue;
        Gizmos.DrawWireSphere(transform.position, radius);
    }

    public virtual void OnInteract(PlayerManager playerManager)
    {
        Debug.Log("You picked up an Item.");
    }
}
