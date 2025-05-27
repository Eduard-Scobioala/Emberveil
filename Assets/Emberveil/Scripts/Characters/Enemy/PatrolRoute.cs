using System.Collections.Generic;
using UnityEngine;

public class PatrolRoute : MonoBehaviour
{
    public List<Transform> patrolPoints = new List<Transform>();
    public bool loop = true;
    public float patrolWaitTime = 3f;

    void OnDrawGizmos() // Visualize the route
    {
        if (patrolPoints == null || patrolPoints.Count < 2) return;

        Gizmos.color = Color.cyan;
        for (int i = 0; i < patrolPoints.Count; i++)
        {
            if (patrolPoints[i] == null) continue;
            Gizmos.DrawSphere(patrolPoints[i].position, 0.3f);
            if (i < patrolPoints.Count - 1)
            {
                if (patrolPoints[i + 1] != null) Gizmos.DrawLine(patrolPoints[i].position, patrolPoints[i + 1].position);
            }
            else if (loop && patrolPoints[0] != null) // Loop back to start
            {
                Gizmos.DrawLine(patrolPoints[i].position, patrolPoints[0].position);
            }
        }
    }
}
