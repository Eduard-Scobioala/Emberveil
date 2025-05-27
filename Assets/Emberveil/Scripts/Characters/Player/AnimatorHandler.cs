using UnityEngine;

public class AnimatorHandler : AnimatorManager
{
    private int vertical;
    private int horizontal;

    private PlayerLocomotion playerLocomotion;
    private PlayerManager playerManager;
    public bool canRotate;

    public void Initialize()
    {
        anim = GetComponent<Animator>();
        playerLocomotion = GetComponentInParent<PlayerLocomotion>();
        playerManager = GetComponentInParent<PlayerManager>();

        vertical = Animator.StringToHash("Vertical");
        horizontal = Animator.StringToHash("Horizontal");
    }

    public void UpdateAnimatorValues(float verticalMovement, float horizontalMovement, bool isSprinting)
    {
        #region Vertical
        float v;

        if (verticalMovement > 0 && verticalMovement <= 0.55f)
        {
            v = 0.5f;
        }
        else if (verticalMovement > 0.55f)
        {
            v = 1f;
        }
        else if (verticalMovement < 0 && verticalMovement >= -0.55f)
        {
            v = -0.5f;
        }
        else if (verticalMovement < -0.55f)
        {
            v = -1f;
        }
        else
        {
            v = 0f;
        }
        #endregion

        #region Horizontal
        float h;

        if (horizontalMovement > 0 && horizontalMovement <= 0.55f)
        {
            h = 0.5f;
        }
        else if (horizontalMovement > 0.55f)
        {
            h = 1f;
        }
        else if (horizontalMovement < 0 && horizontalMovement >= -0.55f)
        {
            h = -0.5f;
        }
        else if (horizontalMovement < -0.55f)
        {
            h = -1f;
        }
        else
        {
            h = 0f;
        }
        #endregion

        if (isSprinting && verticalMovement > 0)
        {
            v = 2;
            h = horizontalMovement;
        }

        anim.SetFloat(vertical, v, 0.1f, Time.deltaTime);
        anim.SetFloat(horizontal, h, 0.1f, Time.deltaTime);
    }

    public void EnableRotation() => canRotate = true;

    public void DisableRotation() => canRotate = false;

    public void OnAnimatorMove()
    {
        if (playerManager.isInMidAction == false)
            return;

        float deltaTime = Time.deltaTime;

        playerLocomotion.rigidbody.drag = 0;
        Vector3 deltaPosition = anim.deltaPosition;
        deltaPosition.y = 0;
        Vector3 velocity = deltaPosition / deltaTime;
        playerLocomotion.rigidbody.velocity = velocity;
    }

    public bool IsInMidAction() => anim.GetBool("isInMidAction");

    public void EnableCombo()
    {
        anim.SetBool("canDoCombo", true);
    }

    public void DisableCombo()
    {
        anim.SetBool("canDoCombo", false);
    }

    public void EnableInvulnerability()
    {
        anim.SetBool("isInvulnerable", true);
    }

    public void DisableInvulnerability()
    {
        anim.SetBool("isInvulnerable", false);
    }

    public void OnDodgeAnimationEnd()
    {
        playerLocomotion.OnDodgeAnimationEnd();
    }

    public void AnimEvent_ApplyBackstabDamage()
    {
        if (playerManager != null)
        {
            playerManager.AnimEvent_ApplyBackstabDamage();
        }
        else
        {
            Debug.LogError("AnimatorHandler: AnimEvent_ApplyBackstabDamage called, but PlayerManager is null!", this);
        }
    }

    public void AnimEvent_FinishPerformingBackstab()
    {
        if (playerManager != null)
        {
            playerManager.AnimEvent_FinishPerformingBackstab();
        }
        else
        {
            Debug.LogError("AnimatorHandler: AnimEvent_FinishPerformingBackstab called, but PlayerManager is null!", this);
        }
    }

    public void AnimEvent_FinishBeingBackstabbed()
    {
        if (playerManager != null)
        {
            playerManager.FinishBeingBackstabbed();
        }
        else
        {
            Debug.LogError("AnimEvent_FinishBeingBackstabbed called, but PlayerManager is null!", this);
        }
    }
}
