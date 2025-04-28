using UnityEngine;

public class EnemyAnimator : AnimatorManager
{
    private EnemyLocomotion enemyLocomotion;

    private void Awake()
    {
        anim = GetComponent<Animator>();
        enemyLocomotion = GetComponentInParent<EnemyLocomotion>();
    }

    private void OnAnimatorMove()
    {
        float deltaTime = Time.deltaTime;

        enemyLocomotion.enemyRigidbody.drag = 0;

        Vector3 deltaPosition = anim.deltaPosition;
        deltaPosition.y = 0;

        Vector3 velocity = deltaPosition / deltaTime;
        enemyLocomotion.enemyRigidbody.velocity = velocity;  
    }
}
