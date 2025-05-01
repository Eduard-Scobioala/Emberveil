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
        navMeshAgent.enabled = false;
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
                }
            }
        }
    }

    public void HandleMoveToTarget()
    {
        if (enemyManager.isPerformingAction)
            return;

        //Vector3 targetDirection = currentTarget.transform.position - transform.position;
        //float viewableAngle = Vector3.Angle(targetDirection, transform.forward);
        distanceFromTarget = Vector3.Distance(currentTarget.transform.position, transform.position);

        // If we are performing an action, stop the movement
        if (enemyManager.isPerformingAction)
        {
            enemyAnimator.anim.SetFloat("Vertical", 0, 0.1f, Time.deltaTime);
            navMeshAgent.enabled = false;
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

        navMeshAgent.transform.localPosition = Vector3.zero;
        navMeshAgent.transform.rotation = Quaternion.identity;
    }

    public void HandleRotateTowardsTarget()
    {
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
        else
        {
            // Ensure NavMeshAgent is enabled and set destination
            if (!navMeshAgent.enabled)
            {
                navMeshAgent.enabled = true;
            }
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
}