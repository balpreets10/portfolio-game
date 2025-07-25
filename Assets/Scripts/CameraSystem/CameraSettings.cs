using UnityEngine;

namespace Portfolio.CameraSystem
{
    /// <summary>
    /// Base class for platform-specific camera settings
    /// </summary>
    [System.Serializable]
    public abstract class BasePlatformCameraSettings
    {
        [Header("Sensitivity")]
        [SerializeField] protected float sensitivity = 2f;

        [SerializeField] protected float rotationDeadzone = 0.1f;
        [SerializeField] protected bool invertYAxis = false;

        public float Sensitivity => sensitivity;
        public float RotationDeadzone => rotationDeadzone;
        public bool InvertYAxis => invertYAxis;

        // Runtime modification support
        public virtual void SetSensitivity(float value) => sensitivity = Mathf.Max(0.1f, value);

        public virtual void SetRotationDeadzone(float value) => rotationDeadzone = Mathf.Max(0f, value);

        public virtual void SetInvertYAxis(bool value) => invertYAxis = value;

        public abstract void ValidateSettings();
    }

    /// <summary>
    /// PC-specific camera settings
    /// </summary>
    [System.Serializable]
    public class PCCameraSettings : BasePlatformCameraSettings
    {
        [Header("PC Specific")]
        [SerializeField] private float mouseScrollSensitivity = 2f;

        [SerializeField] private bool enableMouseScroll = true;

        public float MouseScrollSensitivity => mouseScrollSensitivity;
        public bool EnableMouseScroll => enableMouseScroll;

        public void SetMouseScrollSensitivity(float value) => mouseScrollSensitivity = Mathf.Max(0.1f, value);

        public void SetEnableMouseScroll(bool value) => enableMouseScroll = value;

        public override void ValidateSettings()
        {
            sensitivity = Mathf.Max(0.1f, sensitivity);
            mouseScrollSensitivity = Mathf.Max(0.1f, mouseScrollSensitivity);
            rotationDeadzone = Mathf.Max(0f, rotationDeadzone);
        }
    }

    /// <summary>
    /// Mobile-specific camera settings
    /// </summary>
    [System.Serializable]
    public class MobileCameraSettings : BasePlatformCameraSettings
    {
        [Header("Mobile Specific")]
        [SerializeField] private float touchDeadzone = 50f;

        [SerializeField] private float pinchSensitivity = 1f;
        [SerializeField] private bool enablePinchZoom = true;

        public float TouchDeadzone => touchDeadzone;
        public float PinchSensitivity => pinchSensitivity;
        public bool EnablePinchZoom => enablePinchZoom;

        public void SetTouchDeadzone(float value) => touchDeadzone = Mathf.Max(0f, value);

        public void SetPinchSensitivity(float value) => pinchSensitivity = Mathf.Max(0.1f, value);

        public void SetEnablePinchZoom(bool value) => enablePinchZoom = value;

        public override void ValidateSettings()
        {
            sensitivity = Mathf.Max(0.1f, sensitivity);
            touchDeadzone = Mathf.Max(0f, touchDeadzone);
            pinchSensitivity = Mathf.Max(0.1f, pinchSensitivity);
            rotationDeadzone = Mathf.Max(0f, rotationDeadzone);
        }
    }

    /// <summary>
    /// Updated camera settings with platform-specific composition
    /// </summary>
    [CreateAssetMenu(fileName = "CameraSettings", menuName = "Camera System/Camera Settings")]
    public class CameraSettings : ScriptableObject
    {
        [Header("Camera Movement")]
        [SerializeField] private float followDistance = 5f;

        [SerializeField] private float followHeight = 2f;
        [SerializeField] private Vector3 offset = Vector3.zero;
        [SerializeField] private bool enableSmoothing = true;
        [SerializeField] private float positionSmoothTime = 0.1f;
        [SerializeField] private float rotationSmoothTime = 0.05f;

        [Header("Rotation Limits")]
        [SerializeField] private float minVerticalAngle = -30f;

        [SerializeField] private float maxVerticalAngle = 60f;

        [Header("Player Rotation")]
        [SerializeField] private bool syncPlayerRotation = true;

        [SerializeField] private float playerRotationSpeed = 5f;

        [Header("Collision Detection - Universal")]
        [SerializeField] private bool enableCollisionDetection = true;

        [SerializeField] private LayerMask collisionLayers = -1;
        [SerializeField] private float collisionRadius = 0.3f;
        [SerializeField] private float minimumDistance = 1f;

        [Header("Platform Specific Settings")]
        [SerializeField] private PCCameraSettings pcSettings = new PCCameraSettings();

        [SerializeField] private MobileCameraSettings mobileSettings = new MobileCameraSettings();

        [Header("Debug")]
        [SerializeField] private bool showDebugInfo = false;

        [SerializeField] private bool showCameraGizmos = true;

        // Properties
        public float FollowDistance => followDistance;

        public float FollowHeight => followHeight;
        public Vector3 Offset => offset;
        public bool EnableSmoothing => enableSmoothing;
        public float PositionSmoothTime => positionSmoothTime;
        public float RotationSmoothTime => rotationSmoothTime;

        public float MinVerticalAngle => minVerticalAngle;
        public float MaxVerticalAngle => maxVerticalAngle;

        public bool SyncPlayerRotation => syncPlayerRotation;
        public float PlayerRotationSpeed => playerRotationSpeed;

        public bool EnableCollisionDetection => enableCollisionDetection;
        public LayerMask CollisionLayers => collisionLayers;
        public float CollisionRadius => collisionRadius;
        public float MinimumDistance => minimumDistance;

        public PCCameraSettings PCSettings => pcSettings;
        public MobileCameraSettings MobileSettings => mobileSettings;

        public bool ShowDebugInfo => showDebugInfo;
        public bool ShowCameraGizmos => showCameraGizmos;

        // Runtime modification methods
        public void SetFollowDistance(float value) => followDistance = Mathf.Max(0.1f, value);

        public void SetFollowHeight(float value) => followHeight = value;

        public void SetOffset(Vector3 value) => offset = value;

        public void SetPositionSmoothTime(float value) => positionSmoothTime = Mathf.Max(0.01f, value);

        public void SetRotationSmoothTime(float value) => rotationSmoothTime = Mathf.Max(0.01f, value);

        public void SetMinVerticalAngle(float value) => minVerticalAngle = value;

        public void SetMaxVerticalAngle(float value) => maxVerticalAngle = value;

        public void SetPlayerRotationSpeed(float value) => playerRotationSpeed = Mathf.Max(0.1f, value);

        public void SetMinimumDistance(float value) => minimumDistance = Mathf.Max(0.1f, value);

        /// <summary>
        /// Get platform-specific settings
        /// </summary>
        public BasePlatformCameraSettings GetPlatformSettings(GamePlatform platform)
        {
            switch (platform)
            {
                case GamePlatform.PC:
                    return pcSettings;

                case GamePlatform.Mobile:
                    return mobileSettings;

                default:
                    return pcSettings;
            }
        }

        /// <summary>
        /// Validate all settings
        /// </summary>
        public void ValidateSettings()
        {
            followDistance = Mathf.Max(0.1f, followDistance);
            positionSmoothTime = Mathf.Max(0.01f, positionSmoothTime);
            rotationSmoothTime = Mathf.Max(0.01f, rotationSmoothTime);
            collisionRadius = Mathf.Max(0.1f, collisionRadius);
            minimumDistance = Mathf.Max(0.1f, minimumDistance);

            //if (minVerticalAngle > maxVerticalAngle)
            //{
            //    float temp = minVerticalAngle;
            //    minVerticalAngle = maxVerticalAngle;
            //    maxVerticalAngle = temp;
            //}

            pcSettings.ValidateSettings();
            mobileSettings.ValidateSettings();
        }

        private void OnValidate()
        {
            ValidateSettings();
        }
    }
}