using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem;

public class CameraHandler : MonoBehaviour
{
    [SerializeField] private Transform playerTransform;
    [SerializeField] private Transform cameraTransform;
    [SerializeField] private Transform cameraPivotTransform;
    [SerializeField] private LayerMask lockOnLayerMask;

    private Vector3 cameraTransformPosition;
    public LayerMask ignoreLayers;
    public LayerMask environmentLayer;

    private Vector3 cameraFollowVelocity = Vector3.zero;

    [SerializeField] private float lookSpeed = 0.1f;
    [SerializeField] private float followSpeed = 0.1f;
    [SerializeField] private float pivotSpeed = 0.03f;

    private float targetPosition;
    private float defaultPosition;
    private float lookAngle;
    private float pivotAngle;
    private float mouseX;
    private float mouseY;

    [SerializeField] private float minimumPivot = -35;
    [SerializeField] private float maximumPivot = 35;
    [SerializeField] private float lockedPivotPosition = 1.9f;
    [SerializeField] private float unlockedPivotPosition = 1.65f;
    [SerializeField] private float cameraMovingSmoothTime = 0.2f;
    private Vector3 cameraMovingVelocity = Vector3.zero;

    [SerializeField] private float cameraSphereRadius = 0.2f;
    [SerializeField] private float cameraCollisionOffset = 0.2f;
    [SerializeField] private float cameraMinimumCollisionOffset = 0.2f;

    [SerializeField] private float maximumLockOnDistance = 30f;
    [SerializeField] private float maximumLockOnAngle = 25f;

    [HideInInspector] public CharacterManager currentLockOnTarget;

    public bool lockOnFlag = false;

    private void Awake()
    {
        defaultPosition = cameraTransform.localPosition.z;
        ignoreLayers = ~(1 << 8 | 1 << 9 | 1 << 10);
    }

    private void LateUpdate()
    {
        float deltaTime = Time.fixedDeltaTime;

        FollowTarget(deltaTime);
        HandleCameraRotation(deltaTime, mouseX, mouseY);
    }

    private void OnEnable()
    {
        InputHandler.CameraMovementPerformed += HandleCameraInput;
        InputHandler.LockOnButtonPressed += HandleLockOnButtonPressed;
        InputHandler.LeftLockOnTargetButtonPressed += HandleLeftLockOnButtonPressed;
        InputHandler.RightLockOnTargetButtonPressed += HandleRightLockOnButtonPressed;
    }

    private void OnDisable()
    {
        InputHandler.CameraMovementPerformed -= HandleCameraInput;
        InputHandler.LockOnButtonPressed -= HandleLockOnButtonPressed;
        InputHandler.LeftLockOnTargetButtonPressed -= HandleLeftLockOnButtonPressed;
        InputHandler.RightLockOnTargetButtonPressed -= HandleRightLockOnButtonPressed;
    }

    private void HandleCameraInput(Vector2 cameraInput)
    {
        mouseX = cameraInput.x;
        mouseY = cameraInput.y;
    }

    public void FollowTarget(float deltaTime)
    {
        Vector3 targetPosition = Vector3.SmoothDamp
            (transform.position, playerTransform.position, ref cameraFollowVelocity, deltaTime / followSpeed);
        transform.position = targetPosition;

        HandleCameraCollisions(deltaTime);
    }

    public void HandleCameraRotation(float deltaTime, float mouseXInput, float mouseYInput)
    {
        if(!lockOnFlag && currentLockOnTarget == null)
        {
            lookAngle += (mouseXInput * lookSpeed) / deltaTime;
            pivotAngle -= (mouseYInput * pivotSpeed) / deltaTime;
            pivotAngle = Mathf.Clamp(pivotAngle, minimumPivot, maximumPivot);

            Vector3 rotation = Vector3.zero;
            rotation.y = lookAngle;
            Quaternion targetRotation = Quaternion.Euler(rotation);
            transform.rotation = targetRotation;

            rotation = Vector3.zero;
            rotation.x = pivotAngle;

            targetRotation = Quaternion.Euler(rotation);
            cameraPivotTransform.localRotation = targetRotation;
        }
        else
        {
            Vector3 cameraDirection = currentLockOnTarget.transform.position - transform.position;
            cameraDirection.Normalize();
            cameraDirection.y = 0;

            Quaternion targetRotation = Quaternion.LookRotation(cameraDirection);
            transform.rotation = targetRotation;

            Vector3 pivotDirection = currentLockOnTarget.transform.position - cameraPivotTransform.position;
            pivotDirection.Normalize();

            targetRotation = Quaternion.LookRotation(pivotDirection);
            Vector3 eulerAngle = targetRotation.eulerAngles;
            eulerAngle.y = 0;
            cameraPivotTransform.localEulerAngles = eulerAngle;
        }

        // In Souls Like when you Lock On the camera moves a bit back to have a bigger view
        UpdateCameraHeight();
    }

    private void HandleCameraCollisions(float deltaTime)
    {
        targetPosition = defaultPosition;

        RaycastHit hit;
        Vector3 direction = cameraTransform.position - cameraPivotTransform.position;
        direction.Normalize();

        if (Physics.SphereCast(cameraPivotTransform.position, cameraSphereRadius, direction, out hit, Mathf.Abs(targetPosition), ignoreLayers))
        {
            float dis = Vector3.Distance(cameraPivotTransform.position, hit.point);
            targetPosition = -(dis - cameraCollisionOffset);
        }

        if (Mathf.Abs(targetPosition) < cameraMinimumCollisionOffset)
        {
            targetPosition = -cameraCollisionOffset;
        }

        cameraTransformPosition.z = Mathf.Lerp(cameraTransform.localPosition.z, targetPosition, deltaTime / 0.2f);
        cameraTransform.localPosition = cameraTransformPosition;
    }

    private void SetLockOnTarget(CharacterManager target)
    {
        if (target != null)
        {
            currentLockOnTarget = target;
            currentLockOnTarget.OnDeath += ClearLockOnTarget;
            lockOnFlag = true;
        }
    }

    private void ClearLockOnTarget()
    {
        currentLockOnTarget.OnDeath -= ClearLockOnTarget;
        currentLockOnTarget = null;
        lockOnFlag = false;
    }

    private void HandleLockOnButtonPressed()
    {
        if (!lockOnFlag)
        {
            SetLockOnTarget(FindNearestLockOnTarget());
        }
        else
        {
            ClearLockOnTarget();
        }
    }

    private void HandleLeftLockOnButtonPressed()
    {
        if (lockOnFlag)
        {
            SetLockOnTarget(FindLeftOfLockOnTarget());
        }
    }

    private void HandleRightLockOnButtonPressed()
    {
        if (lockOnFlag)
        {
            SetLockOnTarget(FindRightOfLockOnTarget());
        }
    }

    private List<CharacterManager> GetAvailableLockOnTargets()
    {
        var availableTargets = new List<CharacterManager>();

        Collider[] colliders = Physics.OverlapSphere(playerTransform.position, maximumLockOnDistance, lockOnLayerMask);

        foreach (var collider in colliders)
        {
            CharacterManager character = collider.GetComponent<CharacterManager>();
            if (character != null && character.lockOnTransform)
            {
                //var lockOnConePosition = cameraTransform.position + cameraTransform.forward;
                float targetAngle = Vector3.Angle(cameraTransform.forward, character.lockOnTransform.position - cameraTransform.position);

                if (targetAngle < maximumLockOnAngle)
                {
                    if (!Physics.Linecast(cameraTransform.position + Vector3.down, character.lockOnTransform.position, out _, environmentLayer))
                    {
                        availableTargets.Add(character);
                    }
                }
            }
        }

        return availableTargets;
    }

    private CharacterManager FindNearestLockOnTarget()
    {
        CharacterManager nearestTarget = null;
        float shortestDistanceToTarget = Mathf.Infinity;

        var availableTargets = GetAvailableLockOnTargets();

        foreach (var target in availableTargets)
        {
            float distanceToTarget = Vector3.Distance(playerTransform.position, target.transform.position);

            if (distanceToTarget < shortestDistanceToTarget)
            {
                shortestDistanceToTarget = distanceToTarget;
                nearestTarget = target;
            }
        }

        return nearestTarget;
    }

    private CharacterManager FindLeftOfLockOnTarget()
    {
        CharacterManager leftLockTarget = null;
        float shortestDistance = -Mathf.Infinity;

        var availableTargets = GetAvailableLockOnTargets();

        foreach (var target in availableTargets)
        {
            if (target == currentLockOnTarget)
                continue;

            Vector3 relativeTargetPosition = cameraTransform.transform.InverseTransformPoint(target.transform.position);

            if (relativeTargetPosition.x <= 0f && relativeTargetPosition.x > shortestDistance)
            {
                shortestDistance = relativeTargetPosition.x;
                leftLockTarget = target;
            }
        }

        return leftLockTarget;
    }

    private CharacterManager FindRightOfLockOnTarget()
    {
        CharacterManager rightLockTarget = null;
        float shortestDistance = Mathf.Infinity;

        var availableTargets = GetAvailableLockOnTargets();

        foreach (var target in availableTargets)
        {
            if (target == currentLockOnTarget)
                continue;

            Vector3 relativeTargetPosition = cameraTransform.transform.InverseTransformPoint(target.transform.position);

            if (relativeTargetPosition.x >= 0f && relativeTargetPosition.x < shortestDistance)
            {
                shortestDistance = relativeTargetPosition.x;
                rightLockTarget = target;
            }
        }

        return rightLockTarget;
    }

    private void UpdateCameraHeight()
    {
        Vector3 newCameraPosition = currentLockOnTarget != null ?
            new Vector3(0, lockedPivotPosition) : new Vector3(0, unlockedPivotPosition);

        cameraPivotTransform.localPosition =
            Vector3.SmoothDamp(cameraPivotTransform.localPosition, newCameraPosition, ref cameraMovingVelocity, cameraMovingSmoothTime);
    }

    #region Camera Lock On View Cone Gizmos
    public int coneResolution = 36; // More segments = smoother cone

    private void OnDrawGizmos()
    {
        if (cameraTransform == null) return;

        DrawLockOnCone();
    }

    private void DrawLockOnCone()
    {
        Gizmos.color = new Color(0, 1, 1, 0.25f); // Cyan with transparency

        Vector3 coneTip = cameraTransform.position;
        Vector3 coneBaseCenter = coneTip + cameraTransform.forward * maximumLockOnDistance;

        // Calculate cone radius using trigonometry
        float coneRadius = maximumLockOnDistance * Mathf.Tan(maximumLockOnAngle * Mathf.Deg2Rad);

        // Draw radial lines
        Vector3 previousPoint = coneBaseCenter;
        for (int i = 0; i <= coneResolution; i++)
        {
            float angle = (float)i / coneResolution * 360f;
            Vector3 dir = Quaternion.AngleAxis(angle, cameraTransform.forward) * cameraTransform.up;
            Vector3 point = coneBaseCenter + dir * coneRadius;

            // Draw line from camera to edge
            Gizmos.DrawLine(coneTip, point);

            // Draw circumference
            if (i > 0)
            {
                Gizmos.DrawLine(previousPoint, point);
            }
            previousPoint = point;
        }

        // Draw central axis
        Gizmos.color = Color.yellow;
        Gizmos.DrawLine(coneTip, coneBaseCenter);
    }
    #endregion
}
