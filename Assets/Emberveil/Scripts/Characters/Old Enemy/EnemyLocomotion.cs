using UnityEngine;
using UnityEngine.AI;

public class EnemyLocomotion2 : MonoBehaviour
{
    [SerializeField] private LayerMask detectionLayer;
    [SerializeField] private LayerMask occlusionLayer;

    private EnemyManager enemyManager;
    private EnemyAnimator enemyAnimator;
    public NavMeshAgent navMeshAgent;

    [Header("Locomotion Settings")]
    public CharacterStats currentTarget;
    public Rigidbody enemyRigidbody;
    public float distanceFromTarget;
    public float stoppingDistance = 1f;
    public float rotationSpeed = 15f;

    [Header("Target Loss")]
    public float maxChaseDistance = 30f;

    private void Awake()
    {
        enemyManager = GetComponent<EnemyManager>();
        enemyAnimator = GetComponentInChildren<EnemyAnimator>();
        navMeshAgent = GetComponent<NavMeshAgent>();
        enemyRigidbody = GetComponent<Rigidbody>();

        if (navMeshAgent == null)
        {
            Debug.LogError("NavMeshAgent component not found on " + gameObject.name + " or its children!", this);
        }
        
        if (enemyRigidbody == null)
        {
            Debug.LogError("Rigidbody component not found on " + gameObject.name + "!", this);
        }
    }

    private void Start()
    {
        // Start with Rigidbody non-kinematic and Agent disabled (idle state)
        if (enemyRigidbody != null) enemyRigidbody.isKinematic = false;
        DisableNavMeshAgent();
    }

    public void FindAndVerifyTarget()
    {
        if (currentTarget != null)
        {
            distanceFromTarget = Vector3.Distance(currentTarget.transform.position, transform.position);
            if (distanceFromTarget > maxChaseDistance)
            {
                Debug.Log($"{enemyManager.gameObject.name} lost target: Too far ({distanceFromTarget}m > {maxChaseDistance}m)");
                currentTarget = null;
                return; // Lost target
            }

            // Check Line of Sight (LoS)
            Vector3 targetDirection = currentTarget.transform.position - transform.position;
            float viewableAngle = Vector3.Angle(targetDirection, transform.forward);

            // Check if within view cone OR if already chasing (allow wider angle if already engaged)
            bool isInViewCone = viewableAngle > enemyManager.minDetectionAngle && viewableAngle < enemyManager.maxDetectionAngle;
            bool currentlyChasing = enemyManager.GetCurrentState() is ChaseState || enemyManager.GetCurrentState() is CombatState;

            if (isInViewCone || currentlyChasing)
            {
                // Target's approximate eye level
                Vector3 targetPoint = currentTarget.transform.position + Vector3.up * 1.5f;
                Vector3 originPoint = transform.position + Vector3.up * 1.5f;

                if (Physics.Linecast(originPoint, targetPoint, out RaycastHit hit, occlusionLayer))
                {
                    // Something is blocking LoS
                    if (hit.transform != currentTarget.transform && !hit.transform.IsChildOf(currentTarget.transform)) // Ensure it's not hitting the target itself
                    {
                        Debug.Log($"{enemyManager.gameObject.name} lost target: Line of Sight blocked by {hit.collider.name}");
                        // Don't immediately lose target on LoS break, let the ChaseState timer handle it
                        // currentTarget = null;
                        // return;
                    }
                    // else: LoS is clear or hit the target itself
                }
                // else: Linecast didn't hit anything obstructing, LoS is clear
            }
            else if (!currentlyChasing) // If not in view cone AND not already chasing, lose target
            {
                Debug.Log($"{enemyManager.gameObject.name} lost target: Outside view cone and not chasing.");
                currentTarget = null;
                return;
            }

            // If we reach here, the current target is still valid
            return; // Keep existing target
        }

        // Finding a New Target (Only if currentTarget is null) ---
        if (currentTarget == null)
        {
            Collider[] colliders = Physics.OverlapSphere(transform.position, enemyManager.detectionRadius, detectionLayer);

            for (int i = 0; i < colliders.Length; i++)
            {
                CharacterStats characterStats = colliders[i].transform.GetComponent<CharacterStats>();

                if (characterStats != null && characterStats != enemyManager.enemyStats) // Ensure not targeting self
                {
                    // Check for Team ID here if needed

                    Vector3 targetDirection = characterStats.transform.position - transform.position;
                    float viewableAngle = Vector3.Angle(targetDirection, transform.forward);

                    if (viewableAngle > enemyManager.minDetectionAngle && viewableAngle < enemyManager.maxDetectionAngle)
                    {
                        // Check LoS before acquiring new target
                        Vector3 targetPoint = characterStats.transform.position + Vector3.up * 1.5f;
                        Vector3 originPoint = transform.position + Vector3.up * 1.5f;
                        if (!Physics.Linecast(originPoint, targetPoint, occlusionLayer))
                        {
                            Debug.Log($"{enemyManager.gameObject.name} detected NEW target: {characterStats.name}");
                            currentTarget = characterStats;
                            return; // Found a valid new target
                        }
                        // else: New target detected but LoS blocked
                    }
                }
            }
        }

        // If no target found or verified, currentTarget remains null or unchanged
    }

    public bool HasLineOfSightToTarget()
    {
        if (currentTarget == null) return false;

        // Use a point slightly above the enemy's base for the origin
        Vector3 originPoint = transform.position + Vector3.up * navMeshAgent.height * 0.8f; // e.g., 80% of agent height
        // Use a point around the center of the target
        Vector3 targetCenterPoint = currentTarget.transform.position + Vector3.up * 1.0f; // Adjust based on target size/pivot

        // Debug Draw Line
        // Debug.DrawLine(originPoint, targetCenterPoint, Color.yellow);

        if (Physics.Linecast(originPoint, targetCenterPoint, out RaycastHit hit, occlusionLayer))
        {
            // If hit something, check if it WASN'T the target or a child of the target
            if (hit.transform != currentTarget.transform && !hit.transform.IsChildOf(currentTarget.transform))
            {
                // Debug.Log($"LoS blocked by {hit.collider.name}");
                return false; // LoS blocked
            }
        }
        // If no hit or hit the target, LoS is clear
        return true;
    }

    public void HandleMoveToTarget()
    {
        if (currentTarget == null || enemyManager.isPerformingAction)
        {
            // If we stop moving, disable the agent and make RB non-kinematic
            if (IsAgentEnabled())
            {
                DisableNavMeshAgent();
            }
            enemyAnimator.anim.SetFloat("Vertical", 0, 0.1f, Time.deltaTime);
            return;
        }

        distanceFromTarget = Vector3.Distance(currentTarget.transform.position, transform.position);

        // Stopping distance for NavMeshAgent should be slightly LARGER than
        // the distance check here to prevent jittering at the destination.
        // Or handle stopping purely based on agent's remainingDistance.
        // Let's use the agent's stopping distance primarily.

        // Make sure agent is enabled for chasing
        EnableNavMeshAgent(); // This now handles setting Rigidbody kinematic

        if (navMeshAgent.enabled && navMeshAgent.isOnNavMesh)
        {
            navMeshAgent.SetDestination(currentTarget.transform.position);

            // Check if agent has reached the destination (or very close)
            // Use remainingDistance which is more reliable than manual distance check sometimes
            if (navMeshAgent.remainingDistance <= navMeshAgent.stoppingDistance) // Use agent's stopping distance
            {
                // Reached destination (or close enough based on agent settings)
                enemyAnimator.anim.SetFloat("Vertical", 0, 0.1f, Time.deltaTime);
                // Don't disable agent here yet, CombatState might need rotation or fine positioning
                // CombatState entry will handle disabling if needed.
                // Stop the agent from moving further by setting velocity to 0 maybe? Or ResetPath?
                // Let's rely on the state transition to Combat to handle full stop.
                navMeshAgent.velocity = Vector3.zero; // Try stopping velocity explicitly
            }
            else
            {
                // Still moving
                enemyAnimator.anim.SetFloat("Vertical", navMeshAgent.desiredVelocity.magnitude > 0.1f ? 1 : 0, 0.1f, Time.deltaTime); // Use agent velocity for anim
            }
        }

        // --- Rotation Handling ---
        HandleRotateTowardsTarget();
    }

    public void HandleRotateTowardsTarget()
    {
        if (currentTarget == null) return;
        // Only rotate manually if the agent ISN'T handling rotation
        if (navMeshAgent != null && !navMeshAgent.updateRotation)
        {
            HandleRotateTowardsPosition(currentTarget.transform.position);
        }
    }

    public void HandleRotateTowardsPosition(Vector3 targetPosition)
    {
        if (enemyManager.isBeingCriticallyHit) 
            return;

        // Only rotate manually if the agent ISN'T handling rotation OR if the agent is disabled
        if (navMeshAgent != null && (!navMeshAgent.updateRotation || !navMeshAgent.enabled))
        {
            float deltaTime = Time.deltaTime;
            Vector3 direction = targetPosition - transform.position;
            direction.y = 0;
            direction.Normalize();

            if (direction == Vector3.zero) return; // Already facing or at the point

            Quaternion targetRotation = Quaternion.LookRotation(direction);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * deltaTime);
        }
    }

    public void EnableNavMeshAgent()
    {
        if (navMeshAgent != null && !navMeshAgent.enabled)
        {
            navMeshAgent.enabled = true;
            navMeshAgent.updateRotation = true;
            if (enemyRigidbody != null)
            {
                // Make Rigidbody kinematic when Agent is active
                enemyRigidbody.isKinematic = true;
            }
        }
    }

    public void DisableNavMeshAgent()
    {
        if (navMeshAgent != null && navMeshAgent.enabled)
        {
            // Reset path before disabling to stop residual movement/velocity
            if (navMeshAgent.isOnNavMesh)
            {
                navMeshAgent.ResetPath();
                // Optional: Explicitly stop agent velocity if needed
                // navMeshAgent.velocity = Vector3.zero;
            }
            navMeshAgent.enabled = false;
            navMeshAgent.updateRotation = false;
            if (enemyRigidbody != null)
            {
                // Make Rigidbody non-kinematic when Agent is inactive
                // Allows physics interaction (like falling, being pushed if needed)
                enemyRigidbody.isKinematic = false;
            }
        }
        // Also ensure rigidbody is non-kinematic if agent was already disabled
        else if (enemyRigidbody != null && enemyRigidbody.isKinematic)
        {
            enemyRigidbody.isKinematic = false;
        }
    }

    public bool IsAgentEnabled()
    {
        return navMeshAgent != null && navMeshAgent.enabled;
    }

    public void SetAgentDestination(Vector3 destination)
    {
        if (navMeshAgent != null && navMeshAgent.enabled && navMeshAgent.isOnNavMesh)
        {
            navMeshAgent.SetDestination(destination);
        }
        else if (navMeshAgent != null && !navMeshAgent.enabled)
        {
            Debug.LogWarning("SetAgentDestination called while NavMeshAgent is disabled.", this);
        }
        else if (navMeshAgent != null && !navMeshAgent.isOnNavMesh)
        {
            Debug.LogWarning("SetAgentDestination called while NavMeshAgent is not on a NavMesh.", this);
        }
    }
}