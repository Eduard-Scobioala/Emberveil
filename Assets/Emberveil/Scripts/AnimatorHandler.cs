using UnityEngine;

public class AnimatorHandler : MonoBehaviour
{
    private int vertical;
    private int horizontal;

    private Animator anim;
    private InputHandler inputHandler;
    private PlayerLocomotion playerLocomotion;
    private PlayerManager playerManager;
    public bool canRotate;

    public void Initialize()
    {
        anim = GetComponent<Animator>();
        inputHandler = GetComponentInParent<InputHandler>();
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

    public void PlayTargetAnimation(string targetAnim, bool isInteracting)
    {
        anim.applyRootMotion = isInteracting;
        anim.SetBool("isInteracting", isInteracting);
        anim.CrossFade(targetAnim, 0.1f);
    }

    public void EnableRotation() => canRotate = true;

    public void DisableRotation() => canRotate = false;

    public void OnAnimatorMove()
    {
        if (playerManager.isInteracting == false)
            return;

        float deltaTime = Time.deltaTime;

        playerLocomotion.rigidbody.drag = 0;
        Vector3 deltaPosition = anim.deltaPosition;
        deltaPosition.y = 0;
        Vector3 velocity = deltaPosition / deltaTime;
        playerLocomotion.rigidbody.velocity = velocity;
    }

    public bool IsInteracting() => anim.GetBool("isInteracting");
}
