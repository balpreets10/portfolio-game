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

        // State management - simplified
        private bool isEnabled = false;

        private bool isInitialized = false;
        private bool isInputActive = false;

        private Vector2 currentInput = Vector2.zero;
        private int activeTouchId = -1;
        private Vector2 inputStartPosition;

        // Constants
        private const float JOYSTICK_RANGE = 100f;

        private const float SCREEN_SPLIT_RATIO = 0.5f;

        [Inject] private IJoystick virtualJoystick;

        public void Initialize()
        {
            if (isInitialized) return;

            virtualJoystick?.InitializeJoystick();
            isInitialized = true;
        }

        public void UpdateInput()
        {
            if (!isEnabled || !isInitialized) return;

            if (enableMouseEmulation && Application.isEditor)
                ProcessMouseInput();
            else
                ProcessTouchInput();
        }

        public void SetEnabled(bool enabled)
        {
            isEnabled = enabled;

            if (!enabled)
            {
                StopInput();
                virtualJoystick?.ShowJoystick(false);
            }
        }

        public bool IsInputActive()
        {
            return isEnabled && isInitialized && isInputActive;
        }

        public void Cleanup()
        {
            StopInput();
            OnMovementInput = null;
            isEnabled = false;
            isInitialized = false;
        }

        #region Touch Input Processing

        private void ProcessTouchInput()
        {
            // Handle existing active touch first
            if (activeTouchId != -1)
            {
                bool foundActiveTouch = false;
                for (int i = 0; i < Input.touchCount; i++)
                {
                    Touch touch = Input.touches[i];
                    if (touch.fingerId == activeTouchId)
                    {
                        foundActiveTouch = true;
                        HandleActiveTouch(touch);
                        break;
                    }
                }

                // If active touch not found, stop input
                if (!foundActiveTouch)
                {
                    StopInput();
                }
                return; // Don't process new touches while one is active
            }

            // Look for new touches on the left side
            for (int i = 0; i < Input.touchCount; i++)
            {
                Touch touch = Input.touches[i];
                if (touch.phase == TouchPhase.Began && IsLeftSideOfScreen(touch.position))
                {
                    StartInput(touch.fingerId, touch.position);
                    break; // Only handle one new touch at a time
                }
            }
        }

        private void HandleActiveTouch(Touch touch)
        {
            switch (touch.phase)
            {
                case TouchPhase.Moved:
                case TouchPhase.Stationary:
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

        #endregion Touch Input Processing

        #region Mouse Emulation (Editor Only)

        private void ProcessMouseInput()
        {
            Vector2 mousePos = Input.mousePosition;
            bool isLeftSide = IsLeftSideOfScreen(mousePos);

            if (Input.GetMouseButtonDown(0) && isLeftSide && !isInputActive)
            {
                StartInput(0, mousePos);
            }
            else if (Input.GetMouseButton(0) && isInputActive)
            {
                if (isLeftSide)
                    UpdateInput(mousePos);
                else
                    StopInput();
            }
            else if (Input.GetMouseButtonUp(0) && isInputActive)
            {
                StopInput();
            }
        }

        #endregion Mouse Emulation (Editor Only)

        #region Input Management

        private void StartInput(int touchId, Vector2 position)
        {
            activeTouchId = touchId;
            isInputActive = true;
            inputStartPosition = position;

            // Convert screen position to UI position using simplified method
            Vector2 uiPosition = ScreenToUIPositionFixed(position);
            virtualJoystick?.SetJoystickPosition(uiPosition);
            virtualJoystick?.ShowJoystick(true);
        }

        private void UpdateInput(Vector2 currentPosition)
        {
            if (!isInputActive) return;

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
            if (!isInputActive) return;

            activeTouchId = -1;
            isInputActive = false;
            currentInput = Vector2.zero;

            virtualJoystick?.UpdateJoystickVisual(Vector2.zero);
            virtualJoystick?.ShowJoystick(false);
            OnMovementInput?.Invoke(Vector2.zero);
        }

        private bool IsLeftSideOfScreen(Vector2 screenPosition)
        {
            return screenPosition.x < Screen.width * SCREEN_SPLIT_RATIO;
        }

        // FIXED: Simplified coordinate conversion that maintains linear relationship
        private Vector2 ScreenToUIPositionFixed(Vector2 screenPosition)
        {
            Canvas canvas = virtualJoystick?.GetCanvas();

            if (canvas == null) return screenPosition;

            RectTransform canvasRect = canvas.GetComponent<RectTransform>();

            // For ScreenSpaceOverlay canvases, use direct conversion
            if (canvas.renderMode == RenderMode.ScreenSpaceOverlay)
            {
                // Convert screen coordinates to canvas coordinates
                // Canvas size matches screen size in ScreenSpaceOverlay
                Vector2 canvasSize = canvasRect.sizeDelta;

                // Convert screen position to canvas position
                // Screen (0,0) is bottom-left, Canvas (0,0) is center
                Vector2 canvasPosition = new Vector2(
                    screenPosition.x - Screen.width * 0.5f,
                    screenPosition.y - Screen.height * 0.5f
                );

                return canvasPosition;
            }
            else
            {
                // For camera-based canvases, use the utility method
                Camera uiCamera = virtualJoystick?.GetCamera();
                if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    canvasRect, screenPosition, uiCamera, out Vector2 localPosition))
                {
                    return localPosition;
                }
            }

            return screenPosition;
        }

        #endregion Input Management

        #region Required Interface Methods

        public void BlockInput(float duration)
        {
            // Implementation if needed for boost system
        }

        public bool IsInputBlocked()
        {
            return false;
        }

        #endregion Required Interface Methods
    }
}