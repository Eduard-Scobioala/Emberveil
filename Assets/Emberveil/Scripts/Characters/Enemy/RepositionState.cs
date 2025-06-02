using UnityEngine;
using UnityEngine.AI;

public class RepositionState : IEnemyState
{
    private EnemyManager manager;
    private NavMeshAgent agent;

    private float repositionTimer;
    private Vector3 chosenRepositionDestination;
    private bool isStrafing;

    // --- State Configuration (Consider moving to EnemyManager or a ScriptableObject for per-enemy tuning) ---
    private const float MinRepositionDuration = 1.5f;
    private const float MaxRepositionDuration = 3.0f;
    private const float StrafeDistance = 2.0f; // How far to strafe sideways
    private const float IdealMinDistanceFromTarget = 2f; // Try to stay at least this far
    private const float IdealMaxDistanceFromTarget = 4f; // Try to stay within this distance
    private const float RepositionSpeedMultiplier = 0.8f; // Speed during reposition vs base speed
    private const float FacePlayerRotationSpeed = 10f;

    public void Enter(EnemyManager manager)
    {
        this.manager = manager;
        agent = manager.Locomotion.GetComponent<NavMeshAgent>();

        Debug.Log($"{manager.name} entered RepositionState.");

        manager.Locomotion.EnableAgentNavigation();
        agent.speed = manager.Locomotion.baseSpeed * RepositionSpeedMultiplier;
        agent.stoppingDistance = 0.5f; // Allow getting closer to the chosen reposition point
        agent.updateRotation = true; // Let agent handle rotation towards path initially, but we might override to face player

        repositionTimer = Random.Range(MinRepositionDuration, MaxRepositionDuration);
        ChooseAndSetRepositionManeuver();
    }

    private void ChooseAndSetRepositionManeuver()
    {
        if (manager.CurrentTarget == null)
        {
            // Should not happen if called correctly, but as a safeguard
            chosenRepositionDestination = manager.transform.position;
            isStrafing = false;
            manager.Locomotion.StopMovement();
            repositionTimer = 0.1f;
            return;
        }

        Vector3 enemyPos = manager.transform.position;
        Vector3 targetPos = manager.CurrentTarget.transform.position;
        float distanceToTarget = Vector3.Distance(enemyPos, targetPos);
        Vector3 directionToTarget = (targetPos - enemyPos).normalized;
        Vector3 directionAwayFromTarget = -directionToTarget;

        isStrafing = false; // Default

        // Strategy:
        // 1. If too close, try to back away.
        // 2. If too far (but still in reasonable combat range), try to move diagonally closer.
        // 3. Otherwise, strafe left or right.

        if (distanceToTarget < IdealMinDistanceFromTarget)
        {
            // Try to move backward or diagonally backward
            Vector3 perpendicular = Vector3.Cross(Vector3.up, directionToTarget).normalized;
            chosenRepositionDestination = enemyPos + directionAwayFromTarget * StrafeDistance * 0.75f +
                                         (Random.value > 0.5f ? perpendicular : -perpendicular) * StrafeDistance * 0.5f;
            Debug.Log($"{manager.name} reposition: Too close, moving back/diagonally.");
        }
        else if (distanceToTarget > IdealMaxDistanceFromTarget && distanceToTarget < manager.Senses.sightRadius * 0.9f)
        {
            // Try to move diagonally forward
            Vector3 perpendicular = Vector3.Cross(Vector3.up, directionToTarget).normalized;
            chosenRepositionDestination = enemyPos + directionToTarget * StrafeDistance * 0.5f +
                                         (Random.value > 0.5f ? perpendicular : -perpendicular) * StrafeDistance * 0.75f;
            Debug.Log($"{manager.name} reposition: Bit far, moving diagonally closer.");
        }
        else
        {
            // Strafe: Pick a point to the side of the target, maintaining similar distance
            isStrafing = true;
            Vector3 perpendicular = Vector3.Cross(Vector3.up, directionToTarget).normalized;
            Vector3 strafeDir = Random.value > 0.5f ? perpendicular : -perpendicular;

            // Project current position onto line perpendicular to target, then move along it
            Vector3 pointOnStrafeLine = enemyPos + strafeDir * StrafeDistance;
            chosenRepositionDestination = pointOnStrafeLine;
            Debug.Log($"{manager.name} reposition: Strafing.");
        }

        // Validate destination on NavMesh
        if (NavMesh.SamplePosition(chosenRepositionDestination, out NavMeshHit hit, 2.0f, NavMesh.AllAreas))
        {
            chosenRepositionDestination = hit.position;
            manager.Locomotion.MoveToPoint(chosenRepositionDestination);
        }
        else
        {
            // Fallback: could not find valid point, maybe just stay put or try simpler strafe
            Debug.LogWarning($"{manager.name} reposition: Could not find valid NavMesh point for maneuver. Staying put for now.");
            manager.Locomotion.StopMovement();
            repositionTimer = 0.2f; // Quickly exit state if stuck
        }
    }

    public void Tick()
    {
        manager.Senses.TickSenses();
        repositionTimer -= Time.deltaTime;

        if (manager.CurrentTarget != null)
        {
            // --- DYNAMIC ROTATION CONTROL ---
            bool shouldManuallyFacePlayer = false;

            if (agent.enabled && agent.isOnNavMesh)
            {
                // If we are performing a strafing maneuver OR if we are very close to our NavMesh destination,
                // prioritize facing the player.
                if (isStrafing || (!agent.pathPending && agent.remainingDistance <= agent.stoppingDistance + 0.2f))
                {
                    shouldManuallyFacePlayer = true;
                }
            }
            else
            {
                // If agent is somehow disabled but we have a target, try to face them.
                shouldManuallyFacePlayer = true;
            }


            if (shouldManuallyFacePlayer)
            {
                if (agent.enabled) agent.updateRotation = false; // Disable agent's auto-rotation
                manager.Locomotion.RotateTowards(manager.CurrentTarget.transform.position, FacePlayerRotationSpeed);
            }
            else
            {
                if (agent.enabled) agent.updateRotation = true; // Let agent control rotation towards its path
            }
        }
        else // No target
        {
            if (agent.enabled) agent.updateRotation = true; // Let agent control rotation if no target (e.g., returning to post)
        }
    }

    public void FixedTick()
    {
        // Movement is handled by NavMeshAgent.SetDestination in ChooseAndSetRepositionManeuver
        // and updated by EnemyLocomotion.UpdateAnimatorMovementParameters() via EnemyManager.Update()
    }

    public IEnemyState Transition()
    {
        if (manager.CurrentTarget == null)
        {
            return manager.returnToPostState;
        }

        if (!manager.Combat.IsAttackOnCooldown)
        {
            // Debug.Log($"{manager.name} RepositionState: Attack cooldown ended. To CombatStance.");
            return manager.combatStanceState;
        }

        // Check if maneuver is complete (timer or reached destination)
        bool maneuverComplete = repositionTimer <= 0;
        if (agent.enabled && agent.isOnNavMesh && !agent.pathPending)
        {
            if (agent.remainingDistance <= agent.stoppingDistance)
            {
                maneuverComplete = true;
            }
        }
        else if (!agent.enabled || !agent.isOnNavMesh)
        { // If agent gets disabled or off mesh, consider maneuver "done" to avoid getting stuck
            maneuverComplete = true;
        }


        if (maneuverComplete)
        {
            // Debug.Log($"{manager.name} RepositionState: Maneuver/timer complete. To CombatStance (still on cooldown).");
            return manager.combatStanceState;
        }

        float distanceToTarget = Vector3.Distance(manager.transform.position, manager.CurrentTarget.transform.position);
        if (distanceToTarget > manager.Senses.sightRadius * manager.Senses.loseSightDistanceMultiplier * 0.9f)
        {
            // Debug.Log($"{manager.name} RepositionState: Target too far. To ChaseState.");
            return manager.chaseState;
        }

        return null;
    }

    public void Exit()
    {
        // Restore default agent settings
        if (agent.enabled && agent.isOnNavMesh)
        {
            agent.stoppingDistance = manager.defaultStoppingDistance;
            agent.updateRotation = true;
        }
        // manager.Locomotion.StopMovement(); // Let the next state decide to stop or continue
        Debug.Log($"{manager.name} exited RepositionState.");
    }
}
