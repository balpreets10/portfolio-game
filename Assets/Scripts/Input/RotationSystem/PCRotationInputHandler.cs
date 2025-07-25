using System;

using Portfolio.CameraSystem;
using Portfolio.InputSystem.PC;

using UnityEngine;

namespace Portfolio.InputSystem
{
    /// <summary>
    /// PC platform input handler for mouse-based camera rotation with cursor lock system
    /// </summary>
    public class PCRotationInputHandler : IPlatformRotationInputHandler
    {
        public event Action<Vector2> OnRotationInput;

        public event Action<CursorState> OnCursorStateChanged;

        public event Action<CameraRotationState> OnRotationStateChanged;

        public bool IsRotationActive => HasRotationFlag(RotationInputState.RotationActive);

        // State management
        private CommonInputState commonState = CommonInputState.None;

        private RotationInputState rotationState = RotationInputState.None;
        private CursorState cursorState = CursorState.Unlocked;
        private CameraRotationState cameraRotationState = CameraRotationState.Inactive;

        private PCCameraSettings settings;
        private Vector2 lastMousePosition;

        #region Public Interface

        /// <summary>
        /// Initialize the PC input handler
        /// </summary>
        public void Initialize()
        {
            if (HasCommonFlag(CommonInputState.Initialized)) return;

            lastMousePosition = UnityEngine.Input.mousePosition;
            SetRotationFlag(RotationInputState.FirstFrame, true);
            SetCommonFlag(CommonInputState.Initialized, true);
            SetCursorState(CursorState.Unlocked);
            SetCameraRotationState(CameraRotationState.Inactive);
        }

        /// <summary>
        /// Update input processing (called per frame from CameraController)
        /// </summary>
        public void UpdateInput()
        {
            if (!HasCommonFlag(CommonInputState.Enabled | CommonInputState.Initialized)) return;

            HandleCursorInput();
            ProcessMouseMovement();
            ProcessMouseScroll();
        }

        /// <summary>
        /// Enable or disable input processing
        /// </summary>
        /// <param name="enabled">Input processing state</param>
        public void SetEnabled(bool enabled)
        {
            if (enabled)
            {
                SetCommonFlag(CommonInputState.Enabled, true);
                if (HasCommonFlag(CommonInputState.Initialized))
                {
                    SetRotationFlag(RotationInputState.FirstFrame, true);
                }
            }
            else
            {
                SetCommonFlag(CommonInputState.Enabled | CommonInputState.InputActive, false);
                SetCameraRotationState(CameraRotationState.Inactive);
                SetCursorState(CursorState.Unlocked);
                SetRotationFlag(RotationInputState.FirstFrame, true);
            }
        }

        /// <summary>
        /// Get if input is currently active
        /// </summary>
        /// <returns>True if cursor is locked and rotation is active</returns>
        public bool IsInputActive()
        {
            return HasCommonFlag(CommonInputState.Enabled | CommonInputState.Initialized | CommonInputState.InputActive) &&
                   HasRotationFlag(RotationInputState.CursorLocked) &&
                   HasRotationFlag(RotationInputState.RotationActive);
        }

        /// <summary>
        /// Cleanup resources when switching platforms
        /// </summary>
        public void Cleanup()
        {
            OnRotationInput = null;
            OnCursorStateChanged = null;
            OnRotationStateChanged = null;

            ResetState();
            SetCursorState(CursorState.Unlocked);
            SetCameraRotationState(CameraRotationState.Inactive);
        }

        /// <summary>
        /// Set camera settings
        /// </summary>
        public void SetSettings(PCCameraSettings settings)
        {
            this.settings = settings;
        }

        /// <summary>
        /// Force cursor lock state (useful for external systems)
        /// </summary>
        public void ForceCursorLock(bool locked)
        {
            if (locked)
            {
                SetCursorState(CursorState.Locked);
            }
            else
            {
                SetCursorState(CursorState.Unlocked);
            }
        }

        #endregion Public Interface

        #region Private Methods

        /// <summary>
        /// Handle cursor lock/unlock input
        /// </summary>
        private void HandleCursorInput()
        {
            // Escape key to unlock cursor
            if (Input.GetKeyUp(KeyCode.Escape))
            {
                if (cursorState == CursorState.Locked)
                {
                    SetCursorState(CursorState.Unlocked);
                }
            }
            // Left mouse click to lock cursor when unlocked
            else if (Input.GetMouseButtonUp(0))
            {
                if (cursorState == CursorState.Unlocked)
                {
                    SetCursorState(CursorState.Locked);
                }
            }
        }

        /// <summary>
        /// Process mouse movement for camera rotation
        /// </summary>
        private void ProcessMouseMovement()
        {
            // Only process rotation when cursor is locked
            if (cursorState != CursorState.Locked)
            {
                SetCameraRotationState(CameraRotationState.Inactive);
                SetRotationFlag(RotationInputState.RotationActive, false);
                SetCommonFlag(CommonInputState.InputActive, false);
                return;
            }

            // Use Input.GetAxis for mouse delta when cursor is locked
            Vector2 mouseDelta = new Vector2(
                UnityEngine.Input.GetAxis("Mouse X"),
                UnityEngine.Input.GetAxis("Mouse Y")
            );

            // Apply deadzone
            if (mouseDelta.magnitude > settings.RotationDeadzone)
            {
                Vector2 rotationInput = mouseDelta * settings.Sensitivity;

                // Invert Y axis if needed
                if (settings.InvertYAxis)
                    rotationInput.y = -rotationInput.y;

                OnRotationInput?.Invoke(rotationInput);
                SetCameraRotationState(CameraRotationState.Active);
                SetRotationFlag(RotationInputState.RotationActive, true);
                SetCommonFlag(CommonInputState.InputActive, true);
            }
            else
            {
                SetCameraRotationState(CameraRotationState.Inactive);
                SetRotationFlag(RotationInputState.RotationActive, false);
                SetCommonFlag(CommonInputState.InputActive, false);
            }
        }

        /// <summary>
        /// Process mouse scroll for zoom
        /// </summary>
        private void ProcessMouseScroll()
        {
            if (!settings.EnableMouseScroll) return;

            float scroll = UnityEngine.Input.GetAxis("Mouse ScrollWheel");
            if (Mathf.Abs(scroll) > 0.01f)
            {
                Vector2 zoomInput = new Vector2(0, scroll * settings.MouseScrollSensitivity);
                OnRotationInput?.Invoke(zoomInput);
            }
        }

        #endregion Private Methods

        #region State Management

        /// <summary>
        /// Set cursor state and update Unity cursor
        /// </summary>
        private void SetCursorState(CursorState newState)
        {
            if (cursorState == newState) return;

            cursorState = newState;

            // Update Unity cursor state and rotation flags
            switch (newState)
            {
                case CursorState.Locked:
                    Cursor.lockState = CursorLockMode.Locked;
                    SetRotationFlag(RotationInputState.CursorLocked | RotationInputState.FirstFrame, true);
                    break;

                case CursorState.Unlocked:
                    Cursor.lockState = CursorLockMode.None;
                    SetRotationFlag(RotationInputState.CursorLocked, false);
                    break;

                case CursorState.Transitioning:
                    SetRotationFlag(RotationInputState.CursorTransitioning, true);
                    break;
            }

            OnCursorStateChanged?.Invoke(newState);
        }

        /// <summary>
        /// Set camera rotation state with event notification
        /// </summary>
        private void SetCameraRotationState(CameraRotationState newState)
        {
            if (cameraRotationState == newState) return;

            cameraRotationState = newState;
            OnRotationStateChanged?.Invoke(newState);
        }

        private bool HasCommonFlag(CommonInputState flag)
        {
            return (commonState & flag) == flag;
        }

        private void SetCommonFlag(CommonInputState flag, bool value)
        {
            if (value)
                commonState |= flag;
            else
                commonState &= ~flag;
        }

        private bool HasRotationFlag(RotationInputState flag)
        {
            return (rotationState & flag) == flag;
        }

        private void SetRotationFlag(RotationInputState flag, bool value)
        {
            if (value)
                rotationState |= flag;
            else
                rotationState &= ~flag;
        }

        private void ResetState()
        {
            commonState = CommonInputState.None;
            rotationState = RotationInputState.None;
        }

        #endregion State Management

        #region Public Properties (for debugging/monitoring)

        public CommonInputState CommonState => commonState;
        public RotationInputState RotationStateFlags => rotationState;
        public CursorState CurrentCursorState => cursorState;
        public CameraRotationState CameraRotationState => cameraRotationState;

        #endregion Public Properties (for debugging/monitoring)
    }
}

// Legacy enums kept for backward compatibility
namespace Portfolio.InputSystem
{
    /// <summary>
    /// Input handler states for better state management
    /// </summary>
    public enum InputHandlerState
    {
        Disabled,
        Enabled,
        Initialized
    }

    /// <summary>
    /// Camera rotation states
    /// </summary>
    public enum CameraRotationState
    {
        Inactive,
        Active,
        Locked
    }

    /// <summary>
    /// Cursor lock states
    /// </summary>
    public enum CursorState
    {
        Unlocked,
        Locked,
        Transitioning
    }
}