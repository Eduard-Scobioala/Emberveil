using System;
using System.Runtime.CompilerServices;
using UnityEngine;

public class CharacterManager : MonoBehaviour
{
    public event Action OnDeath;

    [Header("Lock On Transform")]
    public Transform lockOnTransform;

    [Header("Interaction Flags")]
    public bool isBeingCriticallyHit;
    
    [Header("Backstab Settings")]
    public Transform backstabReceiverPoint; // A child empty GameObject on the character model where the attacker snaps TO
    public string beingBackstabbedAnimation = "Enemy_Backstab_Main_Victim_01";
    public bool canBeBackstabbed = true;

    protected Animator charAnimator;
    public AnimatorManager charAnimManager;

    protected virtual void Awake()
    {
        charAnimator = GetComponentInChildren<Animator>();
        charAnimManager = GetComponentInChildren<AnimatorManager>();

        if (backstabReceiverPoint == null)
        {
            // Try to find a child named "BackstabReceiverPoint"
            var foundPoint = transform.Find("BackstabReceiverPoint");
            if (foundPoint != null)
            {
                backstabReceiverPoint = foundPoint;
            }
            else
            {
                // Or log a warning if it's critical and should be manually set
                Debug.LogWarning($"BackstabReceiverPoint not set for {gameObject.name}, backstabs might not align perfectly.");
            }
        }
    }

    public virtual void GetBackstabbed(Transform attacker)
    {
        if (!canBeBackstabbed || charAnimManager.IsInvulnerable || charAnimManager.IsInMidAction) return;

        Debug.Log($"{gameObject.name} is being backstabbed by {attacker.name}");
        charAnimManager.IsInMidAction = true;
        charAnimManager.IsInvulnerable = true;
        isBeingCriticallyHit = true;
        canBeBackstabbed = false; // Cannot be backstabbed again while being backstabbed

        // Orient this character to face away from the attacker
        Vector3 directionFromAttacker = transform.position - attacker.position;
        directionFromAttacker.y = 0; // Keep upright
        directionFromAttacker.Normalize();

        if (directionFromAttacker != Vector3.zero)
        {
            transform.rotation = Quaternion.LookRotation(directionFromAttacker);
        }

        if (charAnimManager != null)
        {
            charAnimManager.PlayTargetAnimation(beingBackstabbedAnimation, true);
        }
        else if (charAnimator != null) // Fallback if no specific manager
        {
            charAnimator.CrossFade(beingBackstabbedAnimation, 0.1f);
            charAnimManager.IsInMidAction = true;
        }
        else
        {
            Debug.LogError($"CharacterManager: Animator or AnimatorManager not found for {gameObject.name} to play {beingBackstabbedAnimation}!", this);
        }
    }

    // This method would be called by an Animation Event at the end of the "Being_Backstabbed" animation
    public virtual void FinishBeingBackstabbed()
    {
        charAnimManager.IsInMidAction = false;
        charAnimManager.IsInvulnerable = false;
        canBeBackstabbed = true;
        isBeingCriticallyHit = false;
        Debug.Log($"{gameObject.name} finished being backstabbed.");
    }

    // Death event used to clear the lock on
    protected void RaiseDeath()
    {
        OnDeath?.Invoke();
    }
}
