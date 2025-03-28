using System.Collections.Generic;
using UnityEngine;

public class CameraHandler : MonoBehaviour
{
    [SerializeField] private Transform targetTransform;
    [SerializeField] private Transform cameraTransform;
    [SerializeField] private Transform cameraPivotTransform;
    [SerializeField] private LayerMask lockOnLayerMask;

    private Vector3 cameraTransformPosition;
    public LayerMask ignoreLayers;

    private Vector3 cameraFollowVelocity = Vector3.zero;

    public float lookSpeed = 0.1f;
    public float followSpeed = 0.1f;
    public float pivotSpeed = 0.03f;

    private float targetPosition;
    private float defaultPosition;
    private float lookAngle;
    private float pivotAngle;
    public float minimumPivot = -35;
    public float maximumPivot = 35;

    [SerializeField] private float cameraSphereRadius = 0.2f;
    [SerializeField] private float cameraCollisionOffset = 0.2f;
    [SerializeField] private float cameraMinimumCollisionOffset = 0.2f;

    [SerializeField] private float maximumLockOnDistance = 30f;
    private List<CharacterManager> avaibleTargets = new ();
    private Transform currentLockOnTarget;
    private Transform nearestLockOnTarget;
    private Transform leftLockTarget;
    private Transform rightLockTarget;

    private bool lockOnFlag = false;

    public static CameraHandler Instance { get; private set; }

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            //DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }

        defaultPosition = cameraTransform.localPosition.z;
        ignoreLayers = ~(1 << 8 | 1 << 9 | 1 << 10);
    }

    private void OnEnable()
    {
        InputHandler.LockOnButtonPressed += HandleLockOnButtonPressed;
        InputHandler.LeftLockOnTargetButtonPressed += HandleLeftLockOnButtonPressed;
        InputHandler.RightLockOnTargetButtonPressed += HandleRightLockOnButtonPressed;
    }

    private void OnDisable()
    {
        InputHandler.LockOnButtonPressed -= HandleLockOnButtonPressed;
    }

    public void FollowTarget(float deltaTime)
    {
        Vector3 targetPosition = Vector3.SmoothDamp
            (transform.position, targetTransform.position, ref cameraFollowVelocity, deltaTime / followSpeed);
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
            float velocity = 0;

            Vector3 cameraDirection = currentLockOnTarget.position - transform.position;
            cameraDirection.Normalize();
            cameraDirection.y = 0;

            Quaternion targetRotation = Quaternion.LookRotation(cameraDirection);
            transform.rotation = targetRotation;

            Vector3 pivotDirection = currentLockOnTarget.position - cameraPivotTransform.position;
            pivotDirection.Normalize();

            targetRotation = Quaternion.LookRotation(pivotDirection);
            Vector3 eulerAngle = targetRotation.eulerAngles;
            eulerAngle.y = 0;
            cameraPivotTransform.localEulerAngles = eulerAngle;
        }
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

    private void HandleLockOnButtonPressed()
    {
        if (!lockOnFlag)
        {
            HandleLockOn();

            if (nearestLockOnTarget != null)
            {
                currentLockOnTarget = nearestLockOnTarget;
                lockOnFlag = true;
            }
        }
        else
        {
            lockOnFlag = false;
            ClearLockOnTargets();
        }
    }

    private void HandleLeftLockOnButtonPressed()
    {
        if (lockOnFlag)
        {
            HandleLockOn();
            if (leftLockTarget != null)
            {
                currentLockOnTarget = leftLockTarget;
            }
        }
    }

    private void HandleRightLockOnButtonPressed()
    {
        if (lockOnFlag)
        {
            HandleLockOn();
            if (rightLockTarget != null)
            {
                currentLockOnTarget = rightLockTarget;
            }
        }
    }

    private void HandleLockOn()
    {
        float shortestDistanceToTarget = Mathf.Infinity;
        float shortestDistanceOfLeftTarget = Mathf.Infinity;
        float shortestDistanceOfRightTarget = Mathf.Infinity;

        Collider[] colliders = Physics.OverlapSphere(targetTransform.position, 26, lockOnLayerMask);

        for (int i = 0; i < colliders.Length; i++)
        {
            CharacterManager character = colliders[i].GetComponent<CharacterManager>();

            if (character != null)
            {
                Vector3 lockTargetDirection = character.transform.position - targetTransform.position;
                float distanceFromTarget = Vector3.Distance(targetTransform.position, character.transform.position);
                float viewableAngle = Vector3.Angle(lockTargetDirection, cameraTransform.forward);

                if (character.transform.root !=  targetTransform.transform.root
                    && viewableAngle > -50 && viewableAngle < 50
                    && distanceFromTarget <= maximumLockOnDistance)
                {
                    avaibleTargets.Add(character);
                }
            }
        }

        for (int j =  0; j < avaibleTargets.Count; j++)
        {
            float distanceFromTarget = Vector3.Distance(targetTransform.position, avaibleTargets[j].transform.position);

            if (distanceFromTarget < shortestDistanceToTarget)
            {
                shortestDistanceToTarget = distanceFromTarget;
                nearestLockOnTarget = avaibleTargets[j].lockOnTransform;
            }

            if (lockOnFlag)
            {
                Vector3 relativeEnemyPosition = currentLockOnTarget.InverseTransformPoint(avaibleTargets[j].transform.position);
                var distanceFromLeftTarget = currentLockOnTarget.transform.position.x - avaibleTargets[j].transform.position.x;
                var distanceFromRightTarget = currentLockOnTarget.transform.position.x + avaibleTargets[j].transform.position.x;

                if (relativeEnemyPosition.x > 0f && distanceFromLeftTarget < shortestDistanceToTarget)
                {
                    shortestDistanceOfLeftTarget = distanceFromLeftTarget;
                    leftLockTarget = avaibleTargets[j].lockOnTransform;
                }

                if (relativeEnemyPosition.x <  0f && distanceFromRightTarget < shortestDistanceToTarget)
                {
                    shortestDistanceOfRightTarget = distanceFromRightTarget;
                    rightLockTarget = avaibleTargets[j].lockOnTransform;
                }
            }
        }
    }

    private void ClearLockOnTargets()
    {
        avaibleTargets.Clear();
        nearestLockOnTarget = null;
        currentLockOnTarget = null;
    }
}
