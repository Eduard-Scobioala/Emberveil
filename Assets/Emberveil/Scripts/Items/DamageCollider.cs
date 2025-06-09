using System;
using System.Collections.Generic;
using UnityEngine;

public class DamageCollider : MonoBehaviour
{
    public event Action<Collider> OnDamageableHit;

    private Collider _collider;
    private readonly List<Collider> _collidedTargetsThisSwing = new();

    [Header("Debug")]
    [SerializeField] private bool logHits = false;

    // Set by WeaponSlotManager when the weapon is equipped.
    public CharacterManager Wielder { get; set; }


    private void Awake()
    {
        _collider = GetComponent<Collider>();
        if (_collider == null)
        {
            Debug.LogError($"DamageCollider on {gameObject.name} is missing its Collider component!", this);
            enabled = false;
            return;
        }
        _collider.gameObject.SetActive(true);
        _collider.isTrigger = true;
        _collider.enabled = false; // Start disabled
    }

    public void EnableDamageCollider()
    {
        if (Wielder == null)
        {
            Debug.LogWarning($"DamageCollider on {gameObject.name} enabled without a Wielder assigned. It might not function correctly for player/enemy distinctions.");
        }
        _collidedTargetsThisSwing.Clear();
        _collider.enabled = true;
        if (logHits) Debug.Log($"{Wielder?.name ?? "Unknown Wielder"}'s DamageCollider Enabled.");
    }

    public void DisableDamageCollider()
    {
        _collider.enabled = false;
        if (logHits) Debug.Log($"{Wielder?.name ?? "Unknown Wielder"}'s DamageCollider Disabled.");
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!_collider.enabled || Wielder == null) return;

        // Prevent hitting self
        if (other.transform.root == Wielder.transform.root)
        {
            return;
        }

        if (_collidedTargetsThisSwing.Contains(other))
        {
            return; // Already hit this target in this swing
        }

        _collidedTargetsThisSwing.Add(other);

        // Notify the owner that a damageable target was hit
        OnDamageableHit?.Invoke(other);

        if (logHits) Debug.Log($"{Wielder.name}'s DamageCollider hit {other.name}. Event invoked.");
    }

    private float MaxComponent(Vector3 v) => Mathf.Max(Mathf.Max(v.x, v.y), v.z);
}
