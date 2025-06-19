using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class CameraController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform playerTransform;
    [SerializeField] private Transform cameraActualTransform; // For vertical rotation (child of this rig)
    [SerializeField] private Transform cameraPivotTransform; // The Unity Camera's transform

    [Header("General Settings")]
    [SerializeField] private float generalSmoothTime = 0.15f;
    [SerializeField] private LayerMask obstructionLayers;
    [SerializeField] private LayerMask lockOnTargetLayers; // Layers enemies are on
    [SerializeField] private float sphereCastRadius = 0.25f;
    [SerializeField] private float minCollisionPull = 0.3f; // How much to pull camera from wall

    [Header("Normal Mode Settings")]
    [SerializeField] private float normalRotationSensitivityX = 3f;
    [SerializeField] private float normalRotationSensitivityY = 2f;
    [SerializeField] private float normalModeRotationSmoothing = 15f;
    [SerializeField] private float normalDefaultDistance = 3.5f;
    [SerializeField] private float normalMinPitch = -35f;
    [SerializeField] private float normalMaxPitch = 70f;
    [SerializeField] private Vector3 normalPivotOffset = new Vector3(0, 1.65f, 0);
    [SerializeField] private float normalModeZoomSpeed = 20f;

    [Header("Lock-On Mode Settings")]
    [SerializeField] private float lockOnRotationSpeed = 10f;
    [SerializeField] private float lockOnDesiredDistance = 4.0f;
    [SerializeField] private Vector3 lockOnPivotOffset = new Vector3(0, 1.8f, 0);
    [SerializeField] private float maxLockOnSearchDistance = 25f;
    [SerializeField] private float maxLockOnSearchAngle = 60f; // Degrees from camera forward
    [SerializeField] private float lockOnLOSCheckRate = 4f; // Checks per second for Line of Sight
    [SerializeField] private float obstacleGracePeriod = 0.75f; // Seconds to maintain lock through obstacles
    [SerializeField] private float closeProximityThreshold = 2.5f; // When target is this close
    [SerializeField] private float closeProximityPullback = 1.5f; // How much to pull back/up camera
    [SerializeField] private float lockOnModeZoomSpeed = 15f;

    // Public Properties
    public bool IsLockedOn => _currentLockOnTarget != null;
    public CharacterManager CurrentLockOnTarget => _currentLockOnTarget;

    // Private State
    private Vector3 _cameraRigFollowVelocity = Vector3.zero;
    private Vector3 _pivotPositionVelocity = Vector3.zero;
    private float _targetYaw;    // Desired horizontal rotation (around player Y axis)
    private float _targetPitch;  // Desired vertical rotation (of pivot X axis)
    private float _currentYaw;
    private float _currentPitch;

    private float _rawMouseX;
    private float _rawMouseY;

    private CharacterManager _currentLockOnTarget;
    private float _timeTargetObstructed = 0f;
    private float _losCheckInterval;
    private float _losCheckTimer;
    private bool _isTargetCurrentlyConsideredObstructed; // Stores the result of the last LOS check
    private float _currentDesiredDistance; // Target distance before collision for current mode
    private Vector3 _finalCameraLocalPosition; // Target local pos for cameraActualTransform

    private List<CharacterManager> _availableTargets = new List<CharacterManager>();
    private int _currentTargetIndex = -1;
    private bool isFollowing = true;

    private void Awake()
    {
        if (playerTransform == null || cameraActualTransform == null || cameraPivotTransform == null)
        {
            Debug.LogError("Camera references not set!");
            enabled = false;
            return;
        }

        // Initialize based on player's forward or current camera setup
        _targetYaw = playerTransform.eulerAngles.y;
        _currentYaw = _targetYaw;
        _targetPitch = cameraPivotTransform.localEulerAngles.x; // Use initial pitch
        if (_targetPitch > 180) _targetPitch -= 360;
        _currentPitch = _targetPitch;

        _currentDesiredDistance = normalDefaultDistance;
        _finalCameraLocalPosition = new Vector3(0, 0, -_currentDesiredDistance);
        cameraActualTransform.localPosition = _finalCameraLocalPosition;

        //Cursor.lockState = CursorLockMode.Locked;
        //Cursor.visible = false;

        // Initialize LOS check variables
        if (lockOnLOSCheckRate > 0f)
        {
            _losCheckInterval = 1.0f / lockOnLOSCheckRate;
        }
        else
        {
            _losCheckInterval = 0.25f; // Fallback to check 4 times every sec if rate is invalid
        }
        _losCheckTimer = 0f; // Start with an immediate check or check on first lock-on
        _isTargetCurrentlyConsideredObstructed = false;
    }

    private void OnEnable()
    {
        InputHandler.CameraMovementPerformed += OnCameraInput;
        InputHandler.LockOnButtonPressed += ToggleLockOn;
        InputHandler.LeftLockOnTargetButtonPressed += SwitchLockOnTargetLeft;
        InputHandler.RightLockOnTargetButtonPressed += SwitchLockOnTargetRight;
    }

    private void OnDisable()
    {
        InputHandler.CameraMovementPerformed -= OnCameraInput;
        InputHandler.LockOnButtonPressed -= ToggleLockOn;
        InputHandler.LeftLockOnTargetButtonPressed -= SwitchLockOnTargetLeft;
        InputHandler.RightLockOnTargetButtonPressed -= SwitchLockOnTargetRight;
    }

    private void LateUpdate()
    {
        if (!isFollowing) return;

        float dt = Time.deltaTime;
        if (dt <= 0 || playerTransform == null) return;

        HandleModeLogic(dt);    // Determine target Yaw/Pitch/Distance/Pivot based on mode
        FollowPlayerRig(dt);    // Move the main camera rig (this.transform)
        RotateCameraRig(dt);    // Apply Yaw (horizontal) to rig, Pitch (vertical) to pivot
        HandleCollisions(dt);   // Adjust _finalCameraLocalPosition based on collisions
        ApplyCameraTransforms(dt); // Smoothly move actual camera to _finalCameraLocalPosition
    }

    private void OnCameraInput(Vector2 input)
    {
        _rawMouseX = input.x;
        _rawMouseY = input.y;
    }

    private void FollowPlayerRig(float dt)
    {
        Vector3 targetRigPosition = playerTransform.position;
        transform.position = Vector3.SmoothDamp(transform.position, targetRigPosition, ref _cameraRigFollowVelocity, generalSmoothTime, Mathf.Infinity, dt);
    }

    private void HandleModeLogic(float dt)
    {
        if (IsLockedOn)
        {
            // --- LOCK-ON MODE LOGIC ---
            if (_currentLockOnTarget == null || !_currentLockOnTarget.isActiveAndEnabled) // Target might have been destroyed
            {
                ClearLockOn(true); // Preserve orientation
                return; // Exit early as mode might have changed
            }

            _losCheckTimer -= dt;
            bool performFullLOSCheckThisFrame = _losCheckInterval <= 0f || _losCheckTimer <= 0f; // Check if interval is zero for per-frame

            if (performFullLOSCheckThisFrame)
            {
                if (_losCheckInterval > 0f) // Reset timer only if we are using timed intervals
                {
                    _losCheckTimer = _losCheckInterval;
                }

                // Check Line of Sight and grace period
                if (!IsTargetEffectivelyVisible(_currentLockOnTarget))
                {
                    // If target was previously considered visible (or this is the first check after lock-on)
                    // and now it's not, this is the *start* of an obstruction period.
                    if (!_isTargetCurrentlyConsideredObstructed)
                    {
                        _timeTargetObstructed = 0f; // Reset/Start the grace timer from this point
                    }
                    _isTargetCurrentlyConsideredObstructed = true;
                }
                else
                {
                    _isTargetCurrentlyConsideredObstructed = false;
                    _timeTargetObstructed = 0f; // Target is visible, so clear any accumulated obstruction time
                }
            }

            // Grace period handling: This runs every frame, but _timeTargetObstructed
            // only accumulates meaningfully if _isTargetCurrentlyConsideredObstructed is true.
            if (_isTargetCurrentlyConsideredObstructed)
            {
                _timeTargetObstructed += dt; // Accumulate time while considered obstructed
                if (_timeTargetObstructed >= obstacleGracePeriod)
                {
                    ClearLockOn(true); // Preserve orientation
                    return; // Exit early
                }
            }

            // Update target Yaw/Pitch to look at target
            // Calculate direction from pivot's world position to target's lock-on point
            Vector3 directionToTarget = (_currentLockOnTarget.lockOnTransform.position - cameraPivotTransform.position).normalized;
            if (directionToTarget != Vector3.zero)
            {
                Quaternion targetLookRotation = Quaternion.LookRotation(directionToTarget);
                _targetYaw = targetLookRotation.eulerAngles.y;
                _targetPitch = targetLookRotation.eulerAngles.x;
                if (_targetPitch > 180) _targetPitch -= 360f; // Normalize pitch
            }

            // Adjust camera pivot position and desired distance for lock-on
            cameraPivotTransform.localPosition = Vector3.SmoothDamp(cameraPivotTransform.localPosition, lockOnPivotOffset, ref _pivotPositionVelocity, generalSmoothTime, Mathf.Infinity, dt);

            float desiredDist = lockOnDesiredDistance;
            float distToTargetEntity = Vector3.Distance(playerTransform.position, _currentLockOnTarget.transform.position);
            if (distToTargetEntity < closeProximityThreshold)
            {
                float proximityFactor = Mathf.Clamp01(1f - (distToTargetEntity / closeProximityThreshold));
                desiredDist += closeProximityPullback * proximityFactor;
            }
            _currentDesiredDistance = Mathf.Lerp(_currentDesiredDistance, desiredDist, lockOnModeZoomSpeed * dt);
        }
        else
        {
            // --- NORMAL MODE LOGIC ---
            _targetYaw += _rawMouseX * normalRotationSensitivityX * dt * 100f;
            _targetPitch -= _rawMouseY * normalRotationSensitivityY * dt * 100f;

            _targetPitch = Mathf.Clamp(_targetPitch, normalMinPitch, normalMaxPitch);

            cameraPivotTransform.localPosition = Vector3.SmoothDamp(cameraPivotTransform.localPosition, normalPivotOffset, ref _pivotPositionVelocity, generalSmoothTime, Mathf.Infinity, dt);
            _currentDesiredDistance = Mathf.Lerp(_currentDesiredDistance, normalDefaultDistance, normalModeZoomSpeed * dt);
        }
    }

    private void RotateCameraRig(float dt)
    {
        float effectiveRotationSpeed = IsLockedOn ? lockOnRotationSpeed : normalModeRotationSmoothing; // Normal mode is input driven, lock-on is auto

        _currentYaw = Mathf.LerpAngle(_currentYaw, _targetYaw, effectiveRotationSpeed * dt);
        _currentPitch = Mathf.LerpAngle(_currentPitch, _targetPitch, effectiveRotationSpeed * dt);

        transform.rotation = Quaternion.Euler(0f, _currentYaw, 0f);
        cameraPivotTransform.localRotation = Quaternion.Euler(_currentPitch, 0f, 0f);
    }

    private void HandleCollisions(float dt)
    {
        float targetDist = _currentDesiredDistance;

        // Desired position if no collision
        Vector3 idealLocalPos = new Vector3(0, 0, -targetDist);
        Vector3 desiredWorldPos = cameraPivotTransform.TransformPoint(idealLocalPos);

        Vector3 castOrigin = cameraPivotTransform.position;
        Vector3 castDirection = (desiredWorldPos - castOrigin).normalized;
        float castDistance = Vector3.Distance(castOrigin, desiredWorldPos);

        if (castDistance < 0.01f) // Avoid issues with zero distance
        {
            _finalCameraLocalPosition = idealLocalPos;
            return;
        }

        RaycastHit hit;
        if (Physics.SphereCast(castOrigin, sphereCastRadius, castDirection, out hit, castDistance, obstructionLayers, QueryTriggerInteraction.Ignore))
        {
            // Collision detected, pull camera in
            float pullInDistance = hit.distance - minCollisionPull;
            _finalCameraLocalPosition.z = -Mathf.Max(pullInDistance, 0.1f); // Ensure it's not too close or negative due to pull
        }
        else
        {
            _finalCameraLocalPosition.z = -targetDist;
        }
    }

    private void ApplyCameraTransforms(float dt)
    {
        // Smoothly move the actual camera to its final local Z position
        // X and Y local position should remain 0 relative to the pivot.
        Vector3 targetLocalPos = new Vector3(0, _finalCameraLocalPosition.y, _finalCameraLocalPosition.z); // Keep Y if you ever want to offset it
        cameraActualTransform.localPosition = Vector3.Lerp(cameraActualTransform.localPosition, targetLocalPos, generalSmoothTime * 15f * dt); // Faster lerp for responsiveness
    }

    private void ToggleLockOn()
    {
        if (IsLockedOn)
        {
            ClearLockOn(true); // True to preserve orientation seamlessly
        }
        else
        {
            FindAndSetBestLockOnTarget();
        }
    }

    private void FindAndSetBestLockOnTarget()
    {
        CharacterManager bestTarget = FindBestTargetInView();
        if (bestTarget != null)
        {
            SetLockOn(bestTarget);
        }
    }

    private void SetLockOn(CharacterManager target)
    {
        if (_currentLockOnTarget != null)
        {
            _currentLockOnTarget.OnDeath -= HandleTargetDied;
        }

        _currentLockOnTarget = target;
        if (_currentLockOnTarget != null)
        {
            _currentLockOnTarget.OnDeath += HandleTargetDied;

            // Reset LOS and grace period state for new target
            _timeTargetObstructed = 0f;
            _losCheckTimer = 0f; // Force an LOS check in the next HandleModeLogic update
            _isTargetCurrentlyConsideredObstructed = false; // Assume visible until first check

            RefreshAvailableTargets();
            _currentTargetIndex = _availableTargets.IndexOf(_currentLockOnTarget);
        }
    }

    private void ClearLockOn(bool preserveOrientation)
    {
        if (_currentLockOnTarget != null)
        {
            _currentLockOnTarget.OnDeath -= HandleTargetDied;
        }
        _currentLockOnTarget = null;
        _timeTargetObstructed = 0f;
        _currentTargetIndex = -1;
        _availableTargets.Clear();

        if (preserveOrientation)
        {
            // This is CRITICAL for seamless transition:
            // Set normal mode's target yaw/pitch to current camera's actual orientation.
            _targetYaw = transform.eulerAngles.y; // Rig's current world Y rotation
            _targetPitch = cameraPivotTransform.localEulerAngles.x; // Pivot's current local X rotation
            if (_targetPitch > 180) _targetPitch -= 360f; // Normalize

            // _currentYaw and _currentPitch will smoothly lerp towards these
            // but since they are already AT these values (or very close), it's seamless.
        }
        // PlayerLocomotion will pick up IsLockedOn being false and revert to normal movement.
    }

    private void HandleTargetDied()
    {
        Debug.Log($"Lock-on target has died. Finding a new target.");

        // The current target is dead, so it's effectively null. Unsubscribe just in case.
        if (_currentLockOnTarget != null)
        {
            _currentLockOnTarget.OnDeath -= HandleTargetDied;
        }

        _currentLockOnTarget = null; // Clear the reference to the dead target

        // Find the next best target currently in view
        CharacterManager newTarget = FindBestTargetInView();

        if (newTarget != null)
        {
            // If a new valid target is found, switch to it immediately.
            Debug.Log($"Auto-switching lock-on to new target: {newTarget.name}");
            SetLockOn(newTarget);
        }
        else
        {
            // If no other targets are available, clear the lock-on completely.
            Debug.Log("No other targets found. Clearing lock-on.");
            ClearLockOn(true); // 'true' to preserve camera orientation seamlessly
        }
    }

    private bool IsTargetEffectivelyVisible(CharacterManager target)
    {
        if (target == null || target.lockOnTransform == null) return false;

        Vector3 viewOrigin = cameraActualTransform.position; // Or cameraPivotTransform.position
        Vector3 targetPoint = target.lockOnTransform.position;
        Vector3 directionToTarget = targetPoint - viewOrigin;

        // Distance check (more generous than search distance as target is already locked)
        if (directionToTarget.magnitude > maxLockOnSearchDistance * 1.5f)
        {
            return false;
        }

        // Linecast from camera to target. If it hits something that isn't the target, it's obstructed.
        if (Physics.Linecast(viewOrigin, targetPoint, out RaycastHit hit, obstructionLayers, QueryTriggerInteraction.Ignore))
        {
            if (hit.transform.root != target.transform.root) // Check root in case target has complex hierarchy
            {
                return false; // Obstructed by something else
            }
        }
        return true;
    }

    private void RefreshAvailableTargets()
    {
        _availableTargets.Clear();
        Collider[] colliders = Physics.OverlapSphere(playerTransform.position, maxLockOnSearchDistance, lockOnTargetLayers);

        Vector3 cameraViewForward = cameraActualTransform.forward; // Use actual camera's forward

        foreach (var col in colliders)
        {
            CharacterManager potentialTarget = col.GetComponent<CharacterManager>();
            if (potentialTarget == null || potentialTarget.IsDead || !potentialTarget.lockOnTransform || potentialTarget == playerTransform.GetComponent<CharacterManager>() || !potentialTarget.isActiveAndEnabled)
                continue;

            Vector3 directionToTarget = potentialTarget.lockOnTransform.position - cameraActualTransform.position;
            float angle = Vector3.Angle(cameraViewForward, directionToTarget.normalized);

            if (angle <= maxLockOnSearchAngle && IsTargetEffectivelyVisible(potentialTarget))
            {
                _availableTargets.Add(potentialTarget);
            }
        }

        // Sort by angle primarily, then distance
        _availableTargets = _availableTargets.OrderBy(t => Vector3.Angle(cameraViewForward, (t.lockOnTransform.position - cameraActualTransform.position).normalized))
                                           .ThenBy(t => Vector3.Distance(playerTransform.position, t.transform.position))
                                           .ToList();
    }

    private CharacterManager FindBestTargetInView()
    {
        RefreshAvailableTargets();
        return _availableTargets.FirstOrDefault(); // The first one after sorting is the "best"
    }

    private void SwitchLockOnTarget(int direction) // -1 for left, 1 for right
    {
        if (!IsLockedOn || _availableTargets.Count <= 1) return;

        RefreshAvailableTargets(); // Ensure list is up-to-date
        if (_availableTargets.Count <= 1) return; // Still not enough

        _currentTargetIndex = _availableTargets.IndexOf(_currentLockOnTarget);
        if (_currentTargetIndex == -1) // Current target not in list (e.g. died, out of range)
        {
            FindAndSetBestLockOnTarget();
            return;
        }

        // Screen-space based switching is more intuitive for "left/right"
        // Convert target positions to screen space relative to current target
        Vector3 currentTargetScreenPos = Camera.main.WorldToScreenPoint(_currentLockOnTarget.lockOnTransform.position);

        CharacterManager bestCandidate = null;
        float bestScore = (direction == 1) ? float.MaxValue : float.MinValue; // Right: Min X greater than current. Left: Max X less than current.

        foreach (var target in _availableTargets)
        {
            if (target == _currentLockOnTarget) continue;

            Vector3 candidateScreenPos = Camera.main.WorldToScreenPoint(target.lockOnTransform.position);
            float xDelta = candidateScreenPos.x - currentTargetScreenPos.x;

            if (direction == 1) // Right
            {
                if (xDelta > 0 && xDelta < bestScore) // Find smallest positive X delta
                {
                    bestScore = xDelta;
                    bestCandidate = target;
                }
            }
            else // Left (direction == -1)
            {
                if (xDelta < 0 && xDelta > bestScore) // Find largest (closest to zero) negative X delta
                {
                    bestScore = xDelta;
                    bestCandidate = target;
                }
            }
        }

        if (bestCandidate != null)
        {
            SetLockOn(bestCandidate);
        }
        else // If no target strictly to the left/right, wrap around (optional)
        {
            // Simplistic wrap: go to next/prev in sorted list
            _currentTargetIndex = (_currentTargetIndex + direction + _availableTargets.Count) % _availableTargets.Count;
            SetLockOn(_availableTargets[_currentTargetIndex]);
        }
    }

    private void SwitchLockOnTargetLeft()
    {
        if (IsLockedOn) SwitchLockOnTarget(-1);
    }

    private void SwitchLockOnTargetRight()
    {
        if (IsLockedOn) SwitchLockOnTarget(1);
    }

    public void StopFollowing()
    {
        isFollowing = false;
    }

    public void StartFollowing()
    {
        isFollowing = true;
    }

    // Gizmos for debugging
    private void OnDrawGizmosSelected()
    {
        if (cameraPivotTransform == null || cameraActualTransform == null) return;

        // Draw collision sphere
        Gizmos.color = Color.yellow;
        Vector3 desiredWorldPos = cameraPivotTransform.TransformPoint(new Vector3(0, 0, -_currentDesiredDistance));
        Vector3 castOrigin = cameraPivotTransform.position;
        Vector3 castDirection = (desiredWorldPos - castOrigin).normalized;
        float castDistance = Vector3.Distance(castOrigin, desiredWorldPos);
        Gizmos.DrawWireSphere(castOrigin + castDirection * castDistance, sphereCastRadius);

        // Draw line to current lock on target
        if (IsLockedOn && _currentLockOnTarget != null && _currentLockOnTarget.lockOnTransform != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawLine(cameraActualTransform.position, _currentLockOnTarget.lockOnTransform.position);
        }

        // Draw Lock-on search cone (approximate)
        Gizmos.color = new Color(0, 1, 1, 0.25f);
        Gizmos.matrix = Matrix4x4.TRS(cameraActualTransform.position, cameraActualTransform.rotation, Vector3.one);
        Gizmos.DrawFrustum(Vector3.zero, maxLockOnSearchAngle * 2f, maxLockOnSearchDistance, 0.1f, 1f);
    }
}
