using System;
using UnityEngine;
using Reflex.Attributes;

namespace Portfolio.InputSystem.Mobile
{
    public class MobileMovementInputHandler : IPlatformMovementInputHandler
    {
        public event Action<Vector2> OnMovementInput;

        public event Action<BoostData> OnSpeedBoostRequested;

        [Header("Testing")]
        [SerializeField] private bool enableMouseEmulation = true;

        // State management
        private CommonInputState commonState = CommonInputState.None;

        private MovementInputState movementState = MovementInputState.None;

        private Vector2 currentInput = Vector2.zero;
        private int activeTouchId = -1;
        private Vector2 inputStartPosition;

        // Constants
        private const float JOYSTICK_RANGE = 100f;

        private const float SCREEN_SPLIT_RATIO = 0.5f;

        [Inject] private IJoystick virtualJoystick;

        public void Initialize()
        {
            if (HasCommonFlag(CommonInputState.Initialized)) return;

            virtualJoystick?.InitializeJoystick();
            SetCommonFlag(CommonInputState.Initialized, true);
        }

        public void UpdateInput()
        {
            if (!HasCommonFlag(CommonInputState.Enabled | CommonInputState.Initialized)) return;

            if (enableMouseEmulation && Application.isEditor)
                ProcessMouseInput();
            else
                ProcessTouchInput();
        }

        public void SetEnabled(bool enabled)
        {
            SetCommonFlag(CommonInputState.Enabled, enabled);

            if (!enabled)
            {
                StopInput();
                virtualJoystick?.ShowJoystick(false);
            }
        }

        public bool IsInputActive()
        {
            return HasCommonFlag(CommonInputState.Enabled | CommonInputState.Initialized | CommonInputState.InputActive);
        }

        public void Cleanup()
        {
            StopInput();
            OnMovementInput = null;
            ResetState();
        }

        #region Touch Input Processing

        private void ProcessTouchInput()
        {
            // Early exit if no touches
            if (Input.touchCount == 0)
            {
                if (HasCommonFlag(CommonInputState.InputActive))
                {
                    VerifyActiveTouchExists();
                }
                return;
            }

            for (int i = 0; i < Input.touchCount; i++)
            {
                Touch touch = Input.touches[i];

                if (touch.fingerId == activeTouchId)
                {
                    HandleActiveTouch(touch);
                    return; // Found our touch, exit early
                }
                else if (!HasCommonFlag(CommonInputState.InputActive) && touch.phase == TouchPhase.Began)
                {
                    TryStartInput(touch.fingerId, touch.position);
                }
            }
        }

        private void HandleActiveTouch(Touch touch)
        {
            switch (touch.phase)
            {
                case TouchPhase.Moved:
                    if (IsLeftSideOfScreen(touch.position))
                        UpdateInput(touch.position);
                    else
                        StopInput();
                    break;

                case TouchPhase.Ended:
                case TouchPhase.Canceled:
                    StopInput();
                    break;
            }
        }

        private void TryStartInput(int touchId, Vector2 position)
        {
            if (IsLeftSideOfScreen(position))
            {
                StartInput(touchId, position);
            }
        }

        private void VerifyActiveTouchExists()
        {
            // Check if active touch still exists
            for (int i = 0; i < Input.touchCount; i++)
            {
                if (Input.touches[i].fingerId == activeTouchId)
                    return; // Touch still exists
            }

            // Touch no longer exists
            StopInput();
        }

        #endregion Touch Input Processing

        #region Mouse Emulation (Editor Only)

        private void ProcessMouseInput()
        {
            Vector2 mousePos = Input.mousePosition;
            bool isLeftSide = IsLeftSideOfScreen(mousePos);

            if (Input.GetMouseButtonDown(0) && isLeftSide && !HasCommonFlag(CommonInputState.InputActive))
            {
                StartInput(0, mousePos);
            }
            else if (Input.GetMouseButton(0) && HasCommonFlag(CommonInputState.InputActive))
            {
                if (isLeftSide)
                    UpdateInput(mousePos);
                else
                    StopInput();
            }
            else if (Input.GetMouseButtonUp(0) && HasCommonFlag(CommonInputState.InputActive))
            {
                StopInput();
            }
        }

        #endregion Mouse Emulation (Editor Only)

        #region Input Management

        private void StartInput(int touchId, Vector2 position)
        {
            activeTouchId = touchId;
            SetCommonFlag(CommonInputState.InputActive, true);
            inputStartPosition = position;

            // Show joystick at touch position
            Vector2 uiPosition = ScreenToUIPosition(position);
            virtualJoystick?.SetJoystickPosition(uiPosition);
            virtualJoystick?.ShowJoystick(true);
        }

        private void UpdateInput(Vector2 currentPosition)
        {
            if (!HasCommonFlag(CommonInputState.InputActive)) return;

            // Calculate movement vector
            Vector2 delta = currentPosition - inputStartPosition;
            float distance = delta.magnitude;

            // Clamp to joystick range
            if (distance > JOYSTICK_RANGE)
            {
                delta = delta.normalized * JOYSTICK_RANGE;
            }

            // Convert to normalized input (-1 to 1)
            currentInput = delta / JOYSTICK_RANGE;

            // Update visuals and fire event
            virtualJoystick?.UpdateJoystickVisual(currentInput);
            OnMovementInput?.Invoke(currentInput);
        }

        private void StopInput()
        {
            if (!HasCommonFlag(CommonInputState.InputActive)) return;

            activeTouchId = -1;
            SetCommonFlag(CommonInputState.InputActive, false);
            currentInput = Vector2.zero;

            virtualJoystick?.UpdateJoystickVisual(Vector2.zero);
            virtualJoystick?.ShowJoystick(false);
            OnMovementInput?.Invoke(Vector2.zero);
        }

        private bool IsLeftSideOfScreen(Vector2 screenPosition)
        {
            return screenPosition.x < Screen.width * SCREEN_SPLIT_RATIO;
        }

        private Vector2 ScreenToUIPosition(Vector2 screenPosition)
        {
            Canvas canvas = virtualJoystick?.GetCanvas();
            Camera uiCamera = virtualJoystick?.GetCamera();

            if (canvas != null)
            {
                RectTransform canvasRect = canvas.GetComponent<RectTransform>();
                if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    canvasRect, screenPosition, uiCamera, out Vector2 localPosition))
                {
                    return localPosition;
                }
            }

            return screenPosition;
        }

        #endregion Input Management

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
            activeTouchId = -1;
        }

        public void BlockInput(float duration)
        {
        }

        public bool IsInputBlocked()
        {
            return false;
        }

        #endregion State Management
    }
}