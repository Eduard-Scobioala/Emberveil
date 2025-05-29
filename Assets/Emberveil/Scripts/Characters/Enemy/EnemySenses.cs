using System;
using UnityEngine;

public class EnemySenses : MonoBehaviour
{
    public event Action<CharacterManager> OnTargetSpotted;
    public event Action OnTargetLost; // Assumes current target

    private EnemyManager enemyManager;

    [Header("Vision Settings")]
    public float sightRadius = 20f;
    [Range(0, 360)] public float sightAngle = 90f;
    public float loseSightDistanceMultiplier = 1.5f;
    public float timeToLoseTargetAfterLoSBreak = 5f;

    [Header("Proximity Settings")]
    public float proximityDetectionRadius = 5f;
    public float timeToLoseTargetAfterProximityBreak = 1.5f;

    [Header("Layers")]
    public LayerMask targetLayers; // Player layer
    public LayerMask obstructionLayers; // Walls, environment

    public CharacterManager CurrentPerceivedTarget { get; private set; }
    private float currentLoseTargetTimer = 0f;
    private bool wasTargetVisibleLastFrame_LoS = false;
    private bool wasTargetDetectedLastFrame_Proximity = false;
    private enum DetectionMethod { None, LoS, Proximity }
    private DetectionMethod lastDetectionMethod = DetectionMethod.None;

    public void Initialize(EnemyManager manager)
    {
        enemyManager = manager;
    }

    //public void TickSenses2()
    //{
    //    if (CurrentPerceivedTarget != null)
    //    {
    //        HandleTargetRetention();
    //    }
    //    else
    //    {
    //        LookForNewTarget();
    //    }
    //}

    public void TickSenses()
    {
        if (enemyManager.CurrentState is DeadState) // Don't sense if dead
        {
            if (CurrentPerceivedTarget != null) LoseCurrentTarget();
            return;
        }

        CharacterManager potentialTargetByLoS = null;
        CharacterManager potentialTargetByProximity = null;

        // --- Look for new target or update existing ---
        Collider[] collidersInVision = Physics.OverlapSphere(transform.position, sightRadius, targetLayers);
        foreach (var col in collidersInVision)
        {
            CharacterManager pTarget = col.GetComponent<CharacterManager>();
            if (pTarget != null && pTarget != enemyManager && pTarget.isActiveAndEnabled)
            {
                if (IsTargetInDirectSight(pTarget)) // Line of Sight check
                {
                    potentialTargetByLoS = pTarget;
                    break; // Prioritize LoS target
                }
            }
        }

        // Proximity check (only if no LoS target found or to confirm existing proximity target)
        // Or, always check proximity and LoS gives higher priority.
        // Let's check proximity regardless, LoS will override if found.
        Collider[] collidersInProximity = Physics.OverlapSphere(transform.position, proximityDetectionRadius, targetLayers);
        foreach (var col in collidersInProximity)
        {
            CharacterManager pTarget = col.GetComponent<CharacterManager>();
            if (pTarget != null && pTarget != enemyManager && pTarget.isActiveAndEnabled)
            {
                PlayerManager player = pTarget as PlayerManager;
                if (player != null && !player.isCrouching) // Proximity only detects non-crouching players
                {
                    potentialTargetByProximity = pTarget;
                    break; // Found a non-crouching player by proximity
                }
                // If not a player, or a crouching player, proximity alone doesn't detect.
            }
        }

        // --- Determine Actual Target and Handle Retention/Loss ---
        CharacterManager newlyDetectedTarget = null;
        DetectionMethod currentDetectionMethod = DetectionMethod.None;

        if (potentialTargetByLoS != null)
        {
            newlyDetectedTarget = potentialTargetByLoS;
            currentDetectionMethod = DetectionMethod.LoS;
        }
        else if (potentialTargetByProximity != null)
        {
            newlyDetectedTarget = potentialTargetByProximity;
            currentDetectionMethod = DetectionMethod.Proximity;
        }

        // --- Handle Target State Changes ---
        if (CurrentPerceivedTarget == null && newlyDetectedTarget != null) // Fresh detection
        {
            SetNewTarget(newlyDetectedTarget, currentDetectionMethod);
        }
        else if (CurrentPerceivedTarget != null && newlyDetectedTarget == CurrentPerceivedTarget) // Still detecting the same target
        {
            // Target retained, update detection method and reset timer
            currentLoseTargetTimer = 0f;
            lastDetectionMethod = currentDetectionMethod; // Update how we are currently seeing them
            wasTargetVisibleLastFrame_LoS = (currentDetectionMethod == DetectionMethod.LoS);
            wasTargetDetectedLastFrame_Proximity = (currentDetectionMethod == DetectionMethod.Proximity || currentDetectionMethod == DetectionMethod.LoS); // LoS implies proximity
        }
        else if (CurrentPerceivedTarget != null && newlyDetectedTarget != CurrentPerceivedTarget) // Switched target or lost old, found new
        {
            LoseCurrentTarget(); // Lose old one first
            if (newlyDetectedTarget != null) SetNewTarget(newlyDetectedTarget, currentDetectionMethod); // Then set new one
        }
        else if (CurrentPerceivedTarget != null && newlyDetectedTarget == null) // Lost target completely this frame
        {
            HandleTargetLossTimer();
        }
    }

    private void HandleTargetLossTimer()
    {
        if (CurrentPerceivedTarget == null) return;

        // If target moves beyond hard max distance (e.g. sightRadius * multiplier)
        float distanceToTarget = Vector3.Distance(transform.position, CurrentPerceivedTarget.transform.position);
        if (distanceToTarget > sightRadius * loseSightDistanceMultiplier)
        {
            LoseCurrentTarget();
            return;
        }

        // If previously detected, start or continue loss timer
        bool wasDetectedLastFrame = wasTargetVisibleLastFrame_LoS || wasTargetDetectedLastFrame_Proximity;

        if (wasDetectedLastFrame) // Just lost sight/proximity this frame
        {
            currentLoseTargetTimer = 0f; // Reset timer only if it was detected last frame by any means
        }

        currentLoseTargetTimer += Time.deltaTime;
        float maxTimeToLose = (lastDetectionMethod == DetectionMethod.LoS) ? timeToLoseTargetAfterLoSBreak : timeToLoseTargetAfterProximityBreak;

        if (currentLoseTargetTimer >= maxTimeToLose)
        {
            LoseCurrentTarget();
        }
        // Update "last frame" flags after timer logic
        wasTargetVisibleLastFrame_LoS = false;
        wasTargetDetectedLastFrame_Proximity = false;
    }

    private bool IsTargetInDirectSight(CharacterManager target)
    {
        if (target == null || target.lockOnTransform == null) return false;
        Vector3 directionToTarget = target.lockOnTransform.position - transform.position;
        if (directionToTarget.magnitude > sightRadius) return false; // Check distance first

        float angle = Vector3.Angle(transform.forward, directionToTarget.normalized);
        if (angle > sightAngle / 2f) return false;

        Vector3 sightOrigin = transform.position + Vector3.up * (enemyManager.GetComponent<UnityEngine.AI.NavMeshAgent>()?.height * 0.8f ?? 1.5f);
        if (Physics.Linecast(sightOrigin, target.lockOnTransform.position, out RaycastHit hit, obstructionLayers, QueryTriggerInteraction.Ignore))
        {
            return hit.transform.root == target.transform.root; // True if hit target, false if obstructed
        }
        return true; // Clear LoS
    }

    private void SetNewTarget(CharacterManager target, DetectionMethod method)
    {
        CurrentPerceivedTarget = target;
        lastDetectionMethod = method;
        currentLoseTargetTimer = 0f;
        wasTargetVisibleLastFrame_LoS = (method == DetectionMethod.LoS);
        wasTargetDetectedLastFrame_Proximity = (method == DetectionMethod.Proximity || method == DetectionMethod.LoS);
        OnTargetSpotted?.Invoke(target);
        Debug.Log($"{enemyManager.name} spotted target: {target.name} via {method}");
    }

    private void LoseCurrentTarget()
    {
        if (CurrentPerceivedTarget != null)
        {
            Debug.Log($"{enemyManager.name} lost target: {CurrentPerceivedTarget.name} (was {lastDetectionMethod})");
        }
        CurrentPerceivedTarget = null;
        lastDetectionMethod = DetectionMethod.None;
        currentLoseTargetTimer = 0f;
        wasTargetVisibleLastFrame_LoS = false;
        wasTargetDetectedLastFrame_Proximity = false;
        OnTargetLost?.Invoke();
    }

    public void ForceLoseTarget() => LoseCurrentTarget();

    void OnDrawGizmosSelected()
    {
        if (enemyManager == null && TryGetComponent(out enemyManager)) { /* Initialized for gizmo */ }
        if (enemyManager == null) return;


        // Vision Cone
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, sightRadius);
        Vector3 fovLine1 = Quaternion.AngleAxis(sightAngle / 2, transform.up) * transform.forward * sightRadius;
        Vector3 fovLine2 = Quaternion.AngleAxis(-sightAngle / 2, transform.up) * transform.forward * sightRadius;
        Gizmos.color = Color.blue;
        Gizmos.DrawRay(transform.position, fovLine1);
        Gizmos.DrawRay(transform.position, fovLine2);

        // Proximity Radius
        Gizmos.color = new Color(0.5f, 0, 0.5f, 0.3f); // Purpleish
        Gizmos.DrawWireSphere(transform.position, proximityDetectionRadius);


        if (CurrentPerceivedTarget != null && CurrentPerceivedTarget.lockOnTransform != null)
        {
            Gizmos.color = (lastDetectionMethod == DetectionMethod.LoS) ? Color.green : ((lastDetectionMethod == DetectionMethod.Proximity) ? Color.magenta : Color.red);
            Vector3 sightOrigin = transform.position + Vector3.up * (enemyManager.GetComponent<UnityEngine.AI.NavMeshAgent>()?.height * 0.8f ?? 1.5f);
            Gizmos.DrawLine(sightOrigin, CurrentPerceivedTarget.lockOnTransform.position);
        }
    }
}
