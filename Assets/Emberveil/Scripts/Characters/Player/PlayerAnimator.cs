using UnityEngine;

public class PlayerAnimator : AnimatorManager
{
    public readonly int hashVertical = Animator.StringToHash("Vertical");
    public readonly int hashHorizontal = Animator.StringToHash("Horizontal");
    private readonly int hashIsCrouching = Animator.StringToHash("isCrouching");
    private readonly int hashIsDodging = Animator.StringToHash("isDodging");
    private readonly int hashIsGrounded = Animator.StringToHash("isGrounded");
    private readonly int hashIsInAir = Animator.StringToHash("isInAir");

    private PlayerLocomotion playerLocomotion;
    private PlayerManager playerManager;
    private CameraController cameraController;

    public bool canRotate;

    public int RollDirection
    {
        get => GetAnimatorInt();
        set => SetAnimatorInt(value);
    }

    public bool IsTwoHanding
    {
        get => GetAnimatorBool();
        set => SetAnimatorBool(value);
    }

    public void Initialize()
    {
        anim = GetComponent<Animator>();
        playerLocomotion = GetComponentInParent<PlayerLocomotion>();
        playerManager = GetComponentInParent<PlayerManager>();
        cameraController = FindObjectOfType<CameraController>();

        if (playerManager == null) Debug.LogError("PlayerManager not found in parent for AnimatorHandler", this);
        if (playerLocomotion == null) Debug.LogError("PlayerLocomotion not found in parent for AnimatorHandler", this);
        if (cameraController == null) Debug.LogError("CameraController not found in scene for AnimatorHandler", this);
    }

    public void UpdateAnimatorValues(float verticalInput, float horizontalInput, bool isSprinting, bool isCrouching, bool isLockedOn)
    {
        if (anim == null || playerManager == null || cameraController == null) return;

        float v = 0; // Vertical output for animator
        float h = 0; // Horizontal output for animator

        if (isLockedOn && !isSprinting && !isCrouching) // Locked-on standard movement (not sprinting/crouching)
        {
            // Input is already player-relative (forward/strafe)
            v = verticalInput;
            h = horizontalInput;
        }
        else // Free-look, or Sprinting/Crouching (even if locked on, these might use camera-relative for freedom)
        {
            // Convert camera-relative input to player-local space for the animator
            Vector3 moveDirWorld = (cameraController.transform.forward * verticalInput) + (cameraController.transform.right * horizontalInput);
            moveDirWorld.y = 0;
            moveDirWorld.Normalize();

            if (moveDirWorld.sqrMagnitude > 0.01f)
            {
                Vector3 moveDirLocal = playerManager.transform.InverseTransformDirection(moveDirWorld);
                v = moveDirLocal.z;
                h = moveDirLocal.x;
            }
            else // No input
            {
                v = 0;
                h = 0;
            }
        }

        // Apply sprinting multiplier if applicable
        if (isSprinting && v > 0.5f && !isCrouching)
        {
            v = 2f;
        }

        // Crouching state is handled by a separate animator bool usually,
        // but v and h still determine crouch walk/strafe direction within the crouch blend tree/layer.
        // No specific change to v, h needed here just for crouching itself,
        // unless your crouch animations are at different 'speed' values in the blend tree (e.g., crouch walk at v=0.25)
        // If so, you'd scale v and h here when isCrouching is true.
        // For example: if (isCrouching) { v *= 0.5f; h *= 0.5f; } // If crouch anims are at half magnitude

        anim.SetFloat(hashVertical, v, 0.1f, Time.deltaTime);
        anim.SetFloat(hashHorizontal, h, 0.1f, Time.deltaTime);
        anim.SetBool(hashIsCrouching, isCrouching);
        anim.SetBool(hashIsGrounded, playerManager.isGrounded);
        anim.SetBool(hashIsInAir, playerManager.isInAir);
    }

    public void PlayTargetAnimation(string targetAnim, bool isPlayerInAction, float crossFadeDuration = 0.1f, bool? rootMotion = null)
    {
        if (anim == null) return;
        IsInMidAction = isPlayerInAction;
        anim.applyRootMotion = rootMotion ?? isPlayerInAction; // General rule, can be overridden by specific anims
        anim.CrossFade(targetAnim, crossFadeDuration);
    }

    public void EnableRotation() => canRotate = true;

    public void DisableRotation() => canRotate = false;

    public void OnAnimatorMove()
    {
        // Only apply root motion if playerManager says they are in an action that *should* use it.
        // And if not in air where root motion can be problematic unless specifically designed for it.
        if (playerManager != null && IsInMidAction && playerManager.isGrounded)
        {
            if (anim != null && anim.applyRootMotion)
            {
                float deltaTime = Time.deltaTime;
                if (deltaTime > 0)
                {
                    playerLocomotion.rigidbody.drag = 0;
                    Vector3 deltaPosition = anim.deltaPosition;
                    deltaPosition.y = 0; // Comment this out IF the animation (like a hop in an attack) has intentional Y movement
                    Vector3 velocity = deltaPosition / deltaTime;
                    playerLocomotion.rigidbody.velocity = new Vector3(velocity.x, playerLocomotion.rigidbody.velocity.y, velocity.z);
                    playerLocomotion.transform.rotation *= anim.deltaRotation;
                }
            }
        }
    }

    public void SetBool(string paramName, bool value)
    {
        if (anim != null) anim.SetBool(paramName, value);
    }

    public void EnableCombo() => anim.SetBool("canDoCombo", true);
    public void DisableCombo() => anim.SetBool("canDoCombo", false);

    public void EnableInvulnerability() => anim.SetBool("isInvulnerable", true);
    public void DisableInvulnerability() => anim.SetBool("isInvulnerable", false);

    public void OnDodgeAnimationEnd() => playerLocomotion.OnDodgeAnimationEnd();

    public void AnimEvent_FinishAction() => IsInMidAction = false;

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

    public void AnimEvent_ApplyJumpForce()
    {
        if (playerLocomotion != null)
        {
            playerLocomotion.TriggerApplyJumpForce();
        }
    }

    public void AnimEvent_FinishJumpAction() // Called from Jump_End animation
    {
        if (playerLocomotion != null)
        {
            playerLocomotion.FinishJumpAction();
        }
    }
}
