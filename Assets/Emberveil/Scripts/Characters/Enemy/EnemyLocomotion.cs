using UnityEngine;
using UnityEngine.AI;

public class EnemyLocomotion : MonoBehaviour
{
    [SerializeField] private LayerMask detectionLayer;
    [SerializeField] private LayerMask occlusionLayer;

    private EnemyManager enemyManager;
    private EnemyAnimator enemyAnimator;
    private NavMeshAgent navMeshAgent;

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
        navMeshAgent = GetComponentInChildren<NavMeshAgent>();
        enemyRigidbody = GetComponent<Rigidbody>();
    }

    private void Start()
    {
        DisableNavMeshAgent();
        enemyRigidbody.isKinematic = false;
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

        Vector3 targetPoint = currentTarget.transform.position + Vector3.up * 1.5f;
        Vector3 originPoint = transform.position + Vector3.up * 1.5f;

        if (Physics.Linecast(originPoint, targetPoint, out RaycastHit hit, occlusionLayer))
        {
            // If hit something, check if it WASN'T the target
            if (hit.transform != currentTarget.transform && !hit.transform.IsChildOf(currentTarget.transform))
            {
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
            if (navMeshAgent.enabled) navMeshAgent.ResetPath(); // Stop moving if target lost
            enemyAnimator.anim.SetFloat("Vertical", 0, 0.1f, Time.deltaTime);
            return;
        }

        distanceFromTarget = Vector3.Distance(currentTarget.transform.position, transform.position);

        // If we are performing an action, stop the movement
        if (enemyManager.isPerformingAction)
        {
            enemyAnimator.anim.SetFloat("Vertical", 0, 0.1f, Time.deltaTime);
            DisableNavMeshAgent();
        }
        else
        {
            if (distanceFromTarget > stoppingDistance)
            {
                enemyAnimator.anim.SetFloat("Vertical", 1, 0.1f, Time.deltaTime);
                EnableNavMeshAgent(); // Make sure agent is enabled for chasing

                if (navMeshAgent.enabled)
                {
                    navMeshAgent.SetDestination(currentTarget.transform.position);
                }
            }
            else if (distanceFromTarget <= stoppingDistance)
            {
                enemyAnimator.anim.SetFloat("Vertical", 0, 0.1f, Time.deltaTime);
                if (navMeshAgent.enabled) navMeshAgent.ResetPath();
            }
        }

        HandleRotateTowardsTarget();

        // Ensure NavMeshAgent stays at root position
        //if (navMeshAgent.enabled)
        //{
        //    navMeshAgent.transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
        //}
    }

    public void HandleRotateTowardsTarget()
    {
        if (currentTarget == null) return;

        HandleRotateTowardsPosition(currentTarget.transform.position);
    }

    public void HandleRotateTowardsPosition(Vector3 targetPosition)
    {
        float deltaTime = Time.deltaTime;

        // Calculate the direction to the target position
        Vector3 direction = targetPosition - transform.position;
        direction.y = 0; // Keep rotation horizontal (on the XZ plane)
        direction.Normalize();

        // If the direction is effectively zero (we are already at the target), use the current forward direction
        if (direction == Vector3.zero)
        {
            direction = transform.forward;
        }

        // Calculate the target rotation looking towards the direction
        Quaternion targetRotation = Quaternion.LookRotation(direction);

        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * deltaTime);
    }

    public void EnableNavMeshAgent()
    {
        if (!navMeshAgent.enabled)
        {
            navMeshAgent.enabled = true;
        }
    }

    public void DisableNavMeshAgent()
    {
        if (navMeshAgent.enabled)
        {
            navMeshAgent.enabled = false;
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