using UnityEngine;
using UnityEngine.AI;

public class EnemyLocomotion : MonoBehaviour
{
    [SerializeField] private LayerMask detectionLayer;
    private EnemyManager enemyManager;
    private EnemyAnimator enemyAnimator;
    private NavMeshAgent navMeshAgent;

    [Header("Locomotion Settings")]
    public CharacterStats currentTarget;
    public Rigidbody enemyRigidbody;
    public float distanceFromTarget;
    public float stoppingDistance = 1f;
    public float rotationSpeed = 15f;

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

    public void HandleDetection()
    {
        Collider[] colliders = Physics.OverlapSphere(transform.position, enemyManager.detectionRadius, detectionLayer);

        for (int i = 0; i < colliders.Length; i++)
        {
            CharacterStats characterStats = colliders[i].transform.GetComponent<CharacterStats>();

            if (characterStats != null)
            {
                // Check for Team ID

                Vector3 targetDirection = characterStats.transform.position - transform.position;
                float viewableAngle = Vector3.Angle(targetDirection, transform.forward);

                if (viewableAngle > enemyManager.minDetectionAngle && viewableAngle < enemyManager.maxDetectionAngle)
                {
                    currentTarget = characterStats;
                    return; // Found a valid target, exit early
                }
            }
        }
    }

    public void HandleMoveToTarget()
    {
        if (currentTarget == null || enemyManager.isPerformingAction)
            return;

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
            }
            else if (distanceFromTarget <= stoppingDistance)
            {
                enemyAnimator.anim.SetFloat("Vertical", 0, 0.1f, Time.deltaTime);
            }
        }

        HandleRotateTowardsTarget();

        // Ensure NavMeshAgent stays at root position
        if (navMeshAgent.enabled)
        {
            navMeshAgent.transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
        }
    }

    public void HandleRotateTowardsTarget()
    {
        if (currentTarget == null)
            return;

        float deltaTime = Time.deltaTime;

        // Rotate manually (when performing an action)
        if (enemyManager.isPerformingAction)
        {
            Vector3 direction = currentTarget.transform.position - transform.position;
            direction.y = 0; // Keep rotation in the XZ plane
            direction.Normalize();

            if (direction == Vector3.zero)
            {
                direction = transform.forward; // Avoid zero direction
            }

            Quaternion targetRotation = Quaternion.LookRotation(direction);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * deltaTime);
        }
        // Move and rotate with pathfinding
        else if (navMeshAgent.enabled)
        {
            navMeshAgent.SetDestination(currentTarget.transform.position);

            // Rotate toward the NavMeshAgent's desired velocity
            if (navMeshAgent.desiredVelocity.sqrMagnitude > 0.01f) // Check if there's a valid velocity
            {
                Vector3 direction = navMeshAgent.desiredVelocity.normalized;
                direction.y = 0; // Keep rotation in the XZ plane
                Quaternion targetRotation = Quaternion.LookRotation(direction);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * deltaTime);
            }
        }
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
}