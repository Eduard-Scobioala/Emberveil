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
    public float timeToLoseTargetAfterProximityBreak = 2f;

    [Header("Layers")]
    public LayerMask targetLayers; // Player layer
    public LayerMask obstructionLayers; // Walls, environment

    public CharacterManager CurrentPerceivedTarget { get; private set; }
    private float currentLoseTargetTimer = 0f;
    private enum DetectionMethod { None, LoS, Proximity }
    private DetectionMethod lastFrameDetectionMethod = DetectionMethod.None;

    public void Initialize(EnemyManager manager)
    {
        enemyManager = manager;
    }

    public void TickSenses()
    {
        if (enemyManager.CurrentState is DeadState)
        {
            if (CurrentPerceivedTarget != null) LoseCurrentTargetAndNotify();
            return;
        }

        CharacterManager freshlyDetectedThisFrame = null;
        DetectionMethod currentFrameDetectionMethod = DetectionMethod.None;

        // --- Step 1: Attempt to Detect a Target THIS FRAME ---
        Collider[] collidersInVision = Physics.OverlapSphere(transform.position, sightRadius, targetLayers);
        foreach (var col in collidersInVision)
        {
            CharacterManager pTarget = col.GetComponent<CharacterManager>();
            if (pTarget != null && pTarget != enemyManager && pTarget.isActiveAndEnabled)
            {
                if (IsTargetInDirectSight(pTarget)) // Line of Sight check
                {
                    freshlyDetectedThisFrame = pTarget;
                    currentFrameDetectionMethod = DetectionMethod.LoS;
                    break; // Prioritize LoS target
                }
            }
        }

        if (freshlyDetectedThisFrame == null) // If no LoS target, check proximity
        {
            Collider[] collidersInProximity = Physics.OverlapSphere(transform.position, proximityDetectionRadius, targetLayers);
            foreach (var col in collidersInProximity)
            {
                CharacterManager pTarget = col.GetComponent<CharacterManager>();
                if (pTarget != null && pTarget != enemyManager && pTarget.isActiveAndEnabled)
                {
                    PlayerManager player = pTarget as PlayerManager;
                    // Proximity only detects non-crouching players, or any non-player CharacterManager
                    if (player == null || (player != null && !player.playerAnimator.IsCrouching))
                    {
                        freshlyDetectedThisFrame = pTarget;
                        currentFrameDetectionMethod = DetectionMethod.Proximity;
                        break;
                    }
                }
            }
        }

        // --- Step 2: Update Target State based on Detection ---
        if (freshlyDetectedThisFrame != null)
        {
            if (CurrentPerceivedTarget == null)
            {
                SetNewTarget(freshlyDetectedThisFrame, currentFrameDetectionMethod);
            }
            else if (CurrentPerceivedTarget == freshlyDetectedThisFrame)
            {
                currentLoseTargetTimer = 0f;
                lastFrameDetectionMethod = currentFrameDetectionMethod;
            }
            else // Switched target (e.g., player ran past another valid target closer by LoS)
            {
                LoseCurrentTargetAndNotify();
                SetNewTarget(freshlyDetectedThisFrame, currentFrameDetectionMethod); // Spot new one
            }
        }
        else
        {
            if (CurrentPerceivedTarget != null) // We *were* tracking someone
            {
                currentLoseTargetTimer += Time.deltaTime;
                float maxTimeToLose = (lastFrameDetectionMethod == DetectionMethod.LoS) ? timeToLoseTargetAfterLoSBreak : timeToLoseTargetAfterProximityBreak;
                if (lastFrameDetectionMethod == DetectionMethod.None) maxTimeToLose = 0.1f;

                // Instant loss if they get too far, regardless of timer
                float distanceToLastKnown = Vector3.Distance(transform.position, CurrentPerceivedTarget.transform.position);
                if (distanceToLastKnown > sightRadius * loseSightDistanceMultiplier)
                {
                    Debug.Log($"Target {CurrentPerceivedTarget.name} lost due to excessive distance.");
                    LoseCurrentTargetAndNotify();
                }
                else if (currentLoseTargetTimer >= maxTimeToLose)
                {
                    Debug.Log($"Target {CurrentPerceivedTarget.name} lost due to timer ({currentLoseTargetTimer} >= {maxTimeToLose}), last seen via {lastFrameDetectionMethod}.");
                    LoseCurrentTargetAndNotify();
                }
                // else, still in grace period, keep CurrentPerceivedTarget for now
            }
            // If CurrentPerceivedTarget is already null and we didn't detect anyone, do nothing.
        }
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
        lastFrameDetectionMethod = method; // Store how we initially saw them
        currentLoseTargetTimer = 0f;       // Reset loss timer
        OnTargetSpotted?.Invoke(target);
        Debug.Log($"{enemyManager.name} spotted target: {target.name} via {method}");
    }

    private void LoseCurrentTargetAndNotify()
    {
        if (CurrentPerceivedTarget != null)
        {
            Debug.Log($"{enemyManager.name} lost target: {CurrentPerceivedTarget.name} (was {lastFrameDetectionMethod})");
        }
        CurrentPerceivedTarget = null;
        lastFrameDetectionMethod = DetectionMethod.None;
        currentLoseTargetTimer = 0f;
        OnTargetLost?.Invoke(); // Notify manager and other systems
    }

    public void ForceLoseTarget() => LoseCurrentTargetAndNotify();

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
            Gizmos.color = (lastFrameDetectionMethod == DetectionMethod.LoS) ? Color.green : ((lastFrameDetectionMethod == DetectionMethod.Proximity) ? Color.magenta : Color.red);
            Vector3 sightOrigin = transform.position + Vector3.up * (enemyManager.GetComponent<UnityEngine.AI.NavMeshAgent>()?.height * 0.8f ?? 1.5f);
            Gizmos.DrawLine(sightOrigin, CurrentPerceivedTarget.lockOnTransform.position);
        }
    }
}
