using System;

using Portfolio.InputSystem.PC;

using UnityEngine;

namespace Portfolio.InputSystem
{
    public class PCMovementInputHandler : IPlatformMovementInputHandler
    {
        /// <summary>
        /// Event fired when movement input is detected
        /// </summary>
        public event Action<Vector2> OnMovementInput;

        /// <summary>
        /// Event fired when speed boost is requested
        /// </summary>
        public event Action<BoostData> OnSpeedBoostRequested;

        // State management
        private CommonInputState commonState = CommonInputState.None;

        private MovementInputState movementState = MovementInputState.None;
        private Vector2 moveInput;

        // Input blocking
        private float inputBlockTimer = 0f;

        private bool isInputBlocked = false;

        // Boost settings (injected via DI)
        private SpeedBoostSettings boostSettings;

        /// <summary>
        /// Constructor for dependency injection
        /// </summary>
        public PCMovementInputHandler(SpeedBoostSettings settings)
        {
            boostSettings = settings;
        }

        /// <summary>
        /// Initialize the PC input handler
        /// </summary>
        public void Initialize()
        {
            if (HasCommonFlag(CommonInputState.Initialized)) return;

            SetMovementFlag(MovementInputState.FirstFrame, true);
            SetCommonFlag(CommonInputState.Initialized, true);
        }

        /// <summary>
        /// Update input processing
        /// </summary>
        public void UpdateInput()
        {
            if (!HasCommonFlag(CommonInputState.Enabled | CommonInputState.Initialized)) return;

            UpdateInputBlocking();

            if (!isInputBlocked)
            {
                ProcessKeyboardInput();
                ProcessBoostInput();
            }
            else
            {
                // Clear input while blocked
                moveInput = Vector2.zero;
                SetCommonFlag(CommonInputState.InputActive, false);
                OnMovementInput?.Invoke(moveInput);
            }
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
                    SetMovementFlag(MovementInputState.FirstFrame, true);
                }
            }
            else
            {
                SetCommonFlag(CommonInputState.Enabled | CommonInputState.InputActive, false);
            }
        }

        /// <summary>
        /// Block input for a specified duration
        /// </summary>
        /// <param name="duration">Duration to block input</param>
        public void BlockInput(float duration)
        {
            inputBlockTimer = duration;
            isInputBlocked = true;

            if (boostSettings != null && boostSettings.enableDebugLogging)
            {
                Debug.Log($"Input blocked for {duration} seconds");
            }
        }

        /// <summary>
        /// Check if input is currently blocked
        /// </summary>
        /// <returns>True if input is blocked</returns>
        public bool IsInputBlocked()
        {
            return isInputBlocked;
        }

        /// <summary>
        /// Get if input is currently active
        /// </summary>
        /// <returns>True if movement input is detected</returns>
        public bool IsInputActive()
        {
            return HasCommonFlag(CommonInputState.Enabled | CommonInputState.Initialized | CommonInputState.InputActive);
        }

        /// <summary>
        /// Cleanup resources when switching platforms
        /// </summary>
        public void Cleanup()
        {
            OnMovementInput = null;
            OnSpeedBoostRequested = null;
            ResetState();
        }

        /// <summary>
        /// Update input blocking timer
        /// </summary>
        private void UpdateInputBlocking()
        {
            if (isInputBlocked)
            {
                inputBlockTimer -= Time.deltaTime;
                if (inputBlockTimer <= 0f)
                {
                    isInputBlocked = false;
                    inputBlockTimer = 0f;

                    if (boostSettings != null && boostSettings.enableDebugLogging)
                    {
                        Debug.Log("Input unblocked");
                    }
                }
            }
        }

        /// <summary>
        /// Process keyboard input for movement
        /// </summary>
        private void ProcessKeyboardInput()
        {
            moveInput.x = UnityEngine.Input.GetAxis("Horizontal");
            moveInput.y = UnityEngine.Input.GetAxis("Vertical");

            bool hasInput = moveInput != Vector2.zero;
            SetCommonFlag(CommonInputState.InputActive, hasInput);

            // Reset first frame flag after first update
            if (HasMovementFlag(MovementInputState.FirstFrame))
            {
                SetMovementFlag(MovementInputState.FirstFrame, false);
            }

            // Fire movement input event
            OnMovementInput?.Invoke(moveInput);
        }

        /// <summary>
        /// Process boost input (Left Shift)
        /// </summary>
        private void ProcessBoostInput()
        {
            if (UnityEngine.Input.GetKeyDown(KeyCode.LeftShift))
            {
                RequestSpeedBoost();
            }
        }

        /// <summary>
        /// Request speed boost activation
        /// </summary>
        private void RequestSpeedBoost()
        {
            if (boostSettings == null)
            {
                Debug.LogWarning("SpeedBoostSettings not available for boost request");
                return;
            }

            // Calculate boost direction based on current movement input
            Vector3 boostDirection = Vector3.zero;

            if (moveInput != Vector2.zero)
            {
                // Use current movement direction for boost
                boostDirection = new Vector3(moveInput.x, 0f, moveInput.y).normalized;
            }
            else
            {
                // If no movement input, boost forward
                boostDirection = Vector3.forward;
            }

            // Create boost data
            BoostData boostData = BoostData.CreateFromSettings(boostSettings, boostDirection);

            if (boostSettings.enableDebugLogging)
            {
                Debug.Log($"Speed boost requested - Direction: {boostDirection}, Duration: {boostData.duration}, Distance: {boostData.distance}");
            }

            // Fire boost event
            OnSpeedBoostRequested?.Invoke(boostData);
        }

        #region State Management

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

        private bool HasMovementFlag(MovementInputState flag)
        {
            return (movementState & flag) == flag;
        }

        private void SetMovementFlag(MovementInputState flag, bool value)
        {
            if (value)
                movementState |= flag;
            else
                movementState &= ~flag;
        }

        private void ResetState()
        {
            commonState = CommonInputState.None;
            movementState = MovementInputState.None;
            isInputBlocked = false;
            inputBlockTimer = 0f;
        }

        #endregion State Management
    }
}