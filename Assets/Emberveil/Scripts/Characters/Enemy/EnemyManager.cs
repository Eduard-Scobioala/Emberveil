using UnityEngine;

public class EnemyManager : CharacterManager
{
    [Header("AI Settings")]
    public float detectionRadius = 20;
    public float minDetectionAngle = -50;
    public float maxDetectionAngle = 50;
    public bool isPerformingAction;

    private EnemyLocomotion enemyLocomotion;

    private void Awake()
    {
        enemyLocomotion = GetComponent<EnemyLocomotion>();
    }

    private void Update()
    {
        HandleCurrentAction();
    }

    private void HandleCurrentAction()
    {
        if (enemyLocomotion.currentTarget == null)
        {
            enemyLocomotion.HandleDetection();
        }
        else
        {
            enemyLocomotion.HandleMoveToTarget();
        }
    }

    #region Gizmos
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, detectionRadius);

        Vector3 fovLine1 = Quaternion.AngleAxis(maxDetectionAngle, transform.up) * transform.forward * detectionRadius;
        Vector3 fovLine2 = Quaternion.AngleAxis(minDetectionAngle, transform.up) * transform.forward * detectionRadius;
        Gizmos.color = Color.blue;
        Gizmos.DrawRay(transform.position, fovLine1);
        Gizmos.DrawRay(transform.position, fovLine2);
    }
    #endregion
}
