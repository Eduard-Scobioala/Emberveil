using System;
using UnityEngine;

public class EnemySenses : MonoBehaviour
{
    public event Action<CharacterManager> OnTargetSpotted;
    public event Action OnTargetLost; // Assumes current target

    private EnemyManager enemyManager;

    [Header("Detection Settings")]
    public float sightRadius = 20f;
    [Range(0, 360)] public float sightAngle = 90f; // Field of view angle
    public float loseSightDistanceMultiplier = 1.5f; // How much further than sightRadius before instant loss
    public float timeToLoseTarget = 5f; // Seconds of LoS break before forgetting target

    [Header("Layers")]
    public LayerMask targetLayers; // Player layer
    public LayerMask obstructionLayers; // Walls, environment

    public CharacterManager CurrentPerceivedTarget { get; private set; }
    private float currentLoseTargetTimer = 0f;
    private bool wasTargetVisibleLastFrame = false;

    public void Initialize(EnemyManager manager)
    {
        enemyManager = manager;
    }

    public void TickSenses()
    {
        if (CurrentPerceivedTarget != null)
        {
            HandleTargetRetention();
        }
        else
        {
            LookForNewTarget();
        }
    }

    private void LookForNewTarget()
    {
        Collider[] colliders = Physics.OverlapSphere(transform.position, sightRadius, targetLayers);
        foreach (var col in colliders)
        {
            CharacterManager potentialTarget = col.GetComponent<CharacterManager>();
            if (potentialTarget != null && potentialTarget != enemyManager && potentialTarget.isActiveAndEnabled) // Don't target self
            {
                if (IsTargetInSight(potentialTarget))
                {
                    SetNewTarget(potentialTarget);
                    return; // Found a target
                }
            }
        }
    }

    private void HandleTargetRetention()
    {
        if (CurrentPerceivedTarget == null || !CurrentPerceivedTarget.isActiveAndEnabled)
        {
            LoseCurrentTarget();
            return;
        }

        float distanceToTarget = Vector3.Distance(transform.position, CurrentPerceivedTarget.transform.position);
        if (distanceToTarget > sightRadius * loseSightDistanceMultiplier)
        {
            LoseCurrentTarget();
            return;
        }

        if (IsTargetInSight(CurrentPerceivedTarget))
        {
            currentLoseTargetTimer = 0f;
            wasTargetVisibleLastFrame = true;
        }
        else
        {
            if (wasTargetVisibleLastFrame) // Just lost sight
            {
                currentLoseTargetTimer = 0f; // Start timer now
            }
            wasTargetVisibleLastFrame = false;
            currentLoseTargetTimer += Time.deltaTime;
            if (currentLoseTargetTimer >= timeToLoseTarget)
            {
                LoseCurrentTarget();
            }
        }
    }

    private bool IsTargetInSight(CharacterManager target)
    {
        if (target == null || target.lockOnTransform == null) return false;

        Vector3 directionToTarget = target.lockOnTransform.position - transform.position;
        float angle = Vector3.Angle(transform.forward, directionToTarget.normalized);

        if (angle > sightAngle / 2f)
        {
            return false; // Outside FoV
        }

        // Use a point slightly above enemy's base for LoS origin
        Vector3 sightOrigin = transform.position + Vector3.up * (enemyManager.GetComponent<UnityEngine.AI.NavMeshAgent>()?.height * 0.8f ?? 1.5f);
        Vector3 targetPoint = target.lockOnTransform.position;

        if (Physics.Linecast(sightOrigin, targetPoint, out RaycastHit hit, obstructionLayers, QueryTriggerInteraction.Ignore))
        {
            if (hit.transform.root != target.transform.root) // Hit something other than the target
            {
                return false; // Obstructed
            }
        }
        return true; // In FoV and LoS is clear or hit target itself
    }

    private void SetNewTarget(CharacterManager target)
    {
        CurrentPerceivedTarget = target;
        wasTargetVisibleLastFrame = true;
        currentLoseTargetTimer = 0f;
        OnTargetSpotted?.Invoke(target);
        Debug.Log($"{enemyManager.name} spotted target: {target.name}");
    }

    private void LoseCurrentTarget()
    {
        if (CurrentPerceivedTarget != null)
        {
            Debug.Log($"{enemyManager.name} lost target: {CurrentPerceivedTarget.name}");
        }
        CurrentPerceivedTarget = null;
        wasTargetVisibleLastFrame = false;
        currentLoseTargetTimer = 0f;
        OnTargetLost?.Invoke();
    }

    public void ForceLoseTarget()
    {
        LoseCurrentTarget();
    }

    void OnDrawGizmosSelected()
    {
        if (enemyManager == null) return;

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, sightRadius);

        Vector3 fovLine1 = Quaternion.AngleAxis(sightAngle / 2, transform.up) * transform.forward * sightRadius;
        Vector3 fovLine2 = Quaternion.AngleAxis(-sightAngle / 2, transform.up) * transform.forward * sightRadius;

        Gizmos.color = Color.blue;
        Gizmos.DrawRay(transform.position, fovLine1);
        Gizmos.DrawRay(transform.position, fovLine2);

        if (CurrentPerceivedTarget != null && CurrentPerceivedTarget.lockOnTransform != null)
        {
            Gizmos.color = IsTargetInSight(CurrentPerceivedTarget) ? Color.green : Color.red;
            Vector3 sightOrigin = transform.position + Vector3.up * (enemyManager.GetComponent<UnityEngine.AI.NavMeshAgent>()?.height * 0.8f ?? 1.5f);
            Gizmos.DrawLine(sightOrigin, CurrentPerceivedTarget.lockOnTransform.position);
        }
    }
}
