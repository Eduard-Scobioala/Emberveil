using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent), typeof(Rigidbody))]
public class EnemyLocomotion : MonoBehaviour
{
    private EnemyManager enemyManager;
    private NavMeshAgent agent;
    private Rigidbody rb;
    private EnemyAnimator enemyAnimator; // Renamed from AnimatorManager

    [Header("Movement Settings")]
    public float baseSpeed = 3f;
    public float chaseSpeed = 3.5f;
    public float rotationSpeed = 10f;
    public float stoppingDistance = 1.5f; // Agent's stopping distance

    public bool IsMoving => agent.enabled && agent.velocity.sqrMagnitude > 0.01f;
    public bool HasReachedDestination => agent.enabled && !agent.pathPending && agent.remainingDistance <= agent.stoppingDistance;


    public void Initialize(EnemyManager manager)
    {
        enemyManager = manager;
        agent = GetComponent<NavMeshAgent>();
        rb = GetComponent<Rigidbody>();
        enemyAnimator = manager.EnemyAnimator; // Get from manager

        agent.stoppingDistance = stoppingDistance;
        agent.speed = baseSpeed;
        DisableAgentAndPhysicsControl(); // Start in a controlled state
    }

    public void EnableAgentNavigation()
    {
        rb.isKinematic = true; // Agent controls movement
        agent.enabled = true;
        agent.updatePosition = true;
        agent.updateRotation = true; // Let agent handle rotation initially
    }

    public void DisableAgentNavigation(bool makeNonKinematic = true)
    {
        if (agent.enabled)
        {
            if (agent.isOnNavMesh) agent.ResetPath();
            agent.velocity = Vector3.zero;
        }
        agent.enabled = false;
        if (makeNonKinematic) rb.isKinematic = false; // Allow physics/root motion
    }

    public void DisableAgentAndPhysicsControl()
    {
        DisableAgentNavigation(false); // Don't make non-kinematic yet
        rb.velocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
        rb.isKinematic = true; // Full animation control or snapping
    }

    public void EnableCharacterControllerPhysics() // For being pushed, ragdoll prep
    {
        DisableAgentNavigation(true); // Make non-kinematic
    }

    public void MoveToPoint(Vector3 destination)
    {
        if (agent.enabled && agent.isOnNavMesh)
        {
            agent.SetDestination(destination);
        }
    }

    public void FollowTarget(Transform target)
    {
        if (agent.enabled && agent.isOnNavMesh && target != null)
        {
            agent.SetDestination(target.position);
        }
    }

    public void StopMovement()
    {
        if (agent.enabled && agent.isOnNavMesh)
        {
            agent.ResetPath();
            agent.velocity = Vector3.zero; // Explicitly stop
        }
        rb.velocity = Vector3.zero; // Stop Rigidbody too if it was moving
    }

    public void RotateTowards(Vector3 targetPosition, float customRotationSpeed = -1f)
    {
        if (enemyManager.IsReceivingCriticalHit || enemyManager.IsPerformingCriticalAction) return; // No manual rotation during criticals

        Vector3 direction = (targetPosition - transform.position).normalized;
        direction.y = 0; // Keep rotation horizontal

        if (direction == Vector3.zero) return;

        Quaternion targetRotation = Quaternion.LookRotation(direction);
        float speed = (customRotationSpeed > 0) ? customRotationSpeed : rotationSpeed;
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, speed * Time.deltaTime);
    }

    public void FaceTargetInstantly(Transform target)
    {
        if (target == null) return;
        Vector3 direction = (target.position - transform.position).normalized;
        direction.y = 0;
        if (direction != Vector3.zero)
        {
            transform.rotation = Quaternion.LookRotation(direction);
        }
    }

    public void ApplyRootMotion(Vector3 deltaPosition, Quaternion deltaRotation)
    {
        // This is called from EnemyAnimator.OnAnimatorMove
        // Assumes RB is non-kinematic and Agent is disabled OR current state allows root motion override
        if (!rb.isKinematic && Time.deltaTime > 0)
        {
            // Position
            // If you want to preserve Y velocity (gravity):
            // Vector3 newVelocity = deltaPosition / Time.deltaTime;
            // newVelocity.y = rb.velocity.y;
            // rb.velocity = newVelocity;
            // OR:
            rb.MovePosition(rb.position + deltaPosition);

            // Rotation
            rb.MoveRotation(rb.rotation * deltaRotation);
        }
        else if (rb.isKinematic) // e.g. during critical hit victim animation
        {
            transform.position += deltaPosition;
            transform.rotation *= deltaRotation;
        }
    }

    public void UpdateAnimatorMovementParameters()
    {
        if (enemyAnimator == null) return;

        float verticalSpeed = 0f;
        float horizontalSpeed = 0f;

        if (agent.enabled && agent.isOnNavMesh && agent.desiredVelocity.sqrMagnitude > 0.01f)
        {
            // Get the desired velocity of the agent (this is what it *wants* to do)
            Vector3 worldDesiredVelocity = agent.desiredVelocity;

            // Transform this world velocity into the enemy's local space
            // This gives us how much it wants to move forward/backward (local Z)
            // and left/right (local X) relative to its current orientation.
            Vector3 localDesiredVelocity = transform.InverseTransformDirection(worldDesiredVelocity);

            // Normalize these local values to be typically between -1 and 1
            // The agent.speed is its current maximum speed.
            verticalSpeed = localDesiredVelocity.z / agent.speed;
            horizontalSpeed = localDesiredVelocity.x / agent.speed;

            // Clamp them to ensure they are within the expected animator range
            // If your blend tree's max for run is 1 and sprint is 2:
            float maxAnimatorSpeed = 1.0f;

            verticalSpeed = Mathf.Clamp(verticalSpeed, -maxAnimatorSpeed, maxAnimatorSpeed);
            horizontalSpeed = Mathf.Clamp(horizontalSpeed, -maxAnimatorSpeed, maxAnimatorSpeed);
        }
        // If the agent is disabled, or not moving, speeds should be 0.
        enemyAnimator.SetMovementValues(verticalSpeed, horizontalSpeed);
    }

    public void SetAgentSpeed(float speed)
    {
        if (agent.enabled) agent.speed = speed;
    }
}
