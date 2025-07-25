using UnityEngine;
using Reflex.Attributes;
using System.Collections;
using Portfolio.InputSystem;
using Portfolio.InputSystem.Mobile;

namespace Portfolio.CameraSystem
{
    /// <summary>
    /// Enhanced camera follow system with platform-specific rotation handling
    /// </summary>
    public class CameraFollowSystem : MonoBehaviour
    {
        [Header("Settings")]
        [SerializeField][Inject] private CameraSettings cameraSettings;

        [Header("Debug")]
        [SerializeField] private bool enableDebugDraw = true;

        // Components
        private Camera cam;

        private Transform playerTransform;
        private Transform cameraTransform;

        // Platform handling
        [Inject] private IPlatformDetector platformDetector;

        [Inject] private IPlatformRotationFactory rotationFactory;
        private IPlatformRotationInputHandler rotationInputHandler;

        // Rotation state
        private float currentYaw = 0f;

        private float currentPitch = 0f;
        private float targetYaw = 0f;
        private float targetPitch = 0f;

        // Follow state
        private Vector3 desiredPosition;

        private Vector3 currentVelocity;
        private Vector3 rotationVelocity;

        // Player rotation override
        private PlayerMovementInput playerMovement;

        private bool isOverridingPlayerRotation = false;

        // Collision detection
        private float currentFollowDistance;

        #region Unity Lifecycle

        private void Awake()
        {
            InitializeComponents();
        }

        private void Start()
        {
            StartCoroutine(InitializeAsync());
        }

        private void Update()
        {
            if (playerTransform == null || cameraSettings == null) return;

            UpdateRotationInput();
            UpdateCameraPosition();
            UpdatePlayerRotation();
        }

        private void LateUpdate()
        {
            if (playerTransform == null || cameraSettings == null) return;

            ApplyCameraTransform();
        }

        private void OnDestroy()
        {
            CleanupRotationHandler();
        }

        #endregion Unity Lifecycle

        #region Initialization

        private void InitializeComponents()
        {
            cam = GetComponent<Camera>();
            if (cam == null)
            {
                cam = Camera.main;
            }

            cameraTransform = transform;
            currentFollowDistance = cameraSettings?.FollowDistance ?? 5f;
        }

        private IEnumerator InitializeAsync()
        {
            // Wait for platform detector to be ready
            yield return new WaitUntil(() => platformDetector != null);

            // Find player
            yield return StartCoroutine(FindPlayerAsync());

            // Initialize rotation handler
            InitializeRotationHandler();

            // Set initial camera position
            SetInitialCameraPosition();

            // Override player rotation if needed
            SetupPlayerRotationOverride();
        }

        private IEnumerator FindPlayerAsync()
        {
            // Look for player with specific tag first
            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");

            if (playerObj == null)
            {
                // Look for EnhancedMovementInput component
                playerObj = FindFirstObjectByType<PlayerMovementInput>()?.gameObject;
            }

            if (playerObj == null)
            {
                Debug.LogError("CameraFollowSystem: Player not found! Please ensure player has 'Player' tag or EnhancedMovementInput component.");
                yield break;
            }

            playerTransform = playerObj.transform;
            playerMovement = playerObj.GetComponent<PlayerMovementInput>();

            //Debug.Log($"CameraFollowSystem: Found player - {playerObj.name}");
        }

        private void InitializeRotationHandler()
        {
            if (platformDetector == null) return;

            rotationInputHandler = rotationFactory.GetHandler(platformDetector.CurrentPlatform);
            if (platformDetector.CurrentPlatform == GamePlatform.PC)
            {
                ((PCRotationInputHandler)rotationInputHandler).SetSettings(cameraSettings.PCSettings);
            }
            else if (platformDetector.CurrentPlatform == GamePlatform.Mobile)
            {
                ((MobileRotationInputHandler)rotationInputHandler).SetSettings(cameraSettings.MobileSettings);
            }
            rotationInputHandler.Initialize();
            rotationInputHandler.OnRotationInput += HandleRotationInput;
            rotationInputHandler.SetEnabled(true);
        }

        private void SetInitialCameraPosition()
        {
            if (playerTransform == null) return;

            // Calculate initial position based on current camera transform
            Vector3 playerPosition = playerTransform.position;

            // Use current camera position to determine initial yaw and pitch
            Vector3 directionToCamera = cameraTransform.position - playerPosition;

            if (directionToCamera.magnitude > 0.1f)
            {
                // Calculate yaw and pitch from current position
                currentYaw = Mathf.Atan2(directionToCamera.x, directionToCamera.z) * Mathf.Rad2Deg;

                float horizontalDistance = new Vector3(directionToCamera.x, 0, directionToCamera.z).magnitude;
                currentPitch = Mathf.Atan2(directionToCamera.y, horizontalDistance) * Mathf.Rad2Deg;
            }
            else
            {
                // Default values if camera is too close
                currentYaw = 0f;
                currentPitch = 15f;
            }

            targetYaw = currentYaw;
            targetPitch = currentPitch;

            // Set initial follow distance based on current position
            currentFollowDistance = directionToCamera.magnitude;
            if (currentFollowDistance < cameraSettings.MinimumDistance)
            {
                currentFollowDistance = cameraSettings.FollowDistance;
            }
        }

        private void SetupPlayerRotationOverride()
        {
            if (playerMovement == null || !cameraSettings.SyncPlayerRotation) return;

            isOverridingPlayerRotation = true;

            // Set initial player rotation to match camera yaw
            if (playerTransform != null)
            {
                Quaternion targetRotation = Quaternion.Euler(0, currentYaw + 180f, 0);
                playerTransform.rotation = targetRotation;
            }
        }

        #endregion Initialization

        #region Input Handling

        private void UpdateRotationInput()
        {
            rotationInputHandler?.UpdateInput();
        }

        private void HandleRotationInput(Vector2 input)
        {
            if (cameraSettings == null) return;

            // Handle yaw (horizontal rotation)
            targetYaw += input.x;

            // Handle pitch (vertical rotation) with limits
            targetPitch -= input.y; // Negative because input.y is inverted for camera
            targetPitch = Mathf.Clamp(targetPitch, cameraSettings.MinVerticalAngle, cameraSettings.MaxVerticalAngle);

            // Smooth rotation
            currentYaw = Mathf.LerpAngle(currentYaw, targetYaw, Time.deltaTime / cameraSettings.RotationSmoothTime);
            currentPitch = Mathf.Lerp(currentPitch, targetPitch, Time.deltaTime / cameraSettings.RotationSmoothTime);
        }

        #endregion Input Handling

        #region Camera Position Update

        private void UpdateCameraPosition()
        {
            if (playerTransform == null || cameraSettings == null) return;

            // Calculate desired position based on follow distance, height, and rotation
            Vector3 playerPosition = playerTransform.position;

            // Calculate base position using spherical coordinates
            float yawRad = currentYaw * Mathf.Deg2Rad;
            float pitchRad = currentPitch * Mathf.Deg2Rad;

            // Calculate direction vector from player to camera
            Vector3 direction = new Vector3(
                Mathf.Sin(yawRad) * Mathf.Cos(pitchRad),
                Mathf.Sin(pitchRad),
                Mathf.Cos(yawRad) * Mathf.Cos(pitchRad)
            );

            // Apply collision detection
            float actualDistance = currentFollowDistance;
            if (cameraSettings.EnableCollisionDetection)
            {
                actualDistance = CalculateCollisionDistance(playerPosition, direction);
            }

            // Calculate base camera position
            Vector3 basePosition = playerPosition + direction * actualDistance;

            // Apply offset while maintaining the look direction
            Vector3 offsetPosition = ApplyOffset(basePosition, direction);

            desiredPosition = offsetPosition;
        }

        private Vector3 ApplyOffset(Vector3 basePosition, Vector3 cameraDirection)
        {
            if (cameraSettings.Offset == Vector3.zero) return basePosition;

            // Create a coordinate system at the camera position
            Vector3 forward = -cameraDirection.normalized; // Camera looks toward player
            Vector3 right = Vector3.Cross(Vector3.up, forward).normalized;
            Vector3 up = Vector3.Cross(forward, right).normalized;

            // Apply offset in local space
            Vector3 offset = right * cameraSettings.Offset.x +
                           up * cameraSettings.Offset.y +
                           forward * cameraSettings.Offset.z;

            return basePosition + offset;
        }

        private float CalculateCollisionDistance(Vector3 playerPosition, Vector3 direction)
        {
            float maxDistance = currentFollowDistance;

            // Cast a sphere from player to desired camera position
            RaycastHit hit;
            if (Physics.SphereCast(
                playerPosition,
                cameraSettings.CollisionRadius,
                direction,
                out hit,
                maxDistance,
                cameraSettings.CollisionLayers))
            {
                // Return distance to collision point, but not less than minimum distance
                return Mathf.Max(hit.distance - cameraSettings.CollisionRadius, cameraSettings.MinimumDistance);
            }

            return maxDistance;
        }

        private void ApplyCameraTransform()
        {
            if (cameraSettings == null) return;

            // Smooth position movement
            if (cameraSettings.EnableSmoothing)
            {
                cameraTransform.position = Vector3.SmoothDamp(
                    cameraTransform.position,
                    desiredPosition,
                    ref currentVelocity,
                    cameraSettings.PositionSmoothTime
                );
            }
            else
            {
                cameraTransform.position = desiredPosition;
            }

            // Always look at player (with offset consideration)
            Vector3 lookTarget = playerTransform.position + Vector3.up * cameraSettings.FollowHeight;
            cameraTransform.LookAt(lookTarget);
        }

        #endregion Camera Position Update

        #region Player Rotation Override

        private void UpdatePlayerRotation()
        {
            if (!isOverridingPlayerRotation || playerTransform == null || !cameraSettings.SyncPlayerRotation) return;

            // Only rotate player on Y axis (yaw), ignore pitch
            Quaternion targetRotation = Quaternion.Euler(0, currentYaw + 180f, 0);

            // Smooth player rotation
            playerTransform.rotation = Quaternion.Slerp(
                playerTransform.rotation,
                targetRotation,
                Time.deltaTime * cameraSettings.PlayerRotationSpeed
            );
        }

        #endregion Player Rotation Override

        #region Public Methods

        public void SetCameraSettings(CameraSettings settings)
        {
            cameraSettings = settings;

            // Update rotation handler settings if needed
            if (rotationInputHandler != null && platformDetector != null)
            {
                CleanupRotationHandler();
                InitializeRotationHandler();
            }
        }

        public void SetTarget(Transform target)
        {
            playerTransform = target;
            playerMovement = target?.GetComponent<PlayerMovementInput>();

            if (target != null)
            {
                SetInitialCameraPosition();
                SetupPlayerRotationOverride();
            }
        }

        public void SetRotationOverride(bool enable)
        {
            isOverridingPlayerRotation = enable && cameraSettings.SyncPlayerRotation;
        }

        public void ResetToPlayer()
        {
            if (playerTransform != null)
            {
                SetInitialCameraPosition();
            }
        }

        #endregion Public Methods

        #region Cleanup

        private void CleanupRotationHandler()
        {
            if (rotationInputHandler != null)
            {
                rotationInputHandler.OnRotationInput -= HandleRotationInput;
                rotationInputHandler.Cleanup();
                rotationInputHandler = null;
            }
        }

        #endregion Cleanup

        #region Debug Drawing

        private void OnDrawGizmos()
        {
            if (!enableDebugDraw || cameraSettings == null || !cameraSettings.ShowCameraGizmos) return;

            if (playerTransform != null)
            {
                // Draw follow distance
                Gizmos.color = Color.yellow;
                Gizmos.DrawWireSphere(playerTransform.position, currentFollowDistance);

                // Draw collision radius
                if (cameraSettings.EnableCollisionDetection)
                {
                    Gizmos.color = Color.red;
                    Gizmos.DrawWireSphere(transform.position, cameraSettings.CollisionRadius);
                }

                // Draw desired position
                Gizmos.color = Color.green;
                Gizmos.DrawWireSphere(desiredPosition, 0.2f);

                // Draw line from player to camera
                Gizmos.color = Color.blue;
                Gizmos.DrawLine(playerTransform.position, transform.position);
            }
        }

        #endregion Debug Drawing
    }
}